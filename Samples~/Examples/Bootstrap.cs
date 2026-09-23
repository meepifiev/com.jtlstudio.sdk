using UnityEngine;

namespace JTLStudio.SDK.Samples.Examples
{
    public class Bootstrap : MonoBehaviour
    {
        private void Awake()
        {
            if (JTLSDK.IsCreated == false)
            {
                JTLSDK.Create();
            }

            JTLSDK.WhenReady(OnReady);
        }

        private void OnReady()
        {
            Debug.Log("Platform: " + JTLSDK.Platform.Current + ", language: " + JTLSDK.Language.Current);
            JTLSDK.GameEvents.GameReady();
        }
    }
}
