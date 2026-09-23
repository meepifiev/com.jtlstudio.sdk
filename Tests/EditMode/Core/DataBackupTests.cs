using System.Collections.Generic;
using JTLStudio.SDK.Bridge;
using JTLStudio.SDK.Providers;
using JTLStudio.SDK.Tests.Fakes;
using NUnit.Framework;

namespace JTLStudio.SDK.Tests.Core
{
    public class DataBackupTests
    {
        private TestSettingsBuilder _builder;
        private FakeDataProvider _data;
        private FakeBackupStorage _backup;

        [SetUp]
        public void SetUp()
        {
            JTLSDK.Destroy();
            _data = new FakeDataProvider();
            _backup = new FakeBackupStorage();
            SdkInstance.BackupStorage = _backup;
            _builder = new TestSettingsBuilder { Data = _data };
        }

        [TearDown]
        public void TearDown()
        {
            SdkInstance.BackupStorage = null;
            _builder.Cleanup();
        }

        [Test]
        public void PageHidingWritesBackupAndSaves()
        {
            JTLSDK.Create(_builder.Build());
            JTLSDK.Data.SetInt("Money", 7);

            JTLSDK.HandleBridgeEvent(BridgeEventCode.PageHiding);

            Assert.AreEqual(1, _backup.WriteCount);
            Assert.AreEqual(1, _data.SaveCount);
        }

        [Test]
        public void CleanDataIsNotBackedUp()
        {
            JTLSDK.Create(_builder.Build());

            JTLSDK.HandleBridgeEvent(BridgeEventCode.PageHiding);

            Assert.AreEqual(0, _backup.WriteCount);
            Assert.AreEqual(0, _data.SaveCount);
        }

        [Test]
        public void SuccessfulSaveClearsBackup()
        {
            JTLSDK.Create(_builder.Build());
            JTLSDK.Data.SetInt("Money", 7);
            JTLSDK.HandleBridgeEvent(BridgeEventCode.PageHiding);

            Assert.IsFalse(_backup.HasValue);
        }

        [Test]
        public void NewerBackupIsRestoredWhenPlatformSaveWasLost()
        {
            _data.SaveSucceeds = false;
            JTLSDK.Create(_builder.Build());
            JTLSDK.Data.SetInt("Money", 42);
            JTLSDK.HandleBridgeEvent(BridgeEventCode.PageHiding);

            Assert.IsTrue(_backup.HasValue);

            JTLSDK.Destroy();
            _data.SaveSucceeds = true;
            _data.LoadResult = DataLoadResult.Empty;
            JTLSDK.Create(_builder.Build());

            Assert.AreEqual(42, JTLSDK.Data.GetInt("Money"));
            Assert.AreEqual(DataState.Loaded, JTLSDK.Data.LoadState);
            StringAssert.Contains("42", _data.LastSaved);
            Assert.IsFalse(JTLSDK.Data.IsDirty);
            Assert.IsFalse(_backup.HasValue);
        }

        [Test]
        public void OlderBackupIsIgnoredAndCleared()
        {
            JTLSDK.Create(_builder.Build());
            JTLSDK.Data.SetInt("Money", 1);
            JTLSDK.Data.Save();
            string saved = _data.LastSaved;
            JTLSDK.Destroy();

            _backup.Seed(_backup.Key(_builder.Platform.Platform), saved);
            _data.LoadResult = DataLoadResult.Loaded;
            _data.LoadPayload = saved;

            JTLSDK.Create(_builder.Build());

            Assert.AreEqual(1, JTLSDK.Data.GetInt("Money"));
            Assert.IsFalse(JTLSDK.Data.IsDirty);
            Assert.IsFalse(_backup.HasValue);
        }

        [Test]
        public void SaveDuringSaveIsQueuedInsteadOfRefused()
        {
            _data.CompleteSaveImmediately = false;
            JTLSDK.Create(_builder.Build());
            JTLSDK.Data.SetInt("Money", 1);

            List<bool> results = new List<bool>();
            JTLSDK.Data.Save(results.Add);
            JTLSDK.Data.SetInt("Money", 2);
            JTLSDK.Data.Save(results.Add);

            Assert.AreEqual(0, results.Count);

            _data.CompletePendingSave(true);

            Assert.AreEqual(1, results.Count);
            Assert.IsTrue(results[0]);

            _data.CompletePendingSave(true);

            Assert.AreEqual(2, results.Count);
            Assert.IsTrue(results[1]);
            Assert.IsFalse(JTLSDK.Data.IsDirty);
        }
    }
}
