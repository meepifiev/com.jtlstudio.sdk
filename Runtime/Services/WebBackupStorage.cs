using JTLStudio.SDK.Bridge;

namespace JTLStudio.SDK.Services
{
    public class WebBackupStorage : IBackupStorage
    {
        public void Write(string key, string value)
        {
            WebStorage.Write(key, value);
        }

        public string Read(string key)
        {
            return WebStorage.Read(key);
        }

        public void Clear(string key)
        {
            WebStorage.Clear(key);
        }
    }
}
