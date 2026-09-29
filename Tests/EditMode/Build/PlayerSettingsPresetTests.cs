using System;
using JTLStudio.SDK.Editor.Configuration;
using NUnit.Framework;
using UnityEditor;

namespace JTLStudio.SDK.Tests.Build
{
    public class PlayerSettingsPresetTests
    {
        private readonly PlayerSettingsPresetService _service = new PlayerSettingsPresetService();

        [Test]
        public void EveryExceptionValueMapsBothWays()
        {
            foreach (ExceptionSupport value in Enum.GetValues(typeof(ExceptionSupport)))
            {
                Assert.AreEqual(value, _service.FromUnity(_service.ToUnity(value)), value.ToString());
            }
        }

        [Test]
        public void EveryUnityExceptionValueIsCovered()
        {
            foreach (WebGLExceptionSupport value in Enum.GetValues(typeof(WebGLExceptionSupport)))
            {
                Assert.DoesNotThrow(() => _service.FromUnity(value), value.ToString());
            }
        }

        [Test]
        public void ExceptionsAreNotAppliedUntilTheBoxIsTicked()
        {
            PlayerSettingsPreset preset = new PlayerSettingsPreset();

            Assert.IsFalse(preset.ApplyExceptions);
            Assert.AreEqual(ExceptionSupport.ExplicitlyThrownExceptionsOnly, preset.Exceptions);
        }

        [Test]
        public void PlatformDefaultsKeepExplicitlyThrownExceptions()
        {
            PlayerSettingsPreset preset = new PlayerSettingsPreset();
            PresetDefaults defaults = new PresetDefaults();

            defaults.Apply(preset, PlatformId.YandexGames);
            Assert.AreEqual(ExceptionSupport.ExplicitlyThrownExceptionsOnly, preset.Exceptions);

            defaults.Apply(preset, PlatformId.YouTubePlayables);
            Assert.AreEqual(ExceptionSupport.ExplicitlyThrownExceptionsOnly, preset.Exceptions);
        }

        [Test]
        public void CurrentExceptionsReadsTheProject()
        {
            Assert.AreEqual(_service.FromUnity(PlayerSettings.WebGL.exceptionSupport), _service.CurrentExceptions());
        }
    }
}
