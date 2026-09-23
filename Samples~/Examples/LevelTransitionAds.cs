using UnityEngine;

namespace JTLStudio.SDK.Samples.Examples
{
    public class LevelTransitionAds : MonoBehaviour
    {
        public void OnLevelCompleted()
        {
            JTLSDK.Ads.ShowInterstitial(result => LoadNextLevel());
        }

        public void OnMainMenuOpened()
        {
            if (JTLSDK.Ads.IsBannerSupported)
            {
                JTLSDK.Ads.ShowBanner();
            }
        }

        public void OnMainMenuClosed()
        {
            JTLSDK.Ads.HideBanner();
        }

        private void LoadNextLevel()
        {
            Debug.Log("Load the next level here.");
        }
    }
}
