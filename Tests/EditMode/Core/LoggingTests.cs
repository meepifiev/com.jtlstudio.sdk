using System.Collections.Generic;
using JTLStudio.SDK.Tests.Fakes;
using NUnit.Framework;
using UnityEngine;

namespace JTLStudio.SDK.Tests.Core
{
    public class LoggingTests
    {
        private const string Prefix = "[JTL SDK]";

        private TestSettingsBuilder _builder;
        private readonly List<string> _messages = new List<string>();

        [SetUp]
        public void SetUp()
        {
            JTLSDK.Destroy();
            _builder = new TestSettingsBuilder();
            _messages.Clear();
            Application.logMessageReceived += Record;
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceived -= Record;
            JTLSDK.Destroy();
            _builder.Cleanup();
        }

        [Test]
        public void SettingsLevelAllWritesToTheConsole()
        {
            _builder.LogLevel = LogLevel.All;

            JTLSDK.Create(_builder.Build());

            Assert.Greater(_messages.Count, 0);
        }

        [Test]
        public void SettingsLevelNoneKeepsTheConsoleClean()
        {
            _builder.LogLevel = LogLevel.None;

            JTLSDK.Create(_builder.Build());
            JTLSDK.GameEvents.GameReady();
            JTLSDK.Ads.ShowInterstitial(result => { });

            Assert.AreEqual(0, _messages.Count);
        }

        [Test]
        public void LevelFromCodeWinsOverSettings()
        {
            _builder.LogLevel = LogLevel.All;
            JTLSDK.LogLevel = LogLevel.None;

            JTLSDK.Create(_builder.Build());

            Assert.AreEqual(LogLevel.None, JTLSDK.LogLevel);
            Assert.AreEqual(0, _messages.Count);
        }

        [Test]
        public void LevelChangesWhileTheGameRuns()
        {
            _builder.LogLevel = LogLevel.None;
            JTLSDK.Create(_builder.Build());

            JTLSDK.LogLevel = LogLevel.All;
            JTLSDK.GameEvents.GameReady();

            Assert.Greater(_messages.Count, 0);
        }

        [Test]
        public void MissingSettingsErrorRespectsTheLevel()
        {
            JTLSDK.LogLevel = LogLevel.None;

            Assert.AreEqual(LogLevel.None, JTLSDK.LogLevel);
            Assert.AreEqual(0, _messages.Count);
        }

        private void Record(string message, string stackTrace, LogType type)
        {
            if (message.StartsWith(Prefix))
            {
                _messages.Add(message);
            }
        }
    }
}
