using System;
using System.Globalization;
using UnityEditor;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace JTLStudio.SDK.Editor.Updates
{
    public class SdkUpdates
    {
        public const string Repository = "meepifiev/com.jtlstudio.sdk";

        private const string PackagePath = "Packages/com.jtlstudio.sdk";
        private const string FallbackVersion = "0.0.0";
        private const string LatestKey = "JTLSDK.Updates.Latest";
        private const string CheckedTicksKey = "JTLSDK.Updates.CheckedTicks";
        private static readonly TimeSpan CacheLifetime = TimeSpan.FromHours(1);

        private readonly GitHubReleases _github = new GitHubReleases();

        public event Action Changed;

        public ReleaseCheckResult Releases { get; private set; }

        public bool IsChecking { get; private set; }

        public string CheckedAt
        {
            get
            {
                long ticks = CheckedTicks;
                return ticks == 0 ? null : new DateTime(ticks).ToString("HH:mm", CultureInfo.InvariantCulture);
            }
        }

        public string InstalledVersion
        {
            get
            {
                PackageInfo package = PackageInfo.FindForAssetPath(PackagePath);
                return package == null ? FallbackVersion : package.version;
            }
        }

        public ReleaseInfo Latest => FindLatest(Releases);

        public string LatestVersion
        {
            get
            {
                ReleaseInfo latest = Latest;

                if (latest != null)
                {
                    return latest.Version;
                }

                string cached = SessionState.GetString(LatestKey, "");
                return string.IsNullOrEmpty(cached) ? null : cached;
            }
        }

        public bool IsUpdateAvailable
        {
            get
            {
                string latest = LatestVersion;
                return latest != null && CompareVersions(latest, InstalledVersion) > 0;
            }
        }

        private long CheckedTicks
        {
            get => long.TryParse(SessionState.GetString(CheckedTicksKey, "0"), NumberStyles.Integer, CultureInfo.InvariantCulture, out long ticks) ? ticks : 0;
            set => SessionState.SetString(CheckedTicksKey, value.ToString(CultureInfo.InvariantCulture));
        }

        public int CompareVersions(string left, string right)
        {
            return _github.CompareVersions(left, right);
        }

        public void CheckIfStale()
        {
            if (DateTime.Now - new DateTime(CheckedTicks) > CacheLifetime)
            {
                Check();
            }
        }

        public void CheckOnce()
        {
            if (Releases == null)
            {
                Check();
            }
        }

        public void Check()
        {
            if (IsChecking)
            {
                return;
            }

            IsChecking = true;
            Changed?.Invoke();
            _github.Fetch(Repository, OnFetched);
        }

        private void OnFetched(ReleaseCheckResult result)
        {
            Releases = result;
            IsChecking = false;
            CheckedTicks = DateTime.Now.Ticks;

            if (result.IsSuccess)
            {
                SessionState.SetString(LatestKey, FindLatest(result)?.Version ?? "");
            }

            Changed?.Invoke();
        }

        private ReleaseInfo FindLatest(ReleaseCheckResult result)
        {
            if (result == null || result.IsSuccess == false)
            {
                return null;
            }

            ReleaseInfo latest = null;

            foreach (ReleaseInfo release in result.Releases)
            {
                if (release.IsPreRelease)
                {
                    continue;
                }

                if (latest == null || CompareVersions(release.Version, latest.Version) > 0)
                {
                    latest = release;
                }
            }

            return latest;
        }
    }
}
