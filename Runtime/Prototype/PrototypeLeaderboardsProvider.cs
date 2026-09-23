#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using JTLStudio.SDK.Providers;
using UnityEngine;

namespace JTLStudio.SDK.Prototype
{
    public class PrototypeLeaderboardsProvider : ILeaderboardsProvider
    {
        private const string StorageKeyPrefix = "JTLSDK.Prototype.Leaderboard.";

        private readonly PrototypeSimulationSettings _settings;

        public PrototypeLeaderboardsProvider(bool supportsLoad, PrototypeSimulationSettings settings)
        {
            SupportsLoad = supportsLoad;
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public bool SupportsLoad { get; }

        public void Initialize(Action<ProviderState> onInitialized)
        {
            onInitialized(ProviderState.Ready);
        }

        public void SetScore(string platformLeaderboardId, long score, Action<bool> onResult)
        {
            PlayerPrefs.SetString(StorageKeyPrefix + platformLeaderboardId, score.ToString());
            PlayerPrefs.Save();
            onResult(true);
        }

        public void GetPlayerEntry(string platformLeaderboardId, Action<LeaderboardEntry?> onResult)
        {
            if (TryGetScore(platformLeaderboardId, out long score) == false)
            {
                onResult(null);
                return;
            }

            onResult(new LeaderboardEntry(_settings.LeaderboardEntriesAbove + 1, score, _settings.PlayerName, "", true));
        }

        public void Load(string platformLeaderboardId, int topCount, int aroundCount, Action<LeaderboardPage> onResult)
        {
            List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
            long playerScore = TryGetScore(platformLeaderboardId, out long score) ? score : 0;
            int above = Math.Min(_settings.LeaderboardEntriesAbove, Math.Max(0, topCount));
            int below = Math.Min(_settings.LeaderboardEntriesBelow, Math.Max(0, aroundCount));
            long step = _settings.LeaderboardScoreStep;

            for (int index = 0; index < above; index++)
            {
                entries.Add(new LeaderboardEntry(index + 1, playerScore + (above - index) * step, "Player " + (index + 1), "", false));
            }

            LeaderboardEntry current = new LeaderboardEntry(above + 1, playerScore, _settings.PlayerName, "", true);
            entries.Add(current);

            for (int index = 0; index < below; index++)
            {
                int rank = above + 2 + index;
                entries.Add(new LeaderboardEntry(rank, Math.Max(0, playerScore - (index + 1) * step), "Player " + rank, "", false));
            }

            onResult(new LeaderboardPage(entries, current));
        }

        private bool TryGetScore(string platformLeaderboardId, out long score)
        {
            string stored = PlayerPrefs.GetString(StorageKeyPrefix + platformLeaderboardId, "");
            return long.TryParse(stored, out score);
        }
    }
}
#endif
