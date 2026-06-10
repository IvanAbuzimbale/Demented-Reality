using System;
using System.Collections.Generic;
using UnityEngine;

namespace GenericInventorySystem 
{
    [CreateAssetMenu(menuName = "Inventory/Item")]
    public class Item : ScriptableObject
    {
        [SerializeField] private int itemID;
        [SerializeField] private string itemName;
        [SerializeField] private List<ItemAttribute> attributes = new List<ItemAttribute>();
        [SerializeField] private int amountInInventory = 1;
        [SerializeField] private ItemTypeSO itemType;

        public int ItemID => itemID;
        public string ItemName => itemName;
        public List<ItemAttribute> Attributes => attributes;
        public int AmountInInventory
        {
            get => amountInInventory;
            set => amountInInventory = value;
        }
        public ItemTypeSO ItemTypeValue => itemType;

        public void Initialize(string itemName, List<ItemAttribute> attributes, int itemID)
        {
            this.itemName = itemName;
            this.attributes = attributes;
            this.itemID = itemID;
        }

        public void SetType(ItemTypeSO type)
        {
            itemType = type;
        }
    }

    [Serializable]
    public class ItemAttribute
    {
        public string key;
        public AttributeType type;
        public string stringValue;
        public int intValue;
        public float floatValue;
        public bool boolValue;

        public object GetValue()
        {
            switch (type)
            {
                case AttributeType.String: return stringValue;
                case AttributeType.Int: return intValue;
                case AttributeType.Float: return floatValue;
                case AttributeType.Bool: return boolValue;
                default: return null;
            }
        }
    }

    public enum AttributeType
    {
        String,
        Int,
        Float,
        Bool
    }
}