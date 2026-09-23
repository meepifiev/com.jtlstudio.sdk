using UnityEngine;

namespace JTLStudio.SDK.Samples.Examples
{
    public class LanguageExample : MonoBehaviour
    {
        private void Start()
        {
            JTLSDK.WhenReady(OnReady);
        }

        private void OnDestroy()
        {
            if (JTLSDK.IsCreated)
            {
                JTLSDK.Language.Changed -= ApplyLanguage;
            }
        }

        public void OnRussianClicked()
        {
            JTLSDK.Language.Set(Language.Russian);
        }

        private void OnReady()
        {
            JTLSDK.Language.Changed += ApplyLanguage;
            ApplyLanguage(JTLSDK.Language.Current);
        }

        private void ApplyLanguage(Language language)
        {
            Debug.Log("Show texts in " + language);
        }
    }
}
