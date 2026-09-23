using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace JTLStudio.SDK.Bridge
{
    public static class WebStorage
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void JTLSDK_WriteBackup(string key, string value);
        [DllImport("__Internal")] private static extern string JTLSDK_ReadBackup(string key);
        [DllImport("__Internal")] private static extern void JTLSDK_ClearBackup(string key);
#endif

        public static bool IsAvailable
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        public static void Write(string key, string value)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            JTLSDK_WriteBackup(key, value);
#else
            PlayerPrefs.SetString(key, value);
#endif
        }

        public static string Read(string key)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return JTLSDK_ReadBackup(key) ?? "";
#else
            return PlayerPrefs.GetString(key, "");
#endif
        }

        public static void Clear(string key)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            JTLSDK_ClearBackup(key);
#else
            PlayerPrefs.DeleteKey(key);
#endif
        }
    }
}
