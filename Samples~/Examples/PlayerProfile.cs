using UnityEngine;

namespace JTLStudio.SDK.Samples.Examples
{
    public class PlayerProfile : MonoBehaviour
    {
        private void Start()
        {
            JTLSDK.WhenReady(ShowProfile);
        }

        public void OnSignInClicked()
        {
            JTLSDK.Player.Authorize(success => ShowProfile());
        }

        private void ShowProfile()
        {
            if (JTLSDK.Player.IsAuthorized == false)
            {
                Debug.Log("Guest. Show the sign-in button.");
                return;
            }

            Debug.Log("Hello, " + JTLSDK.Player.Name + " (" + JTLSDK.Player.Id + ")");
        }
    }
}
