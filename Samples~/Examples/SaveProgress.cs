using System;
using System.Collections.Generic;
using UnityEngine;

namespace JTLStudio.SDK.Samples.Examples
{
    public class SaveProgress : MonoBehaviour
    {
        private const string LevelKey = "Level";
        private const string SoundKey = "Sound";
        private const string InventoryKey = "Inventory";

        private void Start()
        {
            JTLSDK.WhenReady(Load);
        }

        public void OnLevelCompleted(int level)
        {
            JTLSDK.Data.SetInt(LevelKey, level + 1);
            JTLSDK.Data.Save(success => Debug.Log("Saved to the cloud: " + success));
        }

        public void OnSoundToggled(bool enabled)
        {
            JTLSDK.Data.SetBool(SoundKey, enabled);
        }

        public void OnItemBought(string itemId)
        {
            Inventory inventory = JTLSDK.Data.GetObject(InventoryKey, new Inventory());
            inventory.Add(itemId);
            JTLSDK.Data.SetObject(InventoryKey, inventory);
        }

        private void Load()
        {
            int level = JTLSDK.Data.GetInt(LevelKey, 1);
            bool sound = JTLSDK.Data.GetBool(SoundKey, true);
            Debug.Log("Level " + level + ", sound " + sound + ", save state " + JTLSDK.Data.LoadState);
        }

        [Serializable]
        public class Inventory
        {
            [SerializeField] private List<string> _items = new List<string>();

            public IReadOnlyList<string> Items => _items;

            public void Add(string itemId)
            {
                _items.Add(itemId);
            }
        }
    }
}
