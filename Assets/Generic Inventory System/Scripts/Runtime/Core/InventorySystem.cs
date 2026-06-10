using System.Collections.Generic;
using UnityEngine;

namespace GenericInventorySystem 
{
    [RequireComponent(typeof(InventorySave))]
    [System.Serializable]
    public class InventorySystem : MonoBehaviour
    {
        private Dictionary<int, Item> inventory = new Dictionary<int, Item>();

        public Dictionary<int, Item> Inventory { get => inventory; set => inventory = value; }

        public void Add(Item item)
        {
            if (Inventory.ContainsKey(item.ItemID))
            {
                Inventory[item.ItemID].AmountInInventory += 1;
            }
            else
            {
                Item newItem = ScriptableObject.CreateInstance<Item>();
                newItem.Initialize(item.ItemName, item.Attributes, item.ItemID);
                
                if (item.ItemTypeValue != null)
                {
                    newItem.SetType(item.ItemTypeValue);
                }
                newItem.AmountInInventory = item.AmountInInventory;
                Inventory.Add(item.ItemID, newItem);
            }
        }

        public void Remove(Item item)
        {
            if (Inventory.ContainsKey(item.ItemID))
            {
                Inventory[item.ItemID].AmountInInventory--;
                if (Inventory[item.ItemID].AmountInInventory <= 0)
                {
                    Inventory.Remove(item.ItemID);
                }
            }
        }

        public void PrintInventory()
        {
            string value = "";
            foreach (var item in Inventory)
            {
                string typeName = item.Value.ItemTypeValue != null ? item.Value.ItemTypeValue.typeName : "None";
                value += $"({item.Key}\n {typeName}\n {item.Value.ItemName}\n {item.Value.AmountInInventory}) \t\r";
            }
            if (InventorySave.enableLogging) Debug.Log(value);
        }
    }
}