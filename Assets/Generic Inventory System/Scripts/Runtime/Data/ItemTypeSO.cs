using UnityEngine;
using System.Collections.Generic;
namespace GenericInventorySystem 
{
    [CreateAssetMenu(menuName = "Inventory/ItemType")]
    public class ItemTypeSO : ScriptableObject
    {
        public string typeName;
        public List<ItemAttribute> defaultAttributes = new List<ItemAttribute>();
    }
}