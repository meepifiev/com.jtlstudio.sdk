using System;
using System.Collections.Generic;
using JTLStudio.SDK.Providers;

namespace JTLStudio.SDK.Tests.Fakes
{
    public class FakeFlagsProvider : IFlagsProvider
    {
        public Dictionary<string, string> Values { get; } = new Dictionary<string, string>();
        public IReadOnlyList<FlagDefinition> ConfiguredFlags { get; private set; }

        public void Initialize(Action<ProviderState> onInitialized)
        {
            onInitialized(ProviderState.Ready);
        }

        public void Configure(IReadOnlyList<FlagDefinition> flags)
        {
            ConfiguredFlags = flags;
        }

        public bool TryGetValue(string key, out string value)
        {
            return Values.TryGetValue(key, out value);
        }
    }
}
