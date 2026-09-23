using System;
using System.Collections.Generic;
using JTLStudio.SDK.Bridge;
using JTLStudio.SDK.Providers;
using JTLStudio.SDK.Services;
#if UNITY_EDITOR
using JTLStudio.SDK.Prototype;
#endif
using UnityEngine;

namespace JTLStudio.SDK
{
    public class SdkInstance
    {
        private const string RuntimeObjectName = "JTLSDK";

        private readonly JTLSDKSettings _settings;
        private readonly SdkLogger _logger;
        private readonly List<ModuleBase> _modules = new List<ModuleBase>();
        private readonly List<Action> _readyCallbacks = new List<Action>();
        private readonly PlatformService _platform;
        private readonly PauseService _pause;
        private readonly TimeService _time;
        private readonly AudioService _audio;
        private readonly DeviceService _device;
        private readonly GameEventsService _gameEvents;
        private readonly AdsService _ads;
        private readonly DataService _data;
        private readonly PaymentsService _payments;
        private readonly LanguageService _language;
        private readonly LeaderboardsService _leaderboards;
        private readonly PlayerService _player;
        private readonly FlagsService _flags;
        private readonly ReviewService _review;
        private readonly GameLabelService _gameLabel;
        private readonly LinksService _links;

        private readonly WebBridge _bridge;
        private SdkRuntimeBehaviour _behaviour;
        private float _initializationSeconds;
        private bool _initialized;
        private bool _disposed;

        public SdkInstance(JTLSDKSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logger = new SdkLogger(settings.LogLevel);

            SdkConfiguration configuration = settings.ActiveConfiguration;

            IPlatformProvider platformProvider = ResolveProvider(configuration?.PlatformProvider, new FallbackPlatformProvider());
            IAdsProvider adsProvider = ResolveProvider(configuration?.Ads, new UnsupportedAdsProvider());
            IDataProvider dataProvider = ResolveProvider(configuration?.Data, new UnsupportedDataProvider());
            IPaymentsProvider paymentsProvider = ResolveProvider(configuration?.Payments, new UnsupportedPaymentsProvider());
            ILanguageProvider languageProvider = ResolveProvider(configuration?.LanguageProvider, new FallbackLanguageProvider());
            IPlayerProvider playerProvider = ResolveProvider(configuration?.Player, new UnsupportedPlayerProvider());
            ILeaderboardsProvider leaderboardsProvider = ResolveProvider(configuration?.Leaderboards, new UnsupportedLeaderboardsProvider());
            IFlagsProvider flagsProvider = ResolveProvider(configuration?.Flags, new UnsupportedFlagsProvider());
            ITimeProvider timeProvider = ResolveProvider(configuration?.TimeProvider, new FallbackTimeProvider());
            IGameEventsProvider gameEventsProvider = ResolveProvider(configuration?.GameEvents, new FallbackGameEventsProvider());
            IReviewProvider reviewProvider = ResolveProvider(configuration?.Review, new UnsupportedReviewProvider());
            IGameLabelProvider gameLabelProvider = ResolveProvider(configuration?.GameLabel, new UnsupportedGameLabelProvider());
            ILinksProvider linksProvider = ResolveProvider(configuration?.Links, new UnsupportedLinksProvider());

            bool pauseOnFocusLoss = configuration == null || configuration.PauseOnFocusLoss;
            PlatformId platformId = configuration == null ? PlatformId.Editor : configuration.Platform;

            _bridge = new WebBridge(_logger, platformId);
            AttachBridge(platformProvider, adsProvider, dataProvider, paymentsProvider, languageProvider, playerProvider, leaderboardsProvider, flagsProvider, timeProvider, gameEventsProvider, reviewProvider, gameLabelProvider, linksProvider);

#if UNITY_EDITOR
            if (settings.UsePrototypesInEditor)
            {
                PrototypeFactory prototypes = new PrototypeFactory(platformId, settings);
                platformProvider = prototypes.Platform(platformProvider);
                adsProvider = prototypes.Ads(adsProvider);
                dataProvider = prototypes.Data(dataProvider);
                paymentsProvider = prototypes.Payments(paymentsProvider);
                languageProvider = prototypes.Language(languageProvider);
                playerProvider = prototypes.Player(playerProvider);
                leaderboardsProvider = prototypes.Leaderboards(leaderboardsProvider);
                flagsProvider = prototypes.Flags(flagsProvider);
                timeProvider = prototypes.Time(timeProvider);
                gameEventsProvider = prototypes.GameEvents(gameEventsProvider);
                reviewProvider = prototypes.Review(reviewProvider);
                gameLabelProvider = prototypes.GameLabel(gameLabelProvider);
                linksProvider = prototypes.Links(linksProvider);
            }
#endif

            _pause = new PauseService(platformProvider, pauseOnFocusLoss, _logger);
            _time = new TimeService(timeProvider, _pause, settings.PauseTimeScale, _logger);
            _audio = new AudioService(platformProvider, _pause, settings.PauseAudio, _logger);
            _device = new DeviceService(platformProvider, _pause, settings.ShowCursorOnPause, settings.DisableEventSystemOnPause, _logger);
            _gameEvents = new GameEventsService(gameEventsProvider, _pause, _logger);
            _ads = new AdsService(adsProvider, _pause, _gameEvents, _logger);
            _data = new DataService(dataProvider, settings.AutosaveDelaySeconds, _logger, BackupStorage ?? new WebBackupStorage(), BackupKey(platformId));
            _player = new PlayerService(playerProvider, _data, _logger);
            _payments = new PaymentsService(paymentsProvider, _data, _pause, settings.Products, platformId, _logger);
            _language = new LanguageService(languageProvider, settings, configuration, _logger);
            _leaderboards = new LeaderboardsService(leaderboardsProvider, _player, settings.Leaderboards, platformId, _logger);
            _flags = new FlagsService(flagsProvider, settings.Flags, _logger);
            _review = new ReviewService(reviewProvider, _logger);
            _gameLabel = new GameLabelService(gameLabelProvider, _logger);
            _links = new LinksService(linksProvider, _logger);
            _platform = new PlatformService(platformProvider, _ads, _payments, _leaderboards, _player, _flags, _time, _review, _gameLabel, _links, _logger);

            _modules.Add(_platform);
            _modules.Add(_pause);
            _modules.Add(_time);
            _modules.Add(_audio);
            _modules.Add(_device);
            _modules.Add(_gameEvents);
            _modules.Add(_ads);
            _modules.Add(_data);
            _modules.Add(_player);
            _modules.Add(_payments);
            _modules.Add(_language);
            _modules.Add(_leaderboards);
            _modules.Add(_flags);
            _modules.Add(_review);
            _modules.Add(_gameLabel);
            _modules.Add(_links);

            foreach (ModuleBase module in _modules)
            {
                module.StateChanged += OnModuleStateChanged;
            }
        }

        internal static IBackupStorage BackupStorage { get; set; }

        public bool IsReady { get; private set; }

        public IAds Ads => _ads;
        public IData Data => _data;
        public IPayments Payments => _payments;
        public ILanguage Language => _language;
        public IPause Pause => _pause;
        public ITime Time => _time;
        public IAudio Audio => _audio;
        public IGameEvents GameEvents => _gameEvents;
        public ILeaderboards Leaderboards => _leaderboards;
        public IPlayer Player => _player;
        public IFlags Flags => _flags;
        public IPlatform Platform => _platform;
        public IDevice Device => _device;

        internal DeviceService DeviceInput => _device;
        public IReview Review => _review;
        public IGameLabel GameLabel => _gameLabel;
        public ILinks Links => _links;

        internal LogLevel LogLevel
        {
            get => _logger.Level;
            set => _logger.Level = value;
        }

        internal IReadOnlyList<ModuleBase> Modules => _modules;

        public void WhenReady(Action onReady)
        {
            if (onReady == null)
            {
                throw new ArgumentNullException(nameof(onReady));
            }

            if (IsReady)
            {
                onReady();
                return;
            }

            _readyCallbacks.Add(onReady);
        }

        internal void Initialize()
        {
            if (_initialized)
            {
                throw new InvalidOperationException(nameof(Initialize));
            }

            _initialized = true;
            _logger.Info("JTL SDK " + JTLSDK.Version + " starts on " + _platform.Current + ".");
            CreateRuntimeObject();
            _bridge.EventReceived += OnBridgeEvent;
            _bridge.Connect();

            foreach (ModuleBase module in _modules)
            {
                try
                {
                    module.Initialize();
                }
                catch (Exception exception)
                {
                    _logger.Error(module.ModuleName + " failed to initialize.");
                    _logger.Exception(exception);
                    module.MarkFailed();
                }
            }

            EvaluateReadiness();
        }

        internal void MarkUnsupported()
        {
            foreach (ModuleBase module in _modules)
            {
                module.MarkUnsupported();
            }
        }

        internal void Tick(float unscaledDeltaTime)
        {
            if (IsReady == false)
            {
                _initializationSeconds += unscaledDeltaTime;

                if (_initializationSeconds >= _settings.InitializationTimeoutSeconds)
                {
                    TimeOut();
                }
            }

            _data.Tick(unscaledDeltaTime);
            _payments.Tick(unscaledDeltaTime);
        }

        private static string BackupKey(PlatformId platform)
        {
            return "JTLSDK.Backup." + platform + "." + Application.productName;
        }

        private void OnBridgeEvent(BridgeEventCode code, BridgeResponse response)
        {
            HandleBridgeEvent(code);
        }

        internal void HandleBridgeEvent(BridgeEventCode code)
        {
            if (code == BridgeEventCode.PageHiding)
            {
                _data.FlushBeforeUnload();
            }
        }

        internal void HandleApplicationFocus(bool hasFocus)
        {
            _pause.HandleApplicationFocus(hasFocus);

            if (hasFocus == false)
            {
                _data.SaveIfDirty();
            }
        }

        internal void HandleApplicationPause(bool isPaused)
        {
            if (isPaused)
            {
                _data.SaveIfDirty();
            }
        }

        internal void HandleApplicationQuit()
        {
            _data.SaveIfDirty();
        }

        internal void HandleBehaviourDestroyed()
        {
            _behaviour = null;
            JTLSDK.Destroy();
        }

        internal void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            foreach (ModuleBase module in _modules)
            {
                module.StateChanged -= OnModuleStateChanged;
                module.Dispose();
            }

            _readyCallbacks.Clear();
            _bridge.EventReceived -= OnBridgeEvent;
            _bridge.Disconnect();

            if (_behaviour != null)
            {
                GameObject runtimeObject = _behaviour.gameObject;
                _behaviour.Bind(null);
                _behaviour = null;

                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(runtimeObject);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(runtimeObject);
                }
            }
        }

        private void AttachBridge(params object[] providers)
        {
            foreach (object provider in providers)
            {
                if (provider is IBridgeConsumer consumer)
                {
                    consumer.Attach(_bridge);
                }
            }
        }

        private T ResolveProvider<T>(T configured, T fallback) where T : class
        {
            return configured ?? fallback;
        }

        private void CreateRuntimeObject()
        {
            GameObject runtimeObject = new GameObject(RuntimeObjectName);

            if (Application.isPlaying)
            {
                runtimeObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.NotEditable;
                UnityEngine.Object.DontDestroyOnLoad(runtimeObject);
            }
            else
            {
                runtimeObject.hideFlags = HideFlags.HideAndDontSave;
            }

            _behaviour = runtimeObject.AddComponent<SdkRuntimeBehaviour>();
            _behaviour.Bind(this);
        }

        private void TimeOut()
        {
            foreach (ModuleBase module in _modules)
            {
                if (module.State == ModuleState.Pending)
                {
                    _logger.Warning(module.ModuleName + " did not initialize within " + _settings.InitializationTimeoutSeconds + " seconds.");
                    module.MarkFailed();
                }
            }
        }

        private void EvaluateReadiness()
        {
            if (IsReady)
            {
                return;
            }

            foreach (ModuleBase module in _modules)
            {
                if (module.State == ModuleState.Pending)
                {
                    return;
                }
            }

            IsReady = true;
            _logger.Info("Ready after " + _initializationSeconds.ToString("0.00") + " seconds. " + ModuleSummary());

            List<Action> callbacks = new List<Action>(_readyCallbacks);
            _readyCallbacks.Clear();

            foreach (Action callback in callbacks)
            {
                try
                {
                    callback();
                }
                catch (Exception exception)
                {
                    _logger.Exception(exception);
                }
            }
        }

        private string ModuleSummary()
        {
            List<string> unsupported = new List<string>();
            List<string> failed = new List<string>();

            foreach (ModuleBase module in _modules)
            {
                if (module.State == ModuleState.Unsupported)
                {
                    unsupported.Add(module.ModuleName);
                }
                else if (module.State == ModuleState.Failed)
                {
                    failed.Add(module.ModuleName);
                }
            }

            string summary = "Ready modules: " + (_modules.Count - unsupported.Count - failed.Count) + " of " + _modules.Count + ".";

            if (unsupported.Count > 0)
            {
                summary += " Not supported here: " + string.Join(", ", unsupported) + ".";
            }

            if (failed.Count > 0)
            {
                summary += " Failed: " + string.Join(", ", failed) + ".";
            }

            return summary;
        }

        private void OnModuleStateChanged(ModuleBase module)
        {
            _logger.Info(module.ModuleName + " is " + module.State + ".");
            EvaluateReadiness();
        }
    }
}
