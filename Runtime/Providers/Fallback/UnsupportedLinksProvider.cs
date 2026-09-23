using System;

namespace JTLStudio.SDK.Providers
{
    [Serializable]
    public class UnsupportedLinksProvider : ILinksProvider
    {
        public string Domain => "";
        public string DeveloperPageUrl => "";

        public void Initialize(Action<ProviderState> onInitialized)
        {
            onInitialized(ProviderState.Unsupported);
        }

        public string GamePageUrl(string gameId)
        {
            return "";
        }

        public void Open(string url)
        {
        }
    }
}
