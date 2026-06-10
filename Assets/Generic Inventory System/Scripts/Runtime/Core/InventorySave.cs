using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine; 

namespace GenericInventorySystem 
{
    public class InventorySave : MonoBehaviour
    {
        public static bool enableLogging = true;
        [SerializeField] private string filename;
        private InventorySystem inventory => gameObject.GetComponent<InventorySystem>();
        private List<ItemData> items = new List<ItemData>();
        
        protected string Path => Application.persistentDataPath + "/Inventory/" + filename + ".json";

        private void Start()
        {
            Clear();
            Load();
        }

        public void Save()
        {
            InventoryData data = new InventoryData();
            items.Clear();
            foreach (KeyValuePair<int, Item> pair in inventory.Inventory)
            {
                ItemData curVal = new ItemData(
                    pair.Value.ItemID,
                    pair.Value.ItemTypeValue != null ? pair.Value.ItemTypeValue.typeName : "",
                    pair.Value.ItemName,
                    0,
                    0,
                    pair.Value.AmountInInventory,
                    new List<ItemAttribute>(pair.Value.Attributes)
                );
                if (items.Any(item => item._itemID == pair.Value.ItemID))
                {
                    items.Remove(items.Find(item => item._itemID == pair.Value.ItemID));
                }
                items.Add(curVal);
            }
            data.itemDatas = items;

            if (!File.Exists(Path))
            {
                (new FileInfo(Path)).Directory.Create();
            }
            try
            {
                File.WriteAllText(Path, JsonUtility.ToJson(data, true));
            }
            catch (UnauthorizedAccessException)
            {
                if (enableLogging) Debug.LogError("You haven't specified filename in InventorySave component!");
            }
        }

        public void Load()
        {
            if (enableLogging) Debug.Log($"Attempting to load inventory from: {Path}");
            if (File.Exists(Path))
            {
                if (enableLogging) Debug.Log("Save file found.");
                string jsonData = File.ReadAllText(Path);
                if (enableLogging) Debug.Log($"JSON Data: {jsonData}");
                InventoryData data = JsonUtility.FromJson<InventoryData>(jsonData);
                if (data == null) {
                    if (enableLogging) Debug.LogError("Failed to deserialize InventoryData. Data is null.");
                    inventory.Inventory.Clear();
                    return;
                }
                List<ItemData> loadedItemsFromFile;
                if (data.itemDatas == null) {
                    if (enableLogging) Debug.LogError("Deserialized data.itemDatas is null.");
                    loadedItemsFromFile = new List<ItemData>();
                } else {
                    loadedItemsFromFile = data.itemDatas;
                    if (enableLogging) Debug.Log($"Found {loadedItemsFromFile.Count} items in save file.");
                }
                inventory.Inventory.Clear();
                foreach (var itemData in loadedItemsFromFile)
                {
                    if (enableLogging) Debug.Log($"Processing item from save: ID={itemData._itemID}, Name='{itemData._itemName}', TypeName='{itemData._typeName}', Amount={itemData._amount}");
                    Item tempItem = ScriptableObject.CreateInstance<Item>();
                    tempItem.Initialize(
                        itemData._itemName,
                        itemData._attributes != null ? new List<ItemAttribute>(itemData._attributes) : new List<ItemAttribute>(),
                        itemData._itemID
                    );
                    if (tempItem.Attributes != null && tempItem.Attributes.Count > 0)
                    {
                        if (enableLogging) Debug.Log($"Attributes for item '{tempItem.ItemName}':");
                        foreach (var attr in tempItem.Attributes)
                        {
                            if (enableLogging) Debug.Log($"  - {attr.key}: {attr.GetValue()} (Type: {attr.type})");
                        }
                    }
                    else
                    {
                        if (enableLogging) Debug.Log($"Item '{tempItem.ItemName}' has no attributes.");
                    }
                    ItemTypeSO typeSO = FindTypeSOByName(itemData._typeName);
                    if (typeSO != null)
                    {
                        if (enableLogging) Debug.Log($"Found ItemTypeSO: '{typeSO.typeName}' for item '{itemData._itemName}'");
                        tempItem.SetType(typeSO);
                    }
                    else if (!string.IsNullOrEmpty(itemData._typeName)) 
                    {
                        if (enableLogging) Debug.LogError($"Failed to find ItemTypeSO for typeName: '{itemData._typeName}' for item '{itemData._itemName}'. Ensure the ItemTypeSO asset exists in the project and its typeName matches, or that a runtime lookup mechanism is in place.");
                    }
                    else
                    {
                        if (enableLogging) Debug.Log($"Item '{itemData._itemName}' has no typeName specified in save data.");
                    }
                    tempItem.AmountInInventory = itemData._amount;
                    inventory.Add(tempItem);
                    if (enableLogging) Debug.Log($"Added item '{tempItem.ItemName}' to inventory system. Current inventory count: {inventory.Inventory.Count}");
                }
                inventory.PrintInventory();
            }
            else
            {
                if (enableLogging) Debug.LogWarning($"Save file not found at: {Path}");
            }
        }

        public void Clear()
        {
            inventory.Inventory.Clear();
            items.Clear();
        }

        public void ShowExplorer()
        {
            string itemPath = Path.Replace(@"/", @"\");
    #if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            System.Diagnostics.Process.Start("explorer.exe", "/select," + itemPath);
    #elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
            if (!string.IsNullOrEmpty(itemPath)) System.Diagnostics.Process.Start("xdg-open", itemPath);
            else Debug.LogError("Failed to determine folder path for the file.");
    #else
            Debug.LogError("ShowExplorer is not supported on this platform.");
    #endif
        }

        private ItemTypeSO FindTypeSOByName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return null;
            }

        #if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ItemTypeSO");
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                ItemTypeSO typeSOAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemTypeSO>(path);
                if (typeSOAsset != null && typeSOAsset.typeName == typeName)
                {
                    return typeSOAsset;
                }
            }
            Debug.LogWarning($"[InventorySave-Editor] ItemTypeSO with name '{typeName}' not found via AssetDatabase. Ensure the typeName is correct and the SO asset exists.");
            return null;
        #else
            if (enableLogging) Debug.LogError("[InventorySave-Runtime] ItemTypeRegistry has been removed. Item types cannot be loaded by name at runtime using FindTypeSOByName without an alternative implementation. Returning null.");
            return null;
        #endif
        }
    }
    [System.Serializable]
    public class InventoryData
    {
        public List<ItemData> itemDatas;
    }
}