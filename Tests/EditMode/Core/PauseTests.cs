using System;
using JTLStudio.SDK.Services;
using JTLStudio.SDK.Tests.Fakes;
using NUnit.Framework;
using UnityEngine;

namespace JTLStudio.SDK.Tests.Core
{
    public class PauseTests
    {
        private const string EventSystemTypeName = "UnityEngine.EventSystems.EventSystem, UnityEngine.UI";

        private TestSettingsBuilder _builder;
        private GameObject _eventSystemObject;

        private static PauseService Pause => (PauseService)JTLSDK.Pause;

        [SetUp]
        public void SetUp()
        {
            JTLSDK.Destroy();
            _builder = new TestSettingsBuilder();
        }

        [TearDown]
        public void TearDown()
        {
            JTLSDK.Destroy();

            if (_eventSystemObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_eventSystemObject);
            }

            _builder.Cleanup();
        }

        [Test]
        public void HoldPausesAndDisposeResumes()
        {
            JTLSDK.Create(_builder.Build());
            int changes = 0;
            JTLSDK.Pause.Changed += _ => changes++;

            IDisposable hold = Pause.Hold(PauseSources.Advertisement);

            Assert.IsTrue(JTLSDK.Pause.IsPaused);
            Assert.AreEqual(1, changes);

            hold.Dispose();

            Assert.IsFalse(JTLSDK.Pause.IsPaused);
            Assert.AreEqual(2, changes);
        }

        [Test]
        public void SameSourceTwiceChangesOnce()
        {
            JTLSDK.Create(_builder.Build());
            int changes = 0;
            JTLSDK.Pause.Changed += _ => changes++;

            Pause.Set(PauseSources.Focus, true);
            Pause.Set(PauseSources.Focus, true);

            Assert.AreEqual(1, changes);
            Assert.AreEqual(1, JTLSDK.Pause.Sources.Count);
        }

        [Test]
        public void ResumesOnlyWhenEverySourceReleased()
        {
            JTLSDK.Create(_builder.Build());
            Pause.Set(PauseSources.Advertisement, true);
            _builder.Platform.RequestPause(true);

            Pause.Set(PauseSources.Advertisement, false);
            Assert.IsTrue(JTLSDK.Pause.IsPaused);

            _builder.Platform.RequestPause(false);
            Assert.IsFalse(JTLSDK.Pause.IsPaused);
        }

        [Test]
        public void TimeScaleFollowsPauseAndKeepsGameValue()
        {
            JTLSDK.Create(_builder.Build());
            JTLSDK.Time.Scale = 0.3f;
            Assert.AreEqual(0.3f, Time.timeScale, 0.0001f);

            _builder.Platform.RequestPause(true);
            Assert.AreEqual(0f, Time.timeScale, 0.0001f);
            Assert.AreEqual(0.3f, JTLSDK.Time.Scale, 0.0001f);

            JTLSDK.Time.Scale = 0f;
            _builder.Platform.RequestPause(false);
            Assert.AreEqual(0f, Time.timeScale, 0.0001f);
        }

        [Test]
        public void TimeScaleOptionOffKeepsTimeRunning()
        {
            _builder.PauseTimeScale = false;
            JTLSDK.Create(_builder.Build());

            _builder.Platform.RequestPause(true);

            Assert.AreEqual(1f, Time.timeScale, 0.0001f);
        }

        [Test]
        public void AudioIsSilentWhilePaused()
        {
            JTLSDK.Create(_builder.Build());
            JTLSDK.Audio.Volume = 0.8f;
            Assert.AreEqual(0.8f, AudioListener.volume, 0.0001f);

            _builder.Platform.RequestPause(true);
            Assert.AreEqual(0f, AudioListener.volume, 0.0001f);
            Assert.IsTrue(AudioListener.pause);
            Assert.AreEqual(0.8f, JTLSDK.Audio.Volume, 0.0001f);

            _builder.Platform.RequestPause(false);
            Assert.AreEqual(0.8f, AudioListener.volume, 0.0001f);
            Assert.IsFalse(AudioListener.pause);
        }

        [Test]
        public void GameAudioPauseSurvivesSystemPause()
        {
            JTLSDK.Create(_builder.Build());
            JTLSDK.Audio.Paused = true;

            _builder.Platform.RequestPause(true);
            _builder.Platform.RequestPause(false);

            Assert.IsTrue(AudioListener.pause);

            JTLSDK.Audio.Paused = false;
            Assert.IsFalse(AudioListener.pause);
        }

        [Test]
        public void AudioOptionOffKeepsAudioPlaying()
        {
            _builder.PauseAudio = false;
            JTLSDK.Create(_builder.Build());
            JTLSDK.Audio.Volume = 0.8f;

            _builder.Platform.RequestPause(true);

            Assert.IsFalse(AudioListener.pause);
            Assert.AreEqual(0.8f, AudioListener.volume, 0.0001f);
        }

        [Test]
        public void EventSystemIsDisabledWhilePausedAndRestored()
        {
            Behaviour eventSystem = CreateEventSystem(true);
            JTLSDK.Create(_builder.Build());

            _builder.Platform.RequestPause(true);
            Assert.IsFalse(eventSystem.enabled);

            _builder.Platform.RequestPause(false);
            Assert.IsTrue(eventSystem.enabled);
        }

        [Test]
        public void EventSystemDisabledByGameStaysDisabled()
        {
            Behaviour eventSystem = CreateEventSystem(false);
            JTLSDK.Create(_builder.Build());

            _builder.Platform.RequestPause(true);
            _builder.Platform.RequestPause(false);

            Assert.IsFalse(eventSystem.enabled);
        }

        [Test]
        public void EventSystemOptionOffLeavesEventSystemEnabled()
        {
            _builder.DisableEventSystemOnPause = false;
            Behaviour eventSystem = CreateEventSystem(true);
            JTLSDK.Create(_builder.Build());

            _builder.Platform.RequestPause(true);

            Assert.IsTrue(eventSystem.enabled);
        }

        [Test]
        public void PlatformPauseRequestUsesPlatformSource()
        {
            JTLSDK.Create(_builder.Build());
            _builder.Platform.RequestPause(true);

            Assert.IsTrue(JTLSDK.Pause.IsPaused);
            CollectionAssert.Contains(JTLSDK.Pause.Sources, PauseSources.Platform);

            _builder.Platform.RequestPause(false);

            Assert.IsFalse(JTLSDK.Pause.IsPaused);
        }

        [Test]
        public void ContinuePromptKeepsPlatformPause()
        {
            JTLSDK.Create(_builder.Build());
            _builder.Platform.RequestPause(true);
            bool continued = false;

            JTLSDK.Pause.ShowContinuePrompt(() => continued = true);

            Assert.IsTrue(continued);
            Assert.IsTrue(JTLSDK.Pause.IsPaused);
            Assert.AreEqual(1, _builder.Platform.ContinuePromptCount);

            _builder.Platform.RequestPause(false);

            Assert.IsFalse(JTLSDK.Pause.IsPaused);
        }

        [Test]
        public void GameplaySuspendsDuringPauseAndResumes()
        {
            JTLSDK.Create(_builder.Build());
            JTLSDK.GameEvents.GameplayStarted();
            Assert.IsTrue(JTLSDK.GameEvents.IsGameplayActive);

            _builder.Platform.RequestPause(true);
            Assert.IsFalse(JTLSDK.GameEvents.IsGameplayActive);

            _builder.Platform.RequestPause(false);
            Assert.IsTrue(JTLSDK.GameEvents.IsGameplayActive);
        }

        private Behaviour CreateEventSystem(bool enabled)
        {
            Type eventSystemType = Type.GetType(EventSystemTypeName);

            if (eventSystemType == null)
            {
                Assert.Ignore("UGUI is not installed.");
            }

            _eventSystemObject = new GameObject("EventSystem");
            Behaviour eventSystem = (Behaviour)_eventSystemObject.AddComponent(eventSystemType);
            eventSystem.enabled = enabled;
            return eventSystem;
        }
    }
}
