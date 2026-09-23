using UnityEngine;

namespace JTLStudio.SDK.Samples.Examples
{
    public class PlatformChecks : MonoBehaviour
    {
        [SerializeField] private GameObject _shopButton;
        [SerializeField] private GameObject _leaderboardButton;
        [SerializeField] private GameObject _touchControls;

        private void Start()
        {
            JTLSDK.WhenReady(OnReady);
        }

        private void OnReady()
        {
            _shopButton.SetActive(JTLSDK.Platform.Supports(Capability.Purchases));
            _leaderboardButton.SetActive(JTLSDK.Platform.Supports(Capability.LeaderboardsLoad));
            _touchControls.SetActive(JTLSDK.Device.IsMobile);
        }
    }
}
