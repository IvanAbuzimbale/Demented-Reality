using DementedReality.Gameplay.Player;
using GenericInventorySystem;
using TMPro;
using UnityEngine;

namespace DementedReality.Gameplay.Interaction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DR_PlayerInputReader))]
    public sealed class DR_PlayerPickupInteractor : MonoBehaviour
    {
        [Header("Detection")]
        [SerializeField, Min(0.1f)] private float detectionRadius = 1.6f;
        [SerializeField] private Transform detectionOrigin;
        [SerializeField] private LayerMask detectionMask = ~0;
        [SerializeField, Min(1)] private int maxCandidates = 16;

        [Header("Refs")]
        [Tooltip("Inventory the picked-up item is added to. Auto-located if left empty.")]
        [SerializeField] private InventorySystem inventory;
        [Tooltip("Optional InventorySave triggered after a successful pickup.")]
        [SerializeField] private InventorySave inventorySave;
        [Tooltip("Inspection popup driver. Auto-located if left empty.")]
        [SerializeField] private DR_PickupInspectionUI inspectionUI;

        [Header("Prompt UI")]
        [Tooltip("Optional world/HUD label that shows the interact prompt while a pickup is in range.")]
        [SerializeField] private GameObject promptRoot;
        [SerializeField] private TMP_Text promptText;
        [SerializeField] private string promptFormat = "[E] Pick up {0}";

        private DR_PlayerInputReader inputReader;
        private readonly Collider[] candidateBuffer = new Collider[32];
        private DR_PickupItem currentTarget;
        private bool inputSuppressed;

        private void Awake()
        {
            inputReader = GetComponent<DR_PlayerInputReader>();
            if (detectionOrigin == null) detectionOrigin = transform;
            if (inventory == null) inventory = GetComponentInChildren<InventorySystem>();
            if (inventory == null) inventory = FindAnyObjectByType<InventorySystem>();
            if (inventorySave == null && inventory != null) inventorySave = inventory.GetComponent<InventorySave>();
            if (inspectionUI == null) inspectionUI = FindAnyObjectByType<DR_PickupInspectionUI>(FindObjectsInactive.Include);
            SetPromptVisible(false);
        }

        public void SuppressInput(bool suppressed)
        {
            inputSuppressed = suppressed;
            if (suppressed) SetPromptVisible(false);
        }

        private void Update()
        {
            if (inputSuppressed)
            {
                currentTarget = null;
                return;
            }

            currentTarget = FindClosestPickup();
            RefreshPrompt(currentTarget);

            if (currentTarget != null && inputReader.InteractPressedThisFrame)
            {
                OpenInspection(currentTarget);
            }
        }

        private DR_PickupItem FindClosestPickup()
        {
            int bufferLen = Mathf.Min(candidateBuffer.Length, maxCandidates);
            int hitCount = Physics.OverlapSphereNonAlloc(
                detectionOrigin.position,
                detectionRadius,
                candidateBuffer,
                detectionMask,
                QueryTriggerInteraction.Collide
            );

            DR_PickupItem closest = null;
            float closestSqr = float.PositiveInfinity;
            int evaluated = Mathf.Min(hitCount, bufferLen);
            Vector3 origin = detectionOrigin.position;

            for (int i = 0; i < evaluated; i++)
            {
                Collider c = candidateBuffer[i];
                if (c == null) continue;
                if (!c.CompareTag("Item") && !c.CompareTag("Weapon")) continue;

                DR_PickupItem pickup = c.GetComponentInParent<DR_PickupItem>();
                if (pickup == null) continue;

                float sqr = (pickup.transform.position - origin).sqrMagnitude;
                if (sqr < closestSqr)
                {
                    closestSqr = sqr;
                    closest = pickup;
                }
            }

            return closest;
        }

        private void OpenInspection(DR_PickupItem pickup)
        {
            if (inspectionUI == null)
            {
                Debug.LogError("[DR_PlayerPickupInteractor] No DR_PickupInspectionUI in scene; cannot inspect pickup.", this);
                return;
            }

            SetPromptVisible(false);
            inspectionUI.Open(pickup, HandleInspectionClosed);
            SuppressInput(true);
        }

        private void HandleInspectionClosed(DR_PickupItem pickup, bool taken)
        {
            SuppressInput(false);

            if (!taken || pickup == null) return;

            if (!TryAddPickupToInventory(pickup))
            {
                Debug.LogWarning($"[DR_PlayerPickupInteractor] Inventory add failed for '{pickup.name}'; destroying world object anyway.", pickup);
            }

            Destroy(pickup.gameObject);
        }

        private bool TryAddPickupToInventory(DR_PickupItem pickup)
        {
            if (inventory == null)
            {
                Debug.LogError("[DR_PlayerPickupInteractor] No InventorySystem found; cannot store pickup.", this);
                return false;
            }

            if (pickup.ItemAsset == null)
            {
                Debug.LogError($"[DR_PlayerPickupInteractor] Pickup '{pickup.name}' has no Item asset assigned.", pickup);
                return false;
            }

            try
            {
                Item granted = ScriptableObject.CreateInstance<Item>();
                granted.Initialize(pickup.ItemAsset.ItemName, pickup.ItemAsset.Attributes, pickup.ItemAsset.ItemID);
                if (pickup.ItemAsset.ItemTypeValue != null) granted.SetType(pickup.ItemAsset.ItemTypeValue);
                granted.AmountInInventory = pickup.Amount;

                inventory.Add(granted);
                if (inventorySave != null) inventorySave.Save();
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DR_PlayerPickupInteractor] Inventory add threw for '{pickup.name}': {ex}", pickup);
                return false;
            }
        }

        private void RefreshPrompt(DR_PickupItem pickup)
        {
            if (pickup == null)
            {
                SetPromptVisible(false);
                return;
            }

            if (promptText != null)
            {
                promptText.text = string.Format(promptFormat, pickup.DisplayName);
            }
            SetPromptVisible(true);
        }

        private void SetPromptVisible(bool visible)
        {
            if (promptRoot != null && promptRoot.activeSelf != visible)
            {
                promptRoot.SetActive(visible);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Transform o = detectionOrigin != null ? detectionOrigin : transform;
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.35f);
            Gizmos.DrawWireSphere(o.position, detectionRadius);
        }
#endif
    }
}
