using System.Collections.Generic;
using JTLStudio.SDK.Services;

namespace JTLStudio.SDK.Tests.Fakes
{
    public class FakeBackupStorage : IBackupStorage
    {
        private readonly Dictionary<string, string> _values = new Dictionary<string, string>();

        public int WriteCount { get; private set; }
        public int ClearCount { get; private set; }
        public bool HasValue => _values.Count > 0;

        public void Write(string key, string value)
        {
            WriteCount++;
            _values[key] = value;
        }

        public string Read(string key)
        {
            return _values.TryGetValue(key, out string value) ? value : "";
        }

        public void Clear(string key)
        {
            ClearCount++;
            _values.Remove(key);
        }

        public void Seed(string key, string value)
        {
            _values[key] = value;
        }

        public string Key(PlatformId platform)
        {
            return "JTLSDK.Backup." + platform + "." + UnityEngine.Application.productName;
        }
    }
}
