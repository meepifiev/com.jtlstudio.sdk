using UnityEngine;

namespace JTLStudio.SDK.Samples.Examples
{
    public class FlagsExample : MonoBehaviour
    {
        private const string InterstitialIntervalFlag = "interstitial_interval";
        private const string NewShopFlag = "new_shop";

        private void Start()
        {
            JTLSDK.WhenReady(Apply);
        }

        private void Apply()
        {
            int interval = JTLSDK.Flags.GetInt(InterstitialIntervalFlag, 90);
            bool newShop = JTLSDK.Flags.GetBool(NewShopFlag);
            Debug.Log("Interstitial every " + interval + " s, new shop " + newShop);
        }
    }
}
