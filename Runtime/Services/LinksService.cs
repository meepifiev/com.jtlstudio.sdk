using System;
using JTLStudio.SDK.Providers;

namespace JTLStudio.SDK.Services
{
    public class LinksService : ModuleBase, ILinks
    {
        private readonly ILinksProvider _provider;

        public LinksService(ILinksProvider provider, SdkLogger logger) : base(logger)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public string Domain => State == ModuleState.Ready ? _provider.Domain ?? "" : "";

        internal override string ModuleName => "Links";

        public void Open(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException(nameof(url));
            }

            if (Reject(url))
            {
                return;
            }

            Logger.Info("Opening " + url + ".");
            _provider.Open(url);
        }

        public void OpenOnPlatformDomain(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException(nameof(url));
            }

            Open(OnDomain(url));
        }

        public void OpenDeveloperPage()
        {
            string url = State == ModuleState.Ready ? _provider.DeveloperPageUrl : "";

            if (string.IsNullOrEmpty(url))
            {
                Logger.Warning("The developer page is not available on this platform.");
                return;
            }

            Open(url);
        }

        public void OpenGamePage(string gameId)
        {
            if (string.IsNullOrEmpty(gameId))
            {
                throw new ArgumentException(nameof(gameId));
            }

            string url = State == ModuleState.Ready ? _provider.GamePageUrl(gameId) : "";

            if (string.IsNullOrEmpty(url))
            {
                Logger.Warning("Game pages are not available on this platform.");
                return;
            }

            Open(url);
        }

        internal override void Initialize()
        {
            _provider.Initialize(CompleteInitialization);
        }

        private string OnDomain(string url)
        {
            string domain = Domain;

            if (string.IsNullOrEmpty(domain))
            {
                return url;
            }

            return url.Replace("{domain}", domain);
        }

        private bool Reject(string url)
        {
            if (State == ModuleState.Ready)
            {
                return false;
            }

            Logger.Warning("Links are not available on this platform, '" + url + "' was not opened.");
            return true;
        }
    }
}
