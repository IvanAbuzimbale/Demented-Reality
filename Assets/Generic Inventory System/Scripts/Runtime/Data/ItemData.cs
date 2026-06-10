using System.Collections.Generic;

namespace GenericInventorySystem 
{
    [System.Serializable]
    public class ItemData
    {
        public int _itemID;
        public string _typeName;
        public string _itemName;
        public int _buyValue;
        public int _sellValue;
        public int _amount;
        public List<ItemAttribute> _attributes;

        public ItemData(int itemID, string typeName, string itemName, int buyValue, int sellValue, int amount, List<ItemAttribute> attributes)
        {
            _itemID = itemID;
            _typeName = typeName;
            _itemName = itemName;
            _buyValue = buyValue;
            _sellValue = sellValue;
            _amount = amount;
            _attributes = attributes;
        }
    }
}