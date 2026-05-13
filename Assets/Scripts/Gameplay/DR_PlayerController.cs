using UnityEngine;

namespace DementedReality.Gameplay.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DR_PlayerInputReader))]
    [RequireComponent(typeof(DR_PlayerMotor))]
    [RequireComponent(typeof(DR_PlayerAnimatorDriver))]
    public sealed class DR_PlayerController : MonoBehaviour
    {
        [Header("Movement Speeds")]
        [SerializeField] private float walkSpeed = 1.8f;
        [SerializeField] private float runSpeed = 4.5f;
        [SerializeField] private float crouchSpeed = 1.1f;
        [SerializeField, Range(0.01f, 0.5f)] private float inputDeadZone = 0.1f;

        [Header("Movement Reference")]
        [SerializeField] private Transform movementReference;

        private DR_PlayerInputReader inputReader;
        private DR_PlayerMotor motor;
        private DR_PlayerAnimatorDriver animatorDriver;
        private bool isCrouched;
        private DR_PlayerLocomotionState currentState = DR_PlayerLocomotionState.Idle;

        private void Awake()
        {
            inputReader = GetComponent<DR_PlayerInputReader>();
            motor = GetComponent<DR_PlayerMotor>();
            animatorDriver = GetComponent<DR_PlayerAnimatorDriver>();

            if (movementReference == null)
            {
                movementReference = transform;
            }
        }

        private void Start()
        {
            motor.SetCrouched(false);
            ApplyState(DR_PlayerLocomotionState.Idle, true);
        }

        private void Update()
        {
            Vector2 moveInput = inputReader.MoveInput;
            bool hasMovementInput = moveInput.sqrMagnitude > inputDeadZone * inputDeadZone;

            if (inputReader.CrouchToggledThisFrame)
            {
                isCrouched = !isCrouched;
                motor.SetCrouched(isCrouched);
            }

            bool wantsRun = !isCrouched && inputReader.RunHeld && hasMovementInput;
            DR_PlayerLocomotionState desiredState = ResolveState(hasMovementInput, wantsRun, isCrouched);

            ApplyState(desiredState);
            motor.Tick(moveInput, GetMoveSpeed(desiredState), movementReference, Time.deltaTime);
        }

        private void ApplyState(DR_PlayerLocomotionState desiredState, bool force = false)
        {
            if (!force && desiredState == currentState)
            {
                return;
            }

            currentState = desiredState;
            animatorDriver.PlayState(desiredState);
        }

        private DR_PlayerLocomotionState ResolveState(bool hasMovementInput, bool wantsRun, bool crouched)
        {
            if (crouched)
            {
                return hasMovementInput ? DR_PlayerLocomotionState.CrouchWalk : DR_PlayerLocomotionState.CrouchIdle;
            }

            if (!hasMovementInput)
            {
                return DR_PlayerLocomotionState.Idle;
            }

            if (wantsRun)
            {
                return DR_PlayerLocomotionState.Run;
            }

            return DR_PlayerLocomotionState.Walk;
        }

        private float GetMoveSpeed(DR_PlayerLocomotionState state)
        {
            if (state == DR_PlayerLocomotionState.Walk)
            {
                return walkSpeed;
            }

            if (state == DR_PlayerLocomotionState.Run)
            {
                return runSpeed;
            }

            if (state == DR_PlayerLocomotionState.CrouchWalk)
            {
                return crouchSpeed;
            }

            return 0f;
        }
    }
}
