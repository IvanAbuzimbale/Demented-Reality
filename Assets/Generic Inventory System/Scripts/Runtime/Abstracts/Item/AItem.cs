using UnityEngine;

namespace GenericInventorySystem 
{
    [System.Serializable]
    public abstract class AItem : MonoBehaviour
    {
        [Header("Item Settings")]
        protected int itemID;
        [SerializeField] protected ItemTypeSO itemType;
        [SerializeField] protected string itemName;

        [Space]
        [SerializeField] protected int buyValue;
        [SerializeField] protected int sellValue;
        [SerializeField] protected int amountInInventory = 1;

        public int ItemID { get => itemID; set => itemID = value; }
        public string ItemName { get => itemName; }
        public ItemTypeSO ItemTypeValue => itemType;
        public int BuyValue { get => buyValue; set => buyValue = value; }
        public int SellValue { get => sellValue; set => sellValue = value; }
        public int AmountInInventory { get => amountInInventory; set => amountInInventory = value; }

        void Start()
        {
            amountInInventory = amountInInventory == 0 ? 1 : amountInInventory;
        }
    }
}