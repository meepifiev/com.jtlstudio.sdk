namespace JTLStudio.SDK
{
    public interface ILinks : IModule
    {
        string Domain { get; }

        void Open(string url);
        void OpenOnPlatformDomain(string url);
        void OpenDeveloperPage();
        void OpenGamePage(string gameId);
    }
}
