using UnityEngine;

namespace JTLStudio.SDK.Samples.Examples
{
    public class VolumeSettings : MonoBehaviour
    {
        private const string VolumeKey = "Volume";

        private void Start()
        {
            JTLSDK.WhenReady(OnReady);
        }

        private void OnDestroy()
        {
            if (JTLSDK.IsCreated)
            {
                JTLSDK.Audio.PlatformMuteChanged -= OnPlatformMuteChanged;
            }
        }

        public void OnVolumeChanged(float volume)
        {
            JTLSDK.Audio.Volume = volume;
            JTLSDK.Data.SetFloat(VolumeKey, volume);
        }

        private void OnReady()
        {
            JTLSDK.Audio.Volume = JTLSDK.Data.GetFloat(VolumeKey, 1f);
            JTLSDK.Audio.PlatformMuteChanged += OnPlatformMuteChanged;
        }

        private void OnPlatformMuteChanged(bool muted)
        {
            Debug.Log(muted ? "The portal muted the game." : "The portal unmuted the game.");
        }
    }
}
