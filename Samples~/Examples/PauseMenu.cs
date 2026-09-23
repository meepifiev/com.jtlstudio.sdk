using UnityEngine;

namespace JTLStudio.SDK.Samples.Examples
{
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameObject _menu;

        private void Start()
        {
            JTLSDK.WhenReady(() => JTLSDK.Pause.Changed += OnSystemPauseChanged);
        }

        private void OnDestroy()
        {
            if (JTLSDK.IsCreated)
            {
                JTLSDK.Pause.Changed -= OnSystemPauseChanged;
            }
        }

        public void Open()
        {
            _menu.SetActive(true);
            JTLSDK.Time.Scale = 0f;
            JTLSDK.Audio.Paused = true;
            JTLSDK.GameEvents.GameplayStopped();
        }

        public void Close()
        {
            _menu.SetActive(false);
            JTLSDK.Time.Scale = 1f;
            JTLSDK.Audio.Paused = false;
            JTLSDK.GameEvents.GameplayStarted();
        }

        private void OnSystemPauseChanged(bool paused)
        {
            Debug.Log(paused ? "Paused by an ad, a purchase or the portal." : "Resumed.");
        }
    }
}
