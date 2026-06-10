using System;
using DementedReality.Gameplay.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DementedReality.Gameplay.Interaction
{
    [DisallowMultipleComponent]
    public sealed class DR_PickupInspectionUI : MonoBehaviour
    {
        [Header("UI")]
        [Tooltip("Root popup GameObject (Canvas/Panel). Toggled active while inspecting.")]
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private RawImage previewImage;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text hintText;
        [SerializeField] private string hintFormat = "Drag to rotate    [E] Take    [Esc] Cancel";

        [Header("3D Preview")]
        [Tooltip("Anchor in the scene where the inspected model is spawned. Should sit in front of the InspectionCamera.")]
        [SerializeField] private Transform inspectionAnchor;
        [Tooltip("Camera that renders the spawned model onto previewTexture.")]
        [SerializeField] private Camera inspectionCamera;
        [Tooltip("RenderTexture rendered by inspectionCamera and shown on previewImage. Created at runtime if null.")]
        [SerializeField] private RenderTexture previewTexture;
        [SerializeField] private Vector2Int autoRenderTextureSize = new Vector2Int(1024, 1024);
        [SerializeField, Range(1, 8)] private int autoRenderTextureMSAA = 4;
        [Tooltip("Layer the spawned model is moved onto so only the inspection camera sees it. Must match inspectionCamera cullingMask.")]
        [SerializeField] private string inspectionLayerName = "Inspection";

        [Header("Rotation")]
        [SerializeField] private float rotateSpeedDegPerPixel = 0.4f;
        [SerializeField] private bool invertHorizontal = false;
        [SerializeField] private bool invertVertical = false;
        [Tooltip("If true rotation only applies while the player holds LMB / RT.")]
        [SerializeField] private bool requireHoldToRotate = false;

        [Header("Background Blur")]
        [Tooltip("Optional. Refreshes a blurred snapshot of the world camera into a RawImage behind the popup.")]
        [SerializeField] private DR_BlurredBackground blurBackground;

        [Header("Player Coupling")]
        [Tooltip("Player input reader used to detect Take / Cancel while popup is open. Auto-located if null.")]
        [SerializeField] private DR_PlayerInputReader inputReader;
        [Tooltip("Optional: time scale forced while inspecting. Set to 0 to pause gameplay.")]
        [SerializeField, Range(0f, 1f)] private float timeScaleWhileOpen = 0f;

        private Action<DR_PickupItem, bool> onClosed;
        private DR_PickupItem currentPickup;
        private GameObject spawnedModel;
        private RenderTexture ownedTexture;
        private float cachedTimeScale = 1f;
        private bool isOpen;
        private int openedOnFrame = -1;

        public bool IsOpen => isOpen;

        private void Awake()
        {
            if (popupRoot != null) popupRoot.SetActive(false);
            if (inspectionCamera != null) inspectionCamera.enabled = false;
            if (inputReader == null) inputReader = FindAnyObjectByType<DR_PlayerInputReader>();
        }

        private void EnsureRenderTexture()
        {
            if (previewTexture != null) return;

            previewTexture = new RenderTexture(autoRenderTextureSize.x, autoRenderTextureSize.y, 24, RenderTextureFormat.ARGB32)
            {
                name = "DR_InspectionRT (runtime)",
                antiAliasing = Mathf.Clamp(autoRenderTextureMSAA, 1, 8),
                filterMode = FilterMode.Trilinear,
                anisoLevel = 8,
                useMipMap = false,
                autoGenerateMips = false
            };
            previewTexture.Create();
            ownedTexture = previewTexture;

            if (inspectionCamera != null) inspectionCamera.targetTexture = previewTexture;
            if (previewImage != null) previewImage.texture = previewTexture;
        }

        public void Open(DR_PickupItem pickup, Action<DR_PickupItem, bool> closedCallback)
        {
            if (isOpen)
            {
                Debug.LogWarning("[DR_PickupInspectionUI] Open called while already open; ignoring.", this);
                return;
            }
            if (pickup == null)
            {
                Debug.LogError("[DR_PickupInspectionUI] Open called with null pickup.", this);
                return;
            }
            if (inspectionAnchor == null)
            {
                Debug.LogError("[DR_PickupInspectionUI] inspectionAnchor not assigned.", this);
                return;
            }
            if (inspectionCamera == null)
            {
                Debug.LogError("[DR_PickupInspectionUI] inspectionCamera not assigned.", this);
                return;
            }

            currentPickup = pickup;
            onClosed = closedCallback;

            if (blurBackground != null) blurBackground.Refresh();

            EnsureRenderTexture();
            MatchCameraAspectToPreview();
            SpawnPreviewModel(pickup);

            if (titleText != null) titleText.text = pickup.DisplayName;
            if (hintText != null && !string.IsNullOrEmpty(hintFormat)) hintText.text = hintFormat;

            if (popupRoot != null) popupRoot.SetActive(true);
            inspectionCamera.enabled = true;

            cachedTimeScale = Time.timeScale;
            Time.timeScale = timeScaleWhileOpen;

            openedOnFrame = Time.frameCount;
            isOpen = true;
        }

        private void MatchCameraAspectToPreview()
        {
            if (previewImage == null || inspectionCamera == null) return;
            Rect r = previewImage.rectTransform.rect;
            if (r.width <= 0f || r.height <= 0f) return;
            inspectionCamera.aspect = r.width / r.height;
        }

        private void SpawnPreviewModel(DR_PickupItem pickup)
        {
            GameObject source = pickup.DisplayPrefabOverride != null ? pickup.DisplayPrefabOverride : pickup.gameObject;

            Vector3 worldPos = inspectionAnchor.position + inspectionAnchor.TransformVector(pickup.InspectionOffset);
            Quaternion worldRot = inspectionAnchor.rotation * Quaternion.Euler(pickup.InspectionInitialEuler);

            spawnedModel = Instantiate(source, worldPos, worldRot);
            spawnedModel.transform.SetParent(null, worldPositionStays: true);
            spawnedModel.transform.localScale = Vector3.one * pickup.InspectionScale;

            StripGameplayComponents(spawnedModel);
            SetLayerRecursive(spawnedModel, LayerMask.NameToLayer(inspectionLayerName));
        }

        private static void StripGameplayComponents(GameObject root)
        {
            foreach (var pickup in root.GetComponentsInChildren<DR_PickupItem>(true)) Destroy(pickup);
            foreach (var col in root.GetComponentsInChildren<Collider>(true)) Destroy(col);
            foreach (var rb in root.GetComponentsInChildren<Rigidbody>(true)) Destroy(rb);
        }

        private static void SetLayerRecursive(GameObject go, int layer)
        {
            if (layer < 0) return;
            go.layer = layer;
            foreach (Transform child in go.transform) SetLayerRecursive(child.gameObject, layer);
        }

        private void Update()
        {
            if (!isOpen) return;

            HandleRotation();

            if (inputReader == null) return;
            if (Time.frameCount == openedOnFrame) return;

            if (inputReader.InteractPressedThisFrame)
            {
                Close(true);
            }
            else if (inputReader.CancelPressedThisFrame)
            {
                Close(false);
            }
        }

        private void HandleRotation()
        {
            if (spawnedModel == null || inputReader == null) return;

            if (requireHoldToRotate)
            {
                bool mouse = UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.isPressed;
                bool gamepad = UnityEngine.InputSystem.Gamepad.current != null && UnityEngine.InputSystem.Gamepad.current.rightTrigger.isPressed;
                if (!mouse && !gamepad) return;
            }

            Vector2 delta = inputReader.LookDelta;
            if (delta.sqrMagnitude < 0.0001f) return;

            float yaw = (invertHorizontal ? 1f : -1f) * delta.x * rotateSpeedDegPerPixel;
            float pitch = (invertVertical ? -1f : 1f) * delta.y * rotateSpeedDegPerPixel;

            spawnedModel.transform.Rotate(Vector3.up, yaw, Space.World);
            spawnedModel.transform.Rotate(Vector3.right, pitch, Space.World);
        }

        private void Close(bool taken)
        {
            if (!isOpen) return;
            isOpen = false;

            if (spawnedModel != null)
            {
                Destroy(spawnedModel);
                spawnedModel = null;
            }

            if (inspectionCamera != null) inspectionCamera.enabled = false;
            if (popupRoot != null) popupRoot.SetActive(false);
            if (blurBackground != null) blurBackground.Clear();

            Time.timeScale = cachedTimeScale;

            DR_PickupItem closedPickup = currentPickup;
            Action<DR_PickupItem, bool> cb = onClosed;
            currentPickup = null;
            onClosed = null;

            cb?.Invoke(closedPickup, taken);
        }

        private void OnDestroy()
        {
            if (ownedTexture != null)
            {
                ownedTexture.Release();
                Destroy(ownedTexture);
                ownedTexture = null;
            }
        }
    }
}
