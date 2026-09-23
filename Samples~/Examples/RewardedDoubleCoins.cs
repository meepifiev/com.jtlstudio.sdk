using UnityEngine;

namespace JTLStudio.SDK.Samples.Examples
{
    public class RewardedDoubleCoins : MonoBehaviour
    {
        private const string CoinsKey = "Coins";
        private const string RewardId = "double_coins";

        public void OnDoubleCoinsClicked()
        {
            if (JTLSDK.Ads.IsRewardedSupported == false || JTLSDK.Ads.IsShowing)
            {
                return;
            }

            JTLSDK.Ads.ShowRewarded(RewardId, OnRewardedFinished);
        }

        private void OnRewardedFinished(AdResult result)
        {
            if (result != AdResult.Rewarded)
            {
                Debug.Log("No reward: " + result);
                return;
            }

            int coins = JTLSDK.Data.GetInt(CoinsKey);
            JTLSDK.Data.SetInt(CoinsKey, coins * 2);
            JTLSDK.Data.Save();
        }
    }
}
