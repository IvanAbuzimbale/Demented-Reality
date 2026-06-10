using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace GenericInventorySystem 
{
    [CustomEditor(typeof(InventorySystem))]
    public class EInventory : Editor
    {
        [SerializeField] InventorySystem inventoryReferenceP;
        InventorySave _dataMangement;

        private void OnEnable()
        {
            inventoryReferenceP = FindObjectOfType<InventorySystem>();
            try
            {
                _dataMangement = inventoryReferenceP.gameObject.GetComponent<InventorySave>();
            }
            catch
            {
                Debug.LogError("You have to add InventorySave component to your inventory GameObject");
            }
            _dataMangement.Clear();
            _dataMangement.Load();
        }
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (inventoryReferenceP.Inventory != null)
            {
                try
                {
                    foreach (var kvp in inventoryReferenceP.Inventory)
                    {
                        EditorGUILayout.Space();
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"ID: {kvp.Key} Name: {kvp.Value.ItemName} Amount: {kvp.Value.AmountInInventory} ");
                        if (GUILayout.Button("+"))
                        {
                            inventoryReferenceP.Add(kvp.Value);
                        }
                        if (GUILayout.Button("-"))
                        {
                            inventoryReferenceP.Remove(kvp.Value);
                        }
                        if (GUILayout.Button("Settings"))
                        {
                            EWInventoryAdd window = (EWInventoryAdd)EditorWindow.GetWindow(typeof(EWInventoryAdd), true, "Edit Item");
                            window.GetType().GetField("inventory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                .SetValue(window, inventoryReferenceP);
                            window.GetType().GetField("_itemName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                .SetValue(window, kvp.Value.ItemName);
                            window.GetType().GetField("_attributes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                .SetValue(window, new List<ItemAttribute>(kvp.Value.Attributes));
                            window.GetType().GetField("_typeSO", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                .SetValue(window, kvp.Value.ItemTypeValue);
                            window.GetType().GetField("_lastTypeSO", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                .SetValue(window, kvp.Value.ItemTypeValue);
                            
                            var allTypesField = window.GetType().GetField("_allTypes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            var allTypes = (ItemTypeSO[])allTypesField.GetValue(window);
                            int selectedTypeIndex = 0;
                            if (allTypes != null)
                            {
                                for (int i = 0; i < allTypes.Length; i++)
                                {
                                    if (allTypes[i] == kvp.Value.ItemTypeValue)
                                    {
                                        selectedTypeIndex = i;
                                        break;
                                    }
                                }
                            }
                            window.GetType().GetField("_selectedTypeIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                .SetValue(window, selectedTypeIndex);
                                
                            window.SetEditMode(kvp.Value);

                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
                catch { }
            }
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save"))
            {
                _dataMangement.Save();
            }

            if (GUILayout.Button("Load"))
            {
                _dataMangement.Clear();
                _dataMangement.Load();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Clear"))
            {
                _dataMangement.Clear();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Show In Explorer"))
            {
                _dataMangement.ShowExplorer();
            }

            EditorGUILayout.Space(40);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(InventorySave.enableLogging ? "Disable Logging" : "Enable Logging"))
            {
                InventorySave.enableLogging = !InventorySave.enableLogging;
            }
            GUILayout.Label($"Logging: {(InventorySave.enableLogging ? "ON" : "OFF")}");
            GUILayout.EndHorizontal();
            Repaint();
        }
    }
}