using System;
using JTLStudio.SDK.Providers;

namespace JTLStudio.SDK.Tests.Fakes
{
    public class FakeDataProvider : IDataProvider
    {
        private Action<ProviderState> _onInitialized;
        private Action<bool> _onSaved;

        public int MaxBytes { get; set; } = 200 * 1024;
        public int RecommendedBytes { get; set; } = 100 * 1024;
        public DataLoadResult LoadResult { get; set; } = DataLoadResult.Empty;
        public string LoadPayload { get; set; } = "";
        public bool SaveSucceeds { get; set; } = true;
        public bool CompleteImmediately { get; set; } = true;
        public bool CompleteSaveImmediately { get; set; } = true;
        public int LoadCount { get; private set; }
        public int SaveCount { get; private set; }
        public string LastSaved { get; private set; }

        public void Initialize(Action<ProviderState> onInitialized)
        {
            if (CompleteImmediately)
            {
                onInitialized(ProviderState.Ready);
                return;
            }

            _onInitialized = onInitialized;
        }

        public void Complete(ProviderState state)
        {
            Action<ProviderState> callback = _onInitialized;
            _onInitialized = null;
            callback?.Invoke(state);
        }

        public void Load(Action<DataLoadResult, string> onLoaded)
        {
            LoadCount++;
            onLoaded(LoadResult, LoadPayload);
        }

        public void Save(string serialized, Action<bool> onSaved)
        {
            SaveCount++;
            LastSaved = serialized;

            if (CompleteSaveImmediately)
            {
                onSaved(SaveSucceeds);
                return;
            }

            _onSaved = onSaved;
        }

        public void CompletePendingSave(bool success)
        {
            Action<bool> callback = _onSaved;
            _onSaved = null;
            callback?.Invoke(success);
        }
    }
}
