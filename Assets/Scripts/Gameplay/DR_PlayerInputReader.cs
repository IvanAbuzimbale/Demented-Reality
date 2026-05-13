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

        private PlayerInput playerInput;
        private InputAction moveAction;
        private InputAction runAction;
        private InputAction crouchAction;

        public Vector2 MoveInput => moveAction == null ? Vector2.zero : Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);
        public bool RunHeld => runAction != null && runAction.IsPressed();
        public bool CrouchToggledThisFrame => crouchAction != null && crouchAction.WasPressedThisFrame();

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
        }
    }
}
