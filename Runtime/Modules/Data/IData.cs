using System;

namespace JTLStudio.SDK
{
    public interface IData : IModule
    {
        DataState LoadState { get; }
        bool IsDirty { get; }

        event Action Loaded;
        event Action<bool> Saved;

        bool HasKey(string key);
        int GetInt(string key, int defaultValue = 0);
        float GetFloat(string key, float defaultValue = 0f);
        bool GetBool(string key, bool defaultValue = false);
        string GetString(string key, string defaultValue = "");
        T GetObject<T>(string key, T defaultValue = null) where T : class;

        void SetInt(string key, int value, bool important = true);
        void SetFloat(string key, float value, bool important = true);
        void SetBool(string key, bool value, bool important = true);
        void SetString(string key, string value, bool important = true);
        void SetObject<T>(string key, T value, bool important = true) where T : class;

        void DeleteKey(string key);
        void DeleteAll();

        void Save(Action<bool> onSaved = null);
    }
}
