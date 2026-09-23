using System;
using JTLStudio.SDK.Bridge;
using JTLStudio.SDK.Providers;
using UnityEngine;

namespace JTLStudio.SDK.YandexGames
{
    [Serializable]
    [ProviderPlatforms(PlatformId.YandexGames)]
    public class YandexGamesLinksProvider : BridgeProviderBase, ILinksProvider
    {
        [SerializeField] private string _developerName = "";

        private string _domain = "ru";

        public string Domain => _domain;

        public string DeveloperPageUrl => string.IsNullOrEmpty(_developerName)
            ? ""
            : "https://yandex." + _domain + "/games/developer?name=" + UnityEngine.Networking.UnityWebRequest.EscapeURL(_developerName);

        public void Initialize(Action<ProviderState> onInitialized)
        {
            InitializeWith("platform", "initialize", onInitialized, response =>
            {
                string domain = response.GetString("domain");

                if (string.IsNullOrEmpty(domain) == false)
                {
                    _domain = domain;
                }
            });
        }

        public string GamePageUrl(string gameId)
        {
            return "https://yandex." + _domain + "/games/app/" + gameId;
        }

        public void Open(string url)
        {
            Call("links", "open", new BridgePayload().Set("url", url), _ => { });
        }
    }
}
