using UnityEngine;

namespace DementedReality.Gameplay.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class DR_PlayerMotor : MonoBehaviour
    {
        [Header("Motor")]
        [SerializeField] private float acceleration = 12f;
        [SerializeField] private float rotationSpeed = 12f;
        [SerializeField] private float gravity = -25f;
        [SerializeField] private float groundedStickForce = -2f;

        [Header("Stance")]
        [SerializeField] private float standHeight = 1.8f;
        [SerializeField] private float crouchHeight = 1.1f;
        [SerializeField, Range(0.1f, 1f)] private float crouchStepOffsetMultiplier = 0.5f;

        private CharacterController characterController;
        private Vector3 horizontalVelocity;
        private Vector3 verticalVelocity;

        private float defaultHeight;
        private Vector3 defaultCenter;
        private float defaultStepOffset;
        private bool isCrouched;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();

            defaultHeight = characterController.height;
            defaultCenter = characterController.center;
            defaultStepOffset = characterController.stepOffset;

            if (standHeight <= 0f)
            {
                standHeight = defaultHeight;
            }

            if (crouchHeight <= 0f)
            {
                crouchHeight = standHeight * 0.6f;
            }
        }

        private void OnDisable()
        {
            ResetStance();
            horizontalVelocity = Vector3.zero;
            verticalVelocity = Vector3.zero;
        }

        public void SetCrouched(bool crouched)
        {
            if (isCrouched == crouched)
            {
                return;
            }

            isCrouched = crouched;
            ApplyStance(crouched);
        }

        public void Tick(Vector2 moveInput, float moveSpeed, Transform movementReference, float deltaTime)
        {
            Vector3 moveDirection = GetMoveDirection(moveInput, movementReference);
            Vector3 targetHorizontalVelocity = moveDirection * moveSpeed;

            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetHorizontalVelocity, acceleration * deltaTime);

            if (moveDirection.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotation = GetTargetRotation(moveInput, moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * deltaTime);
            }

            ApplyGravity(deltaTime);

            Vector3 motion = (horizontalVelocity + verticalVelocity) * deltaTime;
            characterController.Move(motion);
        }

        private Vector3 GetMoveDirection(Vector2 moveInput, Transform movementReference)
        {
            if (moveInput.sqrMagnitude < 0.01f)
            {
                return Vector3.zero;
            }

            Vector3 forward = Vector3.forward;
            Vector3 right = Vector3.right;

            if (movementReference != null)
            {
                Vector3 flatForward = Quaternion.Euler(0f, movementReference.eulerAngles.y, 0f) * Vector3.forward;
                Vector3 flatRight = Quaternion.Euler(0f, movementReference.eulerAngles.y, 0f) * Vector3.right;

                if (flatForward.sqrMagnitude > 0.0001f)
                {
                    forward = flatForward.normalized;
                }

                if (flatRight.sqrMagnitude > 0.0001f)
                {
                    right = flatRight.normalized;
                }
            }

            Vector3 direction = (forward * moveInput.y) + (right * moveInput.x);
            return direction.sqrMagnitude > 1f ? direction.normalized : direction;
        }

        private Quaternion GetTargetRotation(Vector2 moveInput, Vector3 moveDirection)
        {
            if (moveInput.y < -0.01f && Mathf.Abs(moveInput.x) < 0.01f)
            {
                return Quaternion.LookRotation(-moveDirection, Vector3.up);
            }

            return Quaternion.LookRotation(moveDirection, Vector3.up);
        }

        private void ApplyGravity(float deltaTime)
        {
            if (characterController.isGrounded && verticalVelocity.y < 0f)
            {
                verticalVelocity.y = groundedStickForce;
            }

            verticalVelocity.y += gravity * deltaTime;
        }

        private void ApplyStance(bool crouched)
        {
            float targetHeight = crouched ? crouchHeight : standHeight;
            float targetStepOffset = crouched ? defaultStepOffset * crouchStepOffsetMultiplier : defaultStepOffset;

            characterController.height = targetHeight;
            characterController.center = new Vector3(
                defaultCenter.x,
                (defaultCenter.y - (defaultHeight * 0.5f)) + (targetHeight * 0.5f),
                defaultCenter.z
            );
            characterController.stepOffset = targetStepOffset;
        }

        private void ResetStance()
        {
            if (characterController == null)
            {
                return;
            }

            characterController.height = defaultHeight;
            characterController.center = defaultCenter;
            characterController.stepOffset = defaultStepOffset;
        }
    }
}
