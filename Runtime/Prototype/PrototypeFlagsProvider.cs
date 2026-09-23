#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using JTLStudio.SDK.Providers;

namespace JTLStudio.SDK.Prototype
{
    public class PrototypeFlagsProvider : IFlagsProvider
    {
        private readonly PrototypeSimulationSettings _settings;

        public PrototypeFlagsProvider(PrototypeSimulationSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public void Initialize(Action<ProviderState> onInitialized)
        {
            onInitialized(ProviderState.Ready);
        }

        public void Configure(IReadOnlyList<FlagDefinition> flags)
        {
        }

        public bool TryGetValue(string key, out string value)
        {
            return _settings.TryGetFlag(key, out value);
        }
    }
}
#endif
