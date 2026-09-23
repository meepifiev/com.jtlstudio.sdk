using System;
using System.Collections.Generic;
using JTLStudio.SDK.Bridge;
using JTLStudio.SDK.Providers;

namespace JTLStudio.SDK.YandexGames
{
    [Serializable]
    [ProviderPlatforms(PlatformId.YandexGames)]
    public class YandexGamesFlagsProvider : BridgeProviderBase, IFlagsProvider
    {
        private readonly Dictionary<string, string> _flags = new Dictionary<string, string>();
        private readonly Dictionary<string, object> _defaults = new Dictionary<string, object>();

        public void Configure(IReadOnlyList<FlagDefinition> flags)
        {
            _defaults.Clear();

            foreach (FlagDefinition definition in flags)
            {
                if (string.IsNullOrEmpty(definition.Key) == false)
                {
                    _defaults[definition.Key] = definition.DefaultValue;
                }
            }
        }

        public void Initialize(Action<ProviderState> onInitialized)
        {
            InitializeWith("flags", "get", new BridgePayload().Set("defaults", _defaults), onInitialized, response =>
            {
                _flags.Clear();

                foreach (KeyValuePair<string, object> pair in AsObject(response.Values.TryGetValue("flags", out object flags) ? flags : null))
                {
                    _flags[pair.Key] = Convert.ToString(pair.Value, System.Globalization.CultureInfo.InvariantCulture);
                }
            });
        }

        public bool TryGetValue(string key, out string value)
        {
            return _flags.TryGetValue(key, out value);
        }
    }
}
