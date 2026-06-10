using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GenericInventorySystem 
{
    public class InventoryObjectCreation
    {
        [UnityEditor.MenuItem("GameObject/Inventory/Create New Inventory Object", false, 0)]
        public static void CreateNewInventoryObject()
        {
            GameObject go = new GameObject("Inventory Object");
            go.AddComponent<InventorySystem>();
        }
    }
}