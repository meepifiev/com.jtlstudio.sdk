namespace JTLStudio.SDK.Services
{
    public interface IBackupStorage
    {
        void Write(string key, string value);
        string Read(string key);
        void Clear(string key);
    }
}
