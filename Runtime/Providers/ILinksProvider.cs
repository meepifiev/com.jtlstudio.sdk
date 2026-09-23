namespace JTLStudio.SDK.Providers
{
    public interface ILinksProvider : IProvider
    {
        string Domain { get; }
        string DeveloperPageUrl { get; }

        string GamePageUrl(string gameId);
        void Open(string url);
    }
}
