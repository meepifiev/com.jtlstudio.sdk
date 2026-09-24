using System;
using System.Collections.Generic;
using JTLStudio.SDK.Services.Json;
using UnityEditor;
using UnityEngine.Networking;

namespace JTLStudio.SDK.Editor.Updates
{
    public class ModuleUpdates
    {
        private const string ApiRoot = "https://api.github.com/repos/";
        private const string UserAgent = "JTLSDK-Toolkit";

        private static readonly Dictionary<string, string> Tags = new Dictionary<string, string>();
        private static readonly HashSet<string> Pending = new HashSet<string>();

        private readonly JsonParser _parser = new JsonParser();
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
            UnityWebRequest request = UnityWebRequest.Get(ApiRoot + repository + "/tags?per_page=100");
            request.SetRequestHeader("User-Agent", UserAgent);
            request.SetRequestHeader("Accept", "application/vnd.github+json");
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

        private string Highest(string json)
        {
            string highest = "";

            try
            {
                if (_parser.Parse(json) is List<object> items == false)
                {
                    return "";
                }

                foreach (object item in items)
                {
                    if (item is Dictionary<string, object> tag == false || tag.TryGetValue("name", out object name) == false || name is string text == false)
                    {
                        continue;
                    }

                    string version = text.StartsWith("v") ? text.Substring(1) : text;

                    if (string.IsNullOrEmpty(highest) || _updates.CompareVersions(version, highest) > 0)
                    {
                        highest = version;
                    }
                }
            }
            catch (FormatException)
            {
                return "";
            }

            return highest;
        }
    }
}
