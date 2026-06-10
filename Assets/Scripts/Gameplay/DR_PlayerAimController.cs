using UnityEngine;

namespace DementedReality.Gameplay.Player
{
    /// <summary>
    /// Drives "aim mode": while the Aim button is held, the mouse becomes a free
    /// aiming cursor. A ray is cast from the camera through the cursor to find a
    /// world aim-point; the player turns to face it and the camera shifts to an
    /// over-the-shoulder framing. The shooter reads the aim direction from here.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DR_PlayerInputReader))]
    public sealed class DR_PlayerAimController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Camera used to convert the cursor position into a world ray. Defaults to Camera.main.")]
        [SerializeField] private Camera aimCamera;
        [SerializeField] private DR_CameraDirector cameraDirector;
        [SerializeField] private DR_PlayerMotor motor;

        [Header("Aim Raycast")]
        [Tooltip("Screen point the aim ray passes through, in viewport space (0..1). " +
                 "(0.5, 0.5) = screen center. Keep this matched to where your crosshair sits.")]
        [SerializeField] private Vector2 aimScreenViewport = new Vector2(0.5f, 0.5f);
        [Tooltip("Layers the aim ray can land on. Exclude the Player layer here.")]
        [SerializeField] private LayerMask aimLayers = ~0;
        [SerializeField] private float maxAimDistance = 100f;
        [Tooltip("If the ray hits nothing, aim this far down the ray instead.")]
        [SerializeField] private float aimFallbackDistance = 50f;

        [Header("Rotation")]
        [SerializeField] private bool faceAimWhileAiming = true;
        [SerializeField] private float aimRotationSpeed = 16f;
        [Tooltip("Euler offset added to the aim facing. Use this if the model's forward " +
                 "axis doesn't match the gun direction — e.g. Y = 90 / -90 / 180 to correct a rotated rig.")]
        [SerializeField] private Vector3 aimRotationOffset = Vector3.zero;

        private DR_PlayerInputReader inputReader;
        private bool hasAimPoint;

        public bool IsAiming { get; private set; }
        public Vector3 AimPoint { get; private set; }

        private void Awake()
        {
            inputReader = GetComponent<DR_PlayerInputReader>();

            if (motor == null)
            {
                motor = GetComponent<DR_PlayerMotor>();
            }
            if (aimCamera == null)
            {
                aimCamera = Camera.main;
            }
            if (cameraDirector == null)
            {
                cameraDirector = FindAnyObjectByType<DR_CameraDirector>();
            }
        }

        private void Update()
        {
            IsAiming = inputReader != null && inputReader.AimHeld;

            if (IsAiming)
            {
                UpdateAimPoint();
            }
            else
            {
                hasAimPoint = false;
            }

            // Hand body rotation to this controller only while aiming.
            if (motor != null)
            {
                motor.AllowRotation = !(IsAiming && faceAimWhileAiming);
            }

            if (cameraDirector != null)
            {
                cameraDirector.SetAiming(IsAiming);
            }
        }

        private void LateUpdate()
        {
            if (!IsAiming || !faceAimWhileAiming || !hasAimPoint)
            {
                return;
            }

            Vector3 flat = AimPoint - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.0004f)
            {
                return;
            }

            Quaternion target = Quaternion.LookRotation(flat.normalized, Vector3.up) * Quaternion.Euler(aimRotationOffset);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, aimRotationSpeed * Time.deltaTime);
        }

        private void UpdateAimPoint()
        {
            if (aimCamera == null)
            {
                hasAimPoint = false;
                return;
            }

            Vector3 screenPoint = new Vector3(aimScreenViewport.x * Screen.width, aimScreenViewport.y * Screen.height, 0f);
            Ray ray = aimCamera.ScreenPointToRay(screenPoint);
            if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, aimLayers, QueryTriggerInteraction.Ignore))
            {
                AimPoint = hit.point;
            }
            else
            {
                AimPoint = ray.GetPoint(aimFallbackDistance);
            }

            hasAimPoint = true;
        }

        /// <summary>
        /// Direction from a muzzle/origin toward the current aim-point.
        /// Returns false when not aiming so the shooter can fall back to firePoint.forward.
        /// </summary>
        public bool TryGetAimDirection(Vector3 fromPosition, out Vector3 direction)
        {
            if (!IsAiming || !hasAimPoint)
            {
                direction = Vector3.zero;
                return false;
            }

            Vector3 d = AimPoint - fromPosition;
            if (d.sqrMagnitude < 0.0001f)
            {
                direction = Vector3.zero;
                return false;
            }

            direction = d.normalized;
            return true;
        }
    }
}
