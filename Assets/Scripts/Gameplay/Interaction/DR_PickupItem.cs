using GenericInventorySystem;
using UnityEngine;

namespace DementedReality.Gameplay.Interaction
{
    [DisallowMultipleComponent]
    public sealed class DR_PickupItem : MonoBehaviour
    {
        [Header("Inventory Payload")]
        [Tooltip("Item ScriptableObject granted to the inventory on pickup.")]
        [SerializeField] private Item itemAsset;
        [SerializeField, Min(1)] private int amount = 1;

        [Header("Inspection Display")]
        [Tooltip("Optional override prefab shown inside the inspection popup. If null, a copy of this GameObject is used.")]
        [SerializeField] private GameObject displayPrefabOverride;
        [Tooltip("Uniform scale applied to the spawned inspection model.")]
        [SerializeField, Min(0.001f)] private float inspectionScale = 1f;
        [Tooltip("Local offset applied to the spawned inspection model.")]
        [SerializeField] private Vector3 inspectionOffset = Vector3.zero;
        [Tooltip("Initial local euler rotation applied to the spawned inspection model.")]
        [SerializeField] private Vector3 inspectionInitialEuler = Vector3.zero;

        [Header("Prompt")]
        [Tooltip("Friendly name shown in the interaction prompt. Falls back to the item name or GameObject name.")]
        [SerializeField] private string displayName;

        public Item ItemAsset => itemAsset;
        public int Amount => Mathf.Max(1, amount);
        public GameObject DisplayPrefabOverride => displayPrefabOverride;
        public float InspectionScale => inspectionScale;
        public Vector3 InspectionOffset => inspectionOffset;
        public Vector3 InspectionInitialEuler => inspectionInitialEuler;

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(displayName)) return displayName;
                if (itemAsset != null && !string.IsNullOrWhiteSpace(itemAsset.ItemName)) return itemAsset.ItemName;
                return gameObject.name;
            }
        }

        public bool IsWeapon => CompareTag("Weapon");
    }
}
