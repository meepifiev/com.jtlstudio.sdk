#if UNITY_EDITOR
using System;
using JTLStudio.SDK.Providers;
using UnityEngine;

namespace JTLStudio.SDK.Prototype
{
    public class PrototypeLinksProvider : ILinksProvider
    {
        private readonly ILinksProvider _platform;

        public PrototypeLinksProvider(ILinksProvider platform)
        {
            _platform = platform;
        }

        public string Domain => "ru";

        public string DeveloperPageUrl => _platform != null && string.IsNullOrEmpty(_platform.DeveloperPageUrl) == false
            ? _platform.DeveloperPageUrl
            : "https://yandex.ru/games/developer?name=developer";

        public void Initialize(Action<ProviderState> onInitialized)
        {
            onInitialized(ProviderState.Ready);
        }

        public string GamePageUrl(string gameId)
        {
            return "https://yandex.ru/games/app/" + gameId;
        }

        public void Open(string url)
        {
            Application.OpenURL(url);
        }
    }
}
#endif
