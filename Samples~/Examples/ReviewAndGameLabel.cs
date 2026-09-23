using UnityEngine;

namespace JTLStudio.SDK.Samples.Examples
{
    public class ReviewAndGameLabel : MonoBehaviour
    {
        public void OnThirdLevelCompleted()
        {
            if (JTLSDK.Review.CanRequest)
            {
                JTLSDK.Review.Request(left => Debug.Log("Review left: " + left));
            }
        }

        public void OnAddToDesktopClicked()
        {
            if (JTLSDK.GameLabel.CanShow)
            {
                JTLSDK.GameLabel.ShowDialog(added => Debug.Log("Game label added: " + added));
            }
        }
    }
}
