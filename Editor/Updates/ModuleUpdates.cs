using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Networking;

namespace JTLStudio.SDK.Editor.Updates
{
    public class ModuleUpdates
    {
        private const string TagsRoot = "https://github.com/";
        private const string TagsFeed = "/tags.atom";
        private const string UserAgent = "JTLSDK-Toolkit";

        private static readonly Dictionary<string, string> Tags = new Dictionary<string, string>();
        private static readonly HashSet<string> Pending = new HashSet<string>();

        private readonly SdkUpdates _updates = new SdkUpdates();

        public event Action Changed;

        public string Latest(string repository)
        {
            if (string.IsNullOrEmpty(repository))
            {
                return "";
            }

            if (Tags.TryGetValue(repository, out string tag))
            {
                return tag;
            }

            Fetch(repository);
            return "";
        }

        public bool HasUpdate(string repository, string installed)
        {
            string latest = Latest(repository);
            return string.IsNullOrEmpty(latest) == false
                && string.IsNullOrEmpty(installed) == false
                && _updates.CompareVersions(latest, installed) > 0;
        }

        private void Fetch(string repository)
        {
            if (Pending.Contains(repository))
            {
                return;
            }

            Pending.Add(repository);
            UnityWebRequest request = UnityWebRequest.Get(TagsRoot + repository + TagsFeed);
            request.SetRequestHeader("User-Agent", UserAgent);
            UnityWebRequestAsyncOperation operation = request.SendWebRequest();

            void Poll()
            {
                if (operation.isDone == false)
                {
                    return;
                }

                EditorApplication.update -= Poll;
                Tags[repository] = request.result == UnityWebRequest.Result.Success ? Highest(request.downloadHandler.text) : "";
                Pending.Remove(repository);
                request.Dispose();
                Changed?.Invoke();
            }

            EditorApplication.update += Poll;
        }

        public string Highest(string feed)
        {
            string highest = "";

            if (string.IsNullOrEmpty(feed))
            {
                return highest;
            }

            int cursor = 0;

            while (true)
            {
                int entry = feed.IndexOf("<entry>", cursor, StringComparison.Ordinal);

                if (entry < 0)
                {
                    break;
                }

                int titleStart = feed.IndexOf("<title>", entry, StringComparison.Ordinal);
                int titleEnd = titleStart < 0 ? -1 : feed.IndexOf("</title>", titleStart, StringComparison.Ordinal);

                if (titleStart < 0 || titleEnd < 0)
                {
                    break;
                }

                string tag = feed.Substring(titleStart + 7, titleEnd - titleStart - 7).Trim();
                string version = tag.StartsWith("v") ? tag.Substring(1) : tag;

                if (string.IsNullOrEmpty(version) == false && (string.IsNullOrEmpty(highest) || _updates.CompareVersions(version, highest) > 0))
                {
                    highest = version;
                }

                cursor = titleEnd;
            }

            return highest;
        }
    }
}
