using UnityEngine;

namespace JTLStudio.SDK.Samples.Examples
{
    public class GameplayEvents : MonoBehaviour
    {
        public void OnLevelStarted()
        {
            JTLSDK.GameEvents.GameplayStarted();
        }

        public void OnPlayerDied()
        {
            JTLSDK.GameEvents.GameplayStopped();
        }

        public void OnRestartClicked()
        {
            JTLSDK.GameEvents.GameplayRestarted();
        }
    }
}
