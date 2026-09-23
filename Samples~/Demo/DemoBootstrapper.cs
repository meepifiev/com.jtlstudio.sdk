using System;
using System.Collections.Generic;
using JTLStudio.SDK;
using UnityEngine;

namespace JTLStudio.SDK.Samples.Demo
{
    public class DemoBootstrapper : MonoBehaviour
    {
        private const int MaxLogLines = 14;
        private const string MoneyKey = "Money";
        private const string RemoveAdsProduct = "remove_ads";
        private const string CoinsProduct = "coins_1000";
        private const string LevelsLeaderboard = "levels";
        private const string DoubleMoneyReward = "double_money";

        private readonly List<string> _log = new List<string>();
        private bool _menuPaused;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;

        private void Awake()
        {
            JTLSDK.Create();
            JTLSDK.WhenReady(OnReady);
            Log("JTLSDK.Create called, waiting for readiness.");
        }

        private void OnGUI()
        {
            EnsureStyles();
            GUILayout.BeginArea(new Rect(16, 16, 360, Screen.height - 32));
            DrawStatus();
            GUILayout.Space(8);
            DrawButtons();
            GUILayout.EndArea();

            GUILayout.BeginArea(new Rect(392, 16, Screen.width - 408, Screen.height - 32));
            DrawLog();
            GUILayout.EndArea();
        }

        private void OnReady()
        {
            Log("Ready. Platform " + JTLSDK.Platform.Current + ", language " + JTLSDK.Language.Current + ", save " + JTLSDK.Data.LoadState + ".");
            JTLSDK.Language.Changed += OnLanguageChanged;
            JTLSDK.Pause.Changed += OnPauseChanged;
            JTLSDK.Audio.PlatformMuteChanged += OnPlatformMuteChanged;
            JTLSDK.GameEvents.GameReady();
            JTLSDK.GameEvents.GameplayStarted();
            JTLSDK.Payments.RestorePurchases(OnRestoreData);
        }

        private void DrawStatus()
        {
            if (JTLSDK.IsCreated == false)
            {
                GUILayout.Label("SDK is not created.", _labelStyle);
                return;
            }

            GUILayout.Label("Ready: " + JTLSDK.IsReady, _labelStyle);
            GUILayout.Label("Platform: " + JTLSDK.Platform.Current + " · device " + JTLSDK.Device.Type, _labelStyle);
            GUILayout.Label("Language: " + JTLSDK.Language.Current, _labelStyle);
            GUILayout.Label("Save: " + JTLSDK.Data.LoadState + " · dirty " + JTLSDK.Data.IsDirty + " · money " + JTLSDK.Data.GetInt(MoneyKey), _labelStyle);
            GUILayout.Label("Paused: " + JTLSDK.Pause.IsPaused + " · scale " + JTLSDK.Time.Scale + " · volume " + JTLSDK.Audio.Volume + " · muted " + JTLSDK.Audio.IsPlatformMuted, _labelStyle);
            GUILayout.Label("Game events: ready " + JTLSDK.GameEvents.IsGameReady + " · gameplay " + JTLSDK.GameEvents.IsGameplayActive, _labelStyle);
            GUILayout.Label("Remove ads purchased: " + JTLSDK.Payments.IsAlreadyPurchased(RemoveAdsProduct), _labelStyle);
        }

        private void DrawButtons()
        {
            if (JTLSDK.IsCreated == false)
            {
                return;
            }

            if (Button("Show interstitial"))
            {
                JTLSDK.Ads.ShowInterstitial(result => Log("Interstitial: " + result));
            }

            if (Button("Show rewarded"))
            {
                JTLSDK.Ads.ShowRewarded(DoubleMoneyReward, result => Log("Rewarded: " + result));
            }

            if (Button("Buy remove_ads"))
            {
                JTLSDK.Payments.Purchase(RemoveAdsProduct, () => GiveProduct(RemoveAdsProduct), () => Log("Purchase remove_ads failed"));
            }

            if (Button("Buy coins_1000"))
            {
                JTLSDK.Payments.Purchase(CoinsProduct, () => GiveProduct(CoinsProduct), () => Log("Purchase coins_1000 failed"));
            }

            if (Button("Add 100 money"))
            {
                JTLSDK.Data.SetInt(MoneyKey, JTLSDK.Data.GetInt(MoneyKey) + 100);
            }

            if (Button("Save now"))
            {
                JTLSDK.Data.Save(success => Log("Save: " + success));
            }

            if (Button("Submit score 27"))
            {
                JTLSDK.Leaderboards.SetScore(LevelsLeaderboard, 27);
                Log("Score submitted.");
            }

            if (Button("Authorize"))
            {
                JTLSDK.Player.Authorize(success => Log("Authorize: " + success + " · " + JTLSDK.Player.Name));
            }

            if (Button("Request review"))
            {
                JTLSDK.Review.Request(sent => Log("Review: " + sent));
            }

            if (Button("Show game label dialog"))
            {
                JTLSDK.GameLabel.ShowDialog(created => Log("Game label: " + created));
            }

            if (Button(_menuPaused ? "Resume (Menu)" : "Pause (Menu)"))
            {
                ToggleMenuPause();
            }

            if (Button("Switch language"))
            {
                SwitchLanguage();
            }

            if (Button("Time scale 0.3 / 1"))
            {
                JTLSDK.Time.Scale = JTLSDK.Time.Scale < 1f ? 1f : 0.3f;
            }
        }

        private void DrawLog()
        {
            foreach (string line in _log)
            {
                GUILayout.Label(line, _labelStyle);
            }
        }

        private void ToggleMenuPause()
        {
            _menuPaused = _menuPaused == false;
            JTLSDK.Time.Scale = _menuPaused ? 0f : 1f;
            JTLSDK.Audio.Paused = _menuPaused;

            if (_menuPaused)
            {
                JTLSDK.GameEvents.GameplayStopped();
            }
            else
            {
                JTLSDK.GameEvents.GameplayStarted();
            }
        }

        private void SwitchLanguage()
        {
            IReadOnlyList<Language> supported = JTLSDK.Language.Supported;

            if (supported.Count < 2)
            {
                Log("Only one language is supported.");
                return;
            }

            int index = 0;

            for (int candidate = 0; candidate < supported.Count; candidate++)
            {
                if (supported[candidate] == JTLSDK.Language.Current)
                {
                    index = candidate;
                }
            }

            JTLSDK.Language.Set(supported[(index + 1) % supported.Count]);
        }

        private bool Button(string text)
        {
            return GUILayout.Button(text, _buttonStyle, GUILayout.Height(28));
        }

        private void EnsureStyles()
        {
            if (_labelStyle != null)
            {
                return;
            }

            _labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
            _buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 14 };
        }

        private void Log(string message)
        {
            _log.Add(DateTime.Now.ToString("HH:mm:ss") + "  " + message);

            while (_log.Count > MaxLogLines)
            {
                _log.RemoveAt(0);
            }

            Debug.Log("[Demo] " + message);
        }

        private void OnLanguageChanged(Language language)
        {
            Log("Language changed: " + language);
        }

        private void OnRestoreData(IRestoreData restoreData)
        {
            if (restoreData == null)
            {
                Log("Restore is not available.");
                return;
            }

            Log("Purchases: " + restoreData.AllPurchases.Count + ", pending: " + restoreData.PendingProducts.Count);

            foreach (string productId in restoreData.PendingProducts)
            {
                restoreData.RestoreProduct(productId, () => GiveProduct(productId));
            }
        }

        private void GiveProduct(string productId)
        {
            if (productId == CoinsProduct)
            {
                JTLSDK.Data.SetInt(MoneyKey, JTLSDK.Data.GetInt(MoneyKey) + 1000);
            }

            Log("Granted: " + productId);
        }

        private void OnPauseChanged(bool paused)
        {
            Log(paused ? "Paused by " + string.Join(", ", JTLSDK.Pause.Sources) : "Resumed.");
        }

        private void OnPlatformMuteChanged(bool muted)
        {
            Log("Platform mute: " + muted);
        }
    }
}
