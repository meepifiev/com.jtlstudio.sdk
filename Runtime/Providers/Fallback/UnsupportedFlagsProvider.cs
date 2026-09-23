using System;
using System.Collections.Generic;

namespace JTLStudio.SDK.Providers
{
    [Serializable]
    public class UnsupportedFlagsProvider : IFlagsProvider
    {
        public void Initialize(Action<ProviderState> onInitialized)
        {
            onInitialized(ProviderState.Unsupported);
        }

        public void Configure(IReadOnlyList<FlagDefinition> flags)
        {
        }

        public bool TryGetValue(string key, out string value)
        {
            value = "";
            return false;
        }
    }
}
