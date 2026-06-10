using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace GenericInventorySystem 
{
    public class EWInventoryAdd : EditorWindow
    {
        InventorySystem inventory;

        string _itemName;
        ItemTypeSO _typeSO;
        List<ItemAttribute> _attributes = new List<ItemAttribute>();
        List<bool> _attributeFoldouts = new List<bool>();

        ItemTypeSO[] _allTypes;
        string[] _allTypeNames;
        int _selectedTypeIndex = 0;

        bool shouldSave = false;
        bool hasSaved = false;

        InventorySave _dataMangement;

        bool isEditMode = false;
        int editingItemID = -1;

        [MenuItem("Tools/Inventory/Add Item")]
        public static void ShowWindow()
        {
            EditorWindow window = GetWindow(typeof(EWInventoryAdd), true, "Add Item to Inventory");
        }
        private void OnEnable()
        {
            _dataMangement = FindObjectOfType<InventorySave>();
            _attributes = new List<ItemAttribute>();
            _attributeFoldouts = new List<bool>();

            string[] guids = AssetDatabase.FindAssets("t:ItemTypeSO");
            _allTypes = new ItemTypeSO[guids.Length];
            _allTypeNames = new string[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                _allTypes[i] = AssetDatabase.LoadAssetAtPath<ItemTypeSO>(path);
                _allTypeNames[i] = _allTypes[i] != null ? _allTypes[i].typeName : "Unnamed";
            }
            if (_typeSO != null)
            {
                for (int i = 0; i < _allTypes.Length; i++)
                {
                    if (_allTypes[i] == _typeSO)
                    {
                        _selectedTypeIndex = i;
                        break;
                    }
                }
            }
        }

        bool _attributesFoldout = true;

        private void ReloadItemTypes()
        {
            string[] guids = AssetDatabase.FindAssets("t:ItemTypeSO");
            _allTypes = new ItemTypeSO[guids.Length];
            _allTypeNames = new string[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                _allTypes[i] = AssetDatabase.LoadAssetAtPath<ItemTypeSO>(path);
                _allTypeNames[i] = _allTypes[i] != null ? _allTypes[i].typeName : "Unnamed";
            }
        }

        private ItemTypeSO _lastTypeSO = null;

        public void SetEditMode(Item item)
        {
            isEditMode = true;
            editingItemID = item.ItemID;
            _itemName = item.ItemName;
            _typeSO = item.ItemTypeValue;
            _attributes = new List<ItemAttribute>(item.Attributes);
        }

        private void OnGUI()
        {
            ReloadItemTypes();

            #region Title
            GUILayout.Space(15);
            if (isEditMode){
                GUILayout.Label("Edit Item", new GUIStyle()
                {
                    fontSize = 20,
                    normal = new GUIStyleState() { textColor = Color.white },
                    alignment = TextAnchor.MiddleCenter
                });
            }
            else {
                GUILayout.Label("Add Item", new GUIStyle()
                {
                    fontSize = 20,
                    normal = new GUIStyleState() { textColor = Color.white },
                    alignment = TextAnchor.MiddleCenter
                });
            }

            EditorGUILayout.Space(50);
            #endregion
            EditorGUILayout.LabelField("Inventory Settings", EditorStyles.boldLabel);
            if (hasSaved)
            {
                EditorGUILayout.LabelField("Reopen window to add item to another inventory!", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Character inventory", inventory.gameObject.name);
            }
            else
            {
                inventory = (InventorySystem)EditorGUILayout.ObjectField("Character inventory", inventory, typeof(InventorySystem), true);
            }

            EditorGUILayout.Space(10);
            #region Item Settings
            EditorGUILayout.LabelField("Item Settings", EditorStyles.boldLabel);

            _itemName = EditorGUILayout.TextField("Item Name", _itemName);

            if (_allTypes != null && _allTypes.Length > 0)
            {
                int prevTypeIndex = _selectedTypeIndex;
                _selectedTypeIndex = EditorGUILayout.Popup("Item Type", _selectedTypeIndex, _allTypeNames);
                _typeSO = _allTypes[_selectedTypeIndex];
        
                if (_typeSO != _lastTypeSO && _typeSO != null)
                {
                    _attributes = new List<ItemAttribute>();
                    _attributeFoldouts = new List<bool>();
                    if (_typeSO.defaultAttributes != null)
                    {
                        foreach (var attr in _typeSO.defaultAttributes)
                        {

                            ItemAttribute newAttr = new ItemAttribute
                            {
                                key = attr.key,
                                type = attr.type,
                                stringValue = attr.stringValue,
                                intValue = attr.intValue,
                                floatValue = attr.floatValue,
                                boolValue = attr.boolValue
                            };
                            _attributes.Add(newAttr);
                            _attributeFoldouts.Add(false);
                        }
                    }
                    _lastTypeSO = _typeSO;
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No Item Types found. Please create an ItemTypeSO asset.", MessageType.Warning);
                _typeSO = null;
            }

            _attributesFoldout = EditorGUILayout.Foldout(_attributesFoldout, "Attributes");
            if (_attributesFoldout)
            {
                for (int i = 0; i < _attributes.Count; i++)
                {
                    if (_attributeFoldouts.Count <= i)
                        _attributeFoldouts.Add(false);

                    _attributeFoldouts[i] = EditorGUILayout.Foldout(_attributeFoldouts[i], $"Attribute {i + 1}: {_attributes[i].key}");

                    if (_attributeFoldouts[i])
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.BeginVertical("box");
                        _attributes[i].key = EditorGUILayout.TextField("Key", _attributes[i].key);
                        _attributes[i].type = (AttributeType)EditorGUILayout.EnumPopup("Type", _attributes[i].type);
                        switch (_attributes[i].type)
                        {
                            case AttributeType.String:
                                _attributes[i].stringValue = EditorGUILayout.TextField("Value", _attributes[i].stringValue);
                                break;
                            case AttributeType.Int:
                                _attributes[i].intValue = EditorGUILayout.IntField("Value", _attributes[i].intValue);
                                break;
                            case AttributeType.Float:
                                _attributes[i].floatValue = EditorGUILayout.FloatField("Value", _attributes[i].floatValue);
                                break;
                            case AttributeType.Bool:
                                _attributes[i].boolValue = EditorGUILayout.Toggle("Value", _attributes[i].boolValue);
                                break;
                        }
                        if (GUILayout.Button("Remove Attribute"))
                        {
                            _attributes.RemoveAt(i);
                            _attributeFoldouts.RemoveAt(i);
                            i--;
                        }
                        EditorGUILayout.EndVertical();
                        EditorGUI.indentLevel--;
                    }
                }
                if (GUILayout.Button("Add Attribute"))
                {
                    _attributes.Add(new ItemAttribute());
                    _attributeFoldouts.Add(true);
                }
            }
            #endregion

            EditorGUILayout.Space(10);
            if (!isEditMode) shouldSave = EditorGUILayout.Toggle("Should save on add", shouldSave);
            float button_height = 50;
            string buttonLabel = isEditMode ? "Save Changes" : "Add new item";
            if (GUI.Button(new Rect(0, position.height - button_height, position.width, button_height), buttonLabel))
            {
                try
                {
                    if (isEditMode)
                    {
                        if (inventory.Inventory.ContainsKey(editingItemID))
                        {
                            var item = inventory.Inventory[editingItemID];
                            item.Initialize(_itemName, new List<ItemAttribute>(_attributes), editingItemID);
                            item.SetType(_typeSO);
                            inventory.PrintInventory();
                            hasSaved = true;
                        }
                    }
                    else
                    {
                        Item newItem = ScriptableObject.CreateInstance<Item>();
                        int newID = UnityEngine.Random.Range(100000, 999999);
                        newItem.Initialize(_itemName, new List<ItemAttribute>(_attributes), newID);
                        newItem.SetType(_typeSO);
                        inventory.Add(newItem);
                        inventory.PrintInventory();
                        hasSaved = true;
                    }
                }
                catch (Exception ex)
                {
                    if (InventorySave.enableLogging) Debug.LogError("Error: " + ex.Message);
                }
            }
            if (shouldSave)
            {
                inventory.PrintInventory();
                _dataMangement.Save();
                hasSaved = true;
            }
        }
    }
}