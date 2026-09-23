using UnityEngine;

namespace JTLStudio.SDK.Samples.Examples
{
    public class LeaderboardExample : MonoBehaviour
    {
        private const string LevelsLeaderboard = "levels";
        private const int TopCount = 10;
        private const int AroundCount = 3;

        public void OnLevelCompleted(int level)
        {
            JTLSDK.Leaderboards.SetScore(LevelsLeaderboard, level);
        }

        public void OnLeaderboardOpened()
        {
            if (JTLSDK.Leaderboards.CanLoad == false)
            {
                return;
            }

            JTLSDK.Leaderboards.Load(LevelsLeaderboard, TopCount, AroundCount, ShowPage);
        }

        private void ShowPage(LeaderboardPage page)
        {
            foreach (LeaderboardEntry entry in page.Entries)
            {
                Debug.Log(entry.Rank + ". " + entry.PlayerName + ": " + entry.Score + (entry.IsCurrentPlayer ? " (you)" : ""));
            }
        }
    }
}
