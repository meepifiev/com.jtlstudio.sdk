using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using JTLStudio.SDK.Providers;
using JTLStudio.SDK.Services.Json;
using UnityEngine;

namespace JTLStudio.SDK.Services
{
    public class DataService : ModuleBase, IData
    {
        private const int DocumentFormat = 1;
        private const float RetryIntervalSeconds = 30f;
        private const string FormatKey = "format";
        private const string RevisionKey = "revision";
        private const string SavedAtKey = "savedAt";
        private const string ValuesKey = "values";

        private readonly IDataProvider _provider;
        private readonly IBackupStorage _backup;
        private readonly string _backupKey;
        private readonly float _autosaveDelaySeconds;
        private readonly List<Action<bool>> _queuedCallbacks = new List<Action<bool>>();
        private readonly JsonWriter _writer = new JsonWriter();
        private readonly JsonParser _parser = new JsonParser();
        private Dictionary<string, object> _values = new Dictionary<string, object>();
        private long _revision;
        private int _writeVersion;
        private float _autosaveTimer;
        private float _retryTimer;
        private bool _saving;
        private bool _reloading;
        private bool _queuedSave;
        private bool _oversizeLogged;
        private bool _autosaveRequested;

        public DataService(IDataProvider provider, float autosaveDelaySeconds, SdkLogger logger, IBackupStorage backup = null, string backupKey = "") : base(logger)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _autosaveDelaySeconds = Mathf.Max(0f, autosaveDelaySeconds);
            _backup = backup;
            _backupKey = string.IsNullOrEmpty(backupKey) ? "JTLSDK.Backup" : backupKey;
        }

        public event Action Loaded;
        public event Action<bool> Saved;

        public DataState LoadState { get; private set; } = DataState.Pending;
        public bool IsDirty { get; private set; }

        internal override string ModuleName => "Data";

        private bool CanWriteToStorage => State == ModuleState.Ready && (LoadState == DataState.Loaded || LoadState == DataState.Empty);

        public bool HasKey(string key)
        {
            ValidateKey(key);
            return _values.ContainsKey(key);
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            if (TryGetRaw(key, out object raw) == false)
            {
                return defaultValue;
            }

            switch (raw)
            {
                case long number:
                    return (int)number;

                case double number:
                    return (int)number;

                default:
                    return defaultValue;
            }
        }

        public float GetFloat(string key, float defaultValue = 0f)
        {
            if (TryGetRaw(key, out object raw) == false)
            {
                return defaultValue;
            }

            switch (raw)
            {
                case double number:
                    return (float)number;

                case long number:
                    return number;

                default:
                    return defaultValue;
            }
        }

        public bool GetBool(string key, bool defaultValue = false)
        {
            return TryGetRaw(key, out object raw) && raw is bool flag ? flag : defaultValue;
        }

        public string GetString(string key, string defaultValue = "")
        {
            return TryGetRaw(key, out object raw) && raw is string text ? text : defaultValue;
        }

        public T GetObject<T>(string key, T defaultValue = null) where T : class
        {
            if (TryGetRaw(key, out object raw) == false || raw is Dictionary<string, object> == false)
            {
                return defaultValue;
            }

            T result = Activator.CreateInstance<T>();

            try
            {
                JsonUtility.FromJsonOverwrite(_writer.Write(raw), result);
            }
            catch (Exception exception)
            {
                Logger.Exception(exception);
                return defaultValue;
            }

            return result;
        }

        public void SetInt(string key, int value, bool important = true)
        {
            Store(key, (long)value, important);
        }

        public void SetFloat(string key, float value, bool important = true)
        {
            Store(key, (double)value, important);
        }

        public void SetBool(string key, bool value, bool important = true)
        {
            Store(key, value, important);
        }

        public void SetString(string key, string value, bool important = true)
        {
            Store(key, value ?? "", important);
        }

        public void SetObject<T>(string key, T value, bool important = true) where T : class
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            object parsed = _parser.Parse(JsonUtility.ToJson(value));
            Store(key, parsed, important);
        }

        public void DeleteKey(string key)
        {
            ValidateKey(key);

            if (RejectIfNotReady(nameof(DeleteKey)))
            {
                return;
            }

            if (_values.Remove(key))
            {
                MarkDirty();
            }
        }

        public void DeleteAll()
        {
            if (RejectIfNotReady(nameof(DeleteAll)))
            {
                return;
            }

            if (_values.Count == 0)
            {
                return;
            }

            _values.Clear();
            MarkDirty();
        }

        public void Save(Action<bool> onSaved = null)
        {
            if (CanWriteToStorage == false || _reloading)
            {
                onSaved?.Invoke(false);
                return;
            }

            if (_saving)
            {
                _queuedSave = true;

                if (onSaved != null)
                {
                    _queuedCallbacks.Add(onSaved);
                }

                return;
            }

            string serialized = SerializeDocument(_revision + 1);
            int bytes = Encoding.UTF8.GetByteCount(serialized);

            if (_provider.MaxBytes > 0 && bytes > _provider.MaxBytes)
            {
                _autosaveTimer = 0f;

                if (_oversizeLogged == false)
                {
                    _oversizeLogged = true;
                    Logger.Error("Save data is " + bytes + " bytes, the platform limit is " + _provider.MaxBytes + " bytes. Nothing was written.");
                }

                Saved?.Invoke(false);
                onSaved?.Invoke(false);
                return;
            }

            _oversizeLogged = false;

            if (_provider.RecommendedBytes > 0 && bytes > _provider.RecommendedBytes)
            {
                Logger.Warning("Save data is " + bytes + " bytes, the platform recommends at most " + _provider.RecommendedBytes + " bytes.");
            }

            int version = _writeVersion;
            Logger.Info("Saving " + bytes + " bytes.");
            _saving = true;
            _autosaveTimer = 0f;

            try
            {
                _provider.Save(serialized, success => OnSaved(success, version, onSaved));
            }
            catch (Exception exception)
            {
                Logger.Exception(exception);
                OnSaved(false, version, onSaved);
            }
        }

        internal override void Initialize()
        {
            _provider.Initialize(OnProviderInitialized);
        }

        internal void Tick(float unscaledDeltaTime)
        {
            if (LoadState == DataState.Failed && State == ModuleState.Ready && _reloading == false)
            {
                _retryTimer += unscaledDeltaTime;

                if (_retryTimer >= RetryIntervalSeconds)
                {
                    _retryTimer = 0f;
                    _provider.Load(OnRetryLoaded);
                }

                return;
            }

            if (_autosaveRequested == false || IsDirty == false || CanWriteToStorage == false)
            {
                return;
            }

            _autosaveTimer += unscaledDeltaTime;

            if (_autosaveTimer >= _autosaveDelaySeconds)
            {
                Save(null);
            }
        }

        internal void FlushBeforeUnload()
        {
            if (IsDirty == false || CanWriteToStorage == false)
            {
                return;
            }

            WriteBackup();
            Save(null);
        }

        internal void SaveIfDirty()
        {
            if (IsDirty)
            {
                Save(null);
            }
        }

        internal void ReloadAfterAuthorization(Action onReloaded)
        {
            if (State != ModuleState.Ready)
            {
                onReloaded?.Invoke();
                return;
            }

            _reloading = true;
            _provider.Load((result, serialized) => OnReloaded(result, serialized, onReloaded));
        }

        private void OnProviderInitialized(ProviderState state)
        {
            if (state != ProviderState.Ready)
            {
                LoadState = state == ProviderState.Failed ? DataState.Failed : DataState.Empty;
                CompleteInitialization(state);
                return;
            }

            _provider.Load(OnLoaded);
        }

        private void OnLoaded(DataLoadResult result, string serialized)
        {
            ApplyLoadResult(result, serialized);
            bool restored = TryRestoreBackup();
            Logger.Info("Save data is " + LoadState + ", " + _values.Count + " keys.");
            SetState(ModuleState.Ready);

            if (restored)
            {
                MarkDirty();
                Save(null);
            }

            Loaded?.Invoke();
        }

        private bool TryRestoreBackup()
        {
            if (_backup == null || (LoadState != DataState.Loaded && LoadState != DataState.Empty))
            {
                return false;
            }

            string stored = ReadBackup();

            if (string.IsNullOrEmpty(stored))
            {
                return false;
            }

            Dictionary<string, object> loadedValues = _values;
            long loadedRevision = _revision;

            if (TryParseDocument(stored) == false || _revision <= loadedRevision)
            {
                _values = loadedValues;
                _revision = loadedRevision;
                ClearBackup();
                return false;
            }

            Logger.Info("Save data was restored from the local backup: revision " + _revision + " over " + loadedRevision + ".");
            _revision = loadedRevision;
            LoadState = DataState.Loaded;
            return true;
        }

        private void WriteBackup()
        {
            if (_backup == null)
            {
                return;
            }

            try
            {
                _backup.Write(_backupKey, SerializeDocument(_revision + 1));
            }
            catch (Exception exception)
            {
                Logger.Exception(exception);
            }
        }

        private string ReadBackup()
        {
            try
            {
                return _backup.Read(_backupKey);
            }
            catch (Exception exception)
            {
                Logger.Exception(exception);
                return "";
            }
        }

        private void ClearBackup()
        {
            if (_backup == null)
            {
                return;
            }

            try
            {
                _backup.Clear(_backupKey);
            }
            catch (Exception exception)
            {
                Logger.Exception(exception);
            }
        }

        private void OnRetryLoaded(DataLoadResult result, string serialized)
        {
            if (result == DataLoadResult.Failed)
            {
                return;
            }

            ApplyLoadResult(result, serialized);
            IsDirty = false;
            Logger.Info("Save data loaded after a retry.");
            Loaded?.Invoke();
        }

        private void OnReloaded(DataLoadResult result, string serialized, Action onReloaded)
        {
            _reloading = false;

            switch (result)
            {
                case DataLoadResult.Loaded:
                    ApplyLoadResult(result, serialized);
                    IsDirty = false;
                    Loaded?.Invoke();
                    break;

                case DataLoadResult.Empty:
                    LoadState = DataState.Empty;
                    MarkDirty();
                    Save(null);
                    break;

                case DataLoadResult.Failed:
                    Logger.Warning("Save data could not be reloaded after authorization.");
                    break;
            }

            onReloaded?.Invoke();
        }

        private void ApplyLoadResult(DataLoadResult result, string serialized)
        {
            switch (result)
            {
                case DataLoadResult.Loaded:
                    if (TryParseDocument(serialized))
                    {
                        LoadState = DataState.Loaded;
                    }
                    else
                    {
                        Logger.Error("Save data could not be parsed. Writing is blocked until a valid document is loaded.");
                        LoadState = DataState.Failed;
                    }

                    break;

                case DataLoadResult.Empty:
                    _values = new Dictionary<string, object>();
                    _revision = 0;
                    LoadState = DataState.Empty;
                    break;

                case DataLoadResult.Failed:
                    _values = new Dictionary<string, object>();
                    _revision = 0;
                    LoadState = DataState.Failed;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(result));
            }
        }

        private bool TryParseDocument(string serialized)
        {
            if (string.IsNullOrWhiteSpace(serialized))
            {
                _values = new Dictionary<string, object>();
                _revision = 0;
                return true;
            }

            try
            {
                object parsed = _parser.Parse(serialized);

                if (parsed is Dictionary<string, object> document == false)
                {
                    return false;
                }

                if (document.TryGetValue(ValuesKey, out object values) && values is Dictionary<string, object> dictionary)
                {
                    _values = dictionary;
                }
                else
                {
                    _values = new Dictionary<string, object>();
                }

                _revision = document.TryGetValue(RevisionKey, out object revision) && revision is long number ? number : 0;
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private string SerializeDocument(long revision)
        {
            Dictionary<string, object> document = new Dictionary<string, object>
            {
                { FormatKey, (long)DocumentFormat },
                { RevisionKey, revision },
                { SavedAtKey, DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture) },
                { ValuesKey, _values }
            };

            return _writer.Write(document);
        }

        private void OnSaved(bool success, int version, Action<bool> onSaved)
        {
            _saving = false;

            if (success)
            {
                _revision++;

                if (version == _writeVersion)
                {
                    IsDirty = false;
                    _autosaveRequested = false;
                }

                ClearBackup();
            }
            else
            {
                Logger.Warning("Save data was not written by the platform.");
            }

            Saved?.Invoke(success);
            onSaved?.Invoke(success);
            RunQueuedSave(success);
        }

        private void RunQueuedSave(bool success)
        {
            if (_queuedSave == false)
            {
                return;
            }

            _queuedSave = false;
            List<Action<bool>> callbacks = new List<Action<bool>>(_queuedCallbacks);
            _queuedCallbacks.Clear();

            if (IsDirty == false || CanWriteToStorage == false)
            {
                foreach (Action<bool> callback in callbacks)
                {
                    callback(success);
                }

                return;
            }

            Save(result =>
            {
                foreach (Action<bool> callback in callbacks)
                {
                    callback(result);
                }
            });
        }

        private bool TryGetRaw(string key, out object raw)
        {
            ValidateKey(key);
            raw = null;

            if (IsReady == false)
            {
                Logger.Warning("Data." + key + " was read before the module became ready. The default value is returned.");
                return false;
            }

            return _values.TryGetValue(key, out raw);
        }

        private void Store(string key, object value, bool important)
        {
            ValidateKey(key);

            if (IsReady == false)
            {
                Logger.Error("Data." + key + " was written before the module became ready. The value is ignored.");
                return;
            }

            _values[key] = value;
            MarkDirty(important);
        }

        private void MarkDirty(bool important = true)
        {
            IsDirty = true;
            _writeVersion++;

            if (important == false)
            {
                return;
            }

            _autosaveRequested = true;
            _autosaveTimer = 0f;
        }

        private void ValidateKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException(nameof(key));
            }
        }
    }
}
