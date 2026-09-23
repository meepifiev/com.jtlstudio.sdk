using System;
using UnityEngine;

namespace JTLStudio.SDK
{
    public class SdkLogger
    {
        private const string Prefix = "[JTL SDK] ";

        public SdkLogger(LogLevel level)
        {
            Level = level;
        }

        public LogLevel Level { get; set; }

        public void Info(string message)
        {
            if (Level >= LogLevel.All)
            {
                Debug.Log(Prefix + message);
            }
        }

        public void Warning(string message)
        {
            if (Level >= LogLevel.ErrorsAndWarnings)
            {
                Debug.LogWarning(Prefix + message);
            }
        }

        public void Error(string message)
        {
            if (Level >= LogLevel.Errors)
            {
                Debug.LogError(Prefix + message);
            }
        }

        public void Exception(Exception exception)
        {
            if (Level >= LogLevel.Errors)
            {
                Debug.LogException(exception);
            }
        }
    }
}
