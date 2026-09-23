using System;
using UnityEngine;

namespace JTLStudio.SDK
{
    public static class JTLSDK
    {
        public const string Version = "1.0.0";

        private const string NotCreated = "JTLSDK.Create() must run before the SDK is used.";

        private static SdkInstance _instance;
        private static LogLevel? _logLevelOverride;

        public static bool IsCreated => _instance != null;
        public static bool IsReady => IsCreated && _instance.IsReady;

        public static IAds Ads => Instance.Ads;
        public static IData Data => Instance.Data;
        public static IPayments Payments => Instance.Payments;
        public static ILanguage Language => Instance.Language;
        public static IPause Pause => Instance.Pause;
        public static ITime Time => Instance.Time;
        public static IAudio Audio => Instance.Audio;
        public static IGameEvents GameEvents => Instance.GameEvents;
        public static ILeaderboards Leaderboards => Instance.Leaderboards;
        public static IPlayer Player => Instance.Player;
        public static IFlags Flags => Instance.Flags;
        public static IPlatform Platform => Instance.Platform;
        public static IDevice Device => Instance.Device;
        public static IReview Review => Instance.Review;
        public static IGameLabel GameLabel => Instance.GameLabel;
        public static ILinks Links => Instance.Links;

        public static LogLevel LogLevel
        {
            get => _logLevelOverride ?? (_instance != null ? _instance.LogLevel : SettingsLogLevel());
            set
            {
                _logLevelOverride = value;

                if (_instance != null)
                {
                    _instance.LogLevel = value;
                }
            }
        }

        internal static SdkInstance Current => _instance;

        internal static void HandleBridgeEvent(Bridge.BridgeEventCode code)
        {
            _instance?.HandleBridgeEvent(code);
        }

        private static SdkInstance Instance => _instance ?? throw new InvalidOperationException(NotCreated);

        public static void Create()
        {
            JTLSDKSettings settings = Resources.Load<JTLSDKSettings>(JTLSDKSettings.ResourcePath);

            if (settings == null)
            {
                if (LogLevel >= LogLevel.Errors)
                {
                    Debug.LogError("[JTL SDK] Settings were not found at Resources/" + JTLSDKSettings.ResourcePath + ". Open JTL SDK > Toolkit and create a configuration. The SDK runs without a platform until then.");
                }

                settings = ScriptableObject.CreateInstance<JTLSDKSettings>();
            }

            Create(settings);
        }

        public static void WhenReady(Action onReady)
        {
            if (_instance == null)
            {
                throw new InvalidOperationException(NotCreated);
            }

            _instance.WhenReady(onReady);
        }

        internal static void Create(JTLSDKSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (_instance != null)
            {
                throw new InvalidOperationException(nameof(Create));
            }

            SdkInstance instance = new SdkInstance(settings);

            if (_logLevelOverride.HasValue)
            {
                instance.LogLevel = _logLevelOverride.Value;
            }

            _instance = instance;

            try
            {
                instance.Initialize();
            }
            catch
            {
                _instance = null;
                instance.Dispose();
                throw;
            }
        }

        private static LogLevel SettingsLogLevel()
        {
            JTLSDKSettings settings = Resources.Load<JTLSDKSettings>(JTLSDKSettings.ResourcePath);
            return settings == null ? LogLevel.All : settings.LogLevel;
        }

        internal static void Destroy()
        {
            SdkInstance instance = _instance;
            _instance = null;
            _logLevelOverride = null;
            instance?.Dispose();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
        }
    }
}
