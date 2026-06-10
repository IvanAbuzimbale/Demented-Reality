using UnityEngine;
using UnityEngine.InputSystem;

namespace DementedReality.Gameplay.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInput))]
    public sealed class DR_PlayerInputReader : MonoBehaviour
    {
        [Header("Input Actions")]
        [SerializeField] private string actionMapName = "Player";
        [SerializeField] private string moveActionName = "Move";
        [SerializeField] private string runActionName = "Run";
        [SerializeField] private string crouchActionName = "Crouch";
        [SerializeField] private string interactActionName = "Interact";
        [SerializeField] private string cancelActionName = "Cancel";
        [SerializeField] private string lookActionName = "Look";
        [SerializeField] private string shootActionName = "Shoot";
        [SerializeField] private string reloadActionName = "Reload";
        [SerializeField] private string aimActionName = "Aim";

        private PlayerInput playerInput;
        private InputAction moveAction;
        private InputAction runAction;
        private InputAction crouchAction;
        private InputAction interactAction;
        private InputAction cancelAction;
        private InputAction lookAction;
        private InputAction shootAction;
        private InputAction reloadAction;
        private InputAction aimAction;

        public Vector2 MoveInput => moveAction == null ? Vector2.zero : Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);
        public bool RunHeld => runAction != null && runAction.IsPressed();
        public bool CrouchToggledThisFrame => crouchAction != null && crouchAction.WasPressedThisFrame();
        public bool InteractPressedThisFrame => interactAction != null && interactAction.WasPressedThisFrame();
        public bool CancelPressedThisFrame => cancelAction != null && cancelAction.WasPressedThisFrame();
        public Vector2 LookDelta => lookAction == null ? Vector2.zero : lookAction.ReadValue<Vector2>();
        public bool ShootPressedThisFrame => shootAction != null && shootAction.WasPressedThisFrame();
        public bool ShootHeld => shootAction != null && shootAction.IsPressed();
        public bool ReloadPressedThisFrame => reloadAction != null && reloadAction.WasPressedThisFrame();
        public bool AimHeld => aimAction != null && aimAction.IsPressed();

        // Absolute pointer (mouse/touch) position in screen pixels. Used to cast an aim ray.
        public Vector2 PointerPosition => Pointer.current != null ? Pointer.current.position.ReadValue() : Vector2.zero;

        private void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
            CacheInputActions();
        }

        private void OnEnable()
        {
            if (playerInput != null && !string.IsNullOrWhiteSpace(actionMapName))
            {
                playerInput.SwitchCurrentActionMap(actionMapName);
            }
        }

        private void CacheInputActions()
        {
            if (playerInput == null || playerInput.actions == null)
            {
                return;
            }

            InputActionMap actionMap = playerInput.actions.FindActionMap(actionMapName, false);
            if (actionMap == null)
            {
                return;
            }

            moveAction = actionMap.FindAction(moveActionName, false);
            runAction = actionMap.FindAction(runActionName, false);
            crouchAction = actionMap.FindAction(crouchActionName, false);
            interactAction = actionMap.FindAction(interactActionName, false);
            cancelAction = actionMap.FindAction(cancelActionName, false);
            lookAction = actionMap.FindAction(lookActionName, false);
            shootAction = actionMap.FindAction(shootActionName, false);
            reloadAction = actionMap.FindAction(reloadActionName, false);
            aimAction = actionMap.FindAction(aimActionName, false);
        }
    }
}
