
# Inventory System Project Documentation

## Overview
This Unity project implements a generic inventory system, supporting item management, saving/loading, and editor extensions for easy item manipulation.

## Main Components
- **InventorySystem**: MonoBehaviour managing the inventory as a dictionary of items. Supports adding, removing, and printing inventory contents.
- **InventorySave**: Handles serialization and deserialization of inventory data to JSON files, including save/load/clear operations and explorer integration.
- **Item**: ScriptableObject representing an inventory item, with attributes such as ID, name, type, and custom attributes.
- **ItemTypeSO**: ScriptableObject defining item types and their default attributes.
- **ItemAttribute**: Serializable class for item attributes, supporting multiple data types (string, int, float, bool).

## Editor Extensions
- **EInventory**: Custom inspector for InventorySystem, allowing item management directly in the Unity Editor.
- **EWInventoryAdd**: Editor window for adding or editing inventory items, supporting attribute editing and type selection.
- **InventoryObjectCreation**: Menu utility for creating new inventory GameObjects in the scene.

## Data Structure
- **ItemData**: Serializable class for saving item state (ID, type, name, values, attributes).
- **InventoryData**: Serializable wrapper for a list of ItemData, used in save/load operations.

## Usage
- Use the custom inspector and editor windows to manage inventory items in the Unity Editor.
- Inventory data is saved as JSON in the persistent data path.
- Item types and attributes are extensible via ScriptableObjects.

---
For any questions, contact me via discord: `@ultraxdevs`

​        