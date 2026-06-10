using UnityEngine;

namespace DementedReality.Gameplay.Player
{
    [DisallowMultipleComponent]
    public sealed class DR_CameraDirector : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform followTarget;
        [SerializeField] private Transform cameraTransform;
        [Tooltip("Player input reader used by the orbit camera. Auto-located on followTarget if null.")]
        [SerializeField] private DR_PlayerInputReader inputReader;

        [Header("Normal Camera (legacy follow)")]
        [SerializeField] private Vector3 followOffset = new Vector3(0f, 1.8f, -4.5f);
        [SerializeField] private Vector3 lookOffset = new Vector3(0f, 1.4f, 0f);
        [SerializeField] private float followPositionSpeed = 12f;
        [SerializeField] private float followRotationSpeed = 12f;
        [SerializeField, Range(0.1f, 20f)] private float followYawSpeed = 6f;

        [Header("Fixed Camera Zone")]
        [SerializeField] private float fixedPositionSpeed = 10f;
        [SerializeField] private float fixedRotationSpeed = 10f;
        [SerializeField] private Vector3 fixedLookOffset = new Vector3(0f, 1.4f, 0f);

        [Header("Orbit Camera (free)")]
        [Tooltip("If true and no DR_CameraArea is active, the camera orbits around the player using LookDelta input.")]
        [SerializeField] private bool useOrbitWhenFree = true;
        [SerializeField] private Vector3 orbitPivotOffset = new Vector3(0f, 1.4f, 0f);
        [Tooltip("Optional aim target. When set, the orbit pivot is blended between the player and this point.")]
        [SerializeField] private Transform aimTarget;
        [Tooltip("0 = orbit purely around player, 1 = orbit purely around aim target, 0.5 = midpoint.")]
        [SerializeField, Range(0f, 1f)] private float aimBlend = 0.5f;
        [SerializeField, Min(0.1f)] private float orbitDistance = 4.5f;
        [SerializeField] private Vector2 orbitMouseSensitivity = new Vector2(0.2f, 0.15f);
        [SerializeField, Range(-89f, 89f)] private float orbitPitchMin = -30f;
        [SerializeField, Range(-89f, 89f)] private float orbitPitchMax = 70f;
        [SerializeField, Range(0.1f, 30f)] private float orbitPositionSmoothing = 14f;
        [SerializeField, Range(0.1f, 30f)] private float orbitRotationSmoothing = 18f;
        [SerializeField] private bool orbitInvertY = false;

        [Header("Aim Camera (over-the-shoulder)")]
        [Tooltip("Sideways/up shoulder offset while aiming. x = right, y = up.")]
        [SerializeField] private Vector3 aimShoulderOffset = new Vector3(0.7f, 0.2f, 0f);
        [SerializeField, Min(0.1f)] private float aimDistance = 2.5f;
        [SerializeField, Range(0.1f, 30f)] private float aimPositionSmoothing = 18f;
        [SerializeField, Range(0.1f, 30f)] private float aimRotationSmoothing = 22f;

        [Header("Cursor")]
        [SerializeField] private bool manageCursor = true;

        [Header("Debug")]
        [SerializeField] private bool drawGizmos = true;
        [SerializeField] private Color followGizmoColor = new Color(0.3f, 1f, 0.35f, 1f);
        [SerializeField] private Color fixedGizmoColor = new Color(1f, 0.45f, 0.2f, 1f);
        [SerializeField] private Color orbitGizmoColor = new Color(0.4f, 0.6f, 1f, 1f);

        private DR_CameraArea activeArea;
        private float orbitYaw;
        private float orbitPitch;
        private bool orbitSeeded;
        private bool aiming;

        private void Awake()
        {
            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
            if (inputReader == null && followTarget != null)
            {
                inputReader = followTarget.GetComponent<DR_PlayerInputReader>();
            }
            if (inputReader == null)
            {
                inputReader = FindAnyObjectByType<DR_PlayerInputReader>();
            }
        }

        private void Start()
        {
            SeedOrbitFromCurrentCamera();
        }

        private void LateUpdate()
        {
            if (cameraTransform == null || followTarget == null)
            {
                return;
            }

            if (aiming)
            {
                if (!orbitSeeded)
                {
                    SeedOrbitFromCurrentCamera();
                }

                UpdateAimCamera();
                UpdateCursor(true);
                return;
            }

            if (activeArea != null)
            {
                UpdateFixedCamera(activeArea);
                UpdateCursor(false);
                return;
            }

            if (useOrbitWhenFree)
            {
                UpdateOrbitCamera();
                UpdateCursor(true);
                return;
            }

            UpdateFollowCamera();
            UpdateCursor(false);
        }

        public void SetFollowTarget(Transform target)
        {
            followTarget = target;
        }

        public void SetAiming(bool value)
        {
            aiming = value;
        }

        public void EnterArea(DR_CameraArea area)
        {
            if (area == null)
            {
                return;
            }

            if (activeArea != null && activeArea.Priority > area.Priority)
            {
                return;
            }

            activeArea = area;
        }

        public void ExitArea(DR_CameraArea area)
        {
            if (activeArea == area)
            {
                activeArea = null;
                SeedOrbitFromCurrentCamera();
            }
        }

        private void UpdateFollowCamera()
        {
            Vector3 desiredPosition = followTarget.position + followTarget.TransformDirection(followOffset);
            Vector3 desiredLookTarget = followTarget.position + followTarget.TransformDirection(lookOffset);
            Quaternion desiredRotation = Quaternion.LookRotation(desiredLookTarget - desiredPosition, Vector3.up);

            float targetYaw = followTarget.eulerAngles.y;
            Vector3 currentEuler = cameraTransform.rotation.eulerAngles;
            Quaternion yawOnlyRotation = Quaternion.Euler(currentEuler.x, targetYaw, currentEuler.z);
            Quaternion smoothedRotation = Quaternion.Slerp(cameraTransform.rotation, yawOnlyRotation, followYawSpeed * Time.deltaTime);

            cameraTransform.position = Vector3.Lerp(cameraTransform.position, desiredPosition, followPositionSpeed * Time.deltaTime);
            cameraTransform.rotation = Quaternion.Slerp(smoothedRotation, desiredRotation, followRotationSpeed * Time.deltaTime);
        }

        private void UpdateFixedCamera(DR_CameraArea area)
        {
            Transform anchor = area.CameraAnchor;
            if (anchor == null)
            {
                activeArea = null;
                return;
            }

            Vector3 desiredPosition = anchor.position;
            Vector3 desiredLookTarget = followTarget.position + fixedLookOffset;
            Quaternion desiredRotation = Quaternion.LookRotation(desiredLookTarget - desiredPosition, Vector3.up);

            cameraTransform.position = Vector3.Lerp(cameraTransform.position, desiredPosition, fixedPositionSpeed * Time.deltaTime);
            cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, desiredRotation, fixedRotationSpeed * Time.deltaTime);
        }

        private void UpdateOrbitCamera()
        {
            if (!orbitSeeded)
            {
                SeedOrbitFromCurrentCamera();
            }

            if (inputReader != null && Time.timeScale > 0f)
            {
                Vector2 look = inputReader.LookDelta;
                orbitYaw += look.x * orbitMouseSensitivity.x;
                float pitchDelta = look.y * orbitMouseSensitivity.y * (orbitInvertY ? 1f : -1f);
                orbitPitch = Mathf.Clamp(orbitPitch + pitchDelta, orbitPitchMin, orbitPitchMax);
            }

            Vector3 pivot = ResolveOrbitPivot();
            Quaternion orbitRotation = Quaternion.Euler(orbitPitch, orbitYaw, 0f);
            Vector3 desiredPosition = pivot + orbitRotation * new Vector3(0f, 0f, -orbitDistance);
            Quaternion desiredRotation = Quaternion.LookRotation(pivot - desiredPosition, Vector3.up);

            float dt = Time.deltaTime;
            cameraTransform.position = Vector3.Lerp(cameraTransform.position, desiredPosition, orbitPositionSmoothing * dt);
            cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, desiredRotation, orbitRotationSmoothing * dt);
        }

        private void UpdateAimCamera()
        {
            // Same mouse-driven orbit as the free camera, but pulled in over one
            // shoulder. The crosshair stays fixed at screen center, so you aim by
            // turning the camera until the crosshair sits on the target.
            if (inputReader != null && Time.timeScale > 0f)
            {
                Vector2 look = inputReader.LookDelta;
                orbitYaw += look.x * orbitMouseSensitivity.x;
                float pitchDelta = look.y * orbitMouseSensitivity.y * (orbitInvertY ? 1f : -1f);
                orbitPitch = Mathf.Clamp(orbitPitch + pitchDelta, orbitPitchMin, orbitPitchMax);
            }

            Quaternion rotation = Quaternion.Euler(orbitPitch, orbitYaw, 0f);
            Vector3 pivot = followTarget.position + orbitPivotOffset;
            Vector3 shoulder = rotation * new Vector3(aimShoulderOffset.x, aimShoulderOffset.y, 0f);
            Vector3 framedPivot = pivot + shoulder;

            Vector3 desiredPosition = framedPivot + rotation * new Vector3(0f, 0f, -aimDistance);
            Quaternion desiredRotation = Quaternion.LookRotation(framedPivot - desiredPosition, Vector3.up);

            float dt = Time.deltaTime;
            cameraTransform.position = Vector3.Lerp(cameraTransform.position, desiredPosition, aimPositionSmoothing * dt);
            cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, desiredRotation, aimRotationSmoothing * dt);
        }

        private Vector3 ResolveOrbitPivot()
        {
            Vector3 playerPivot = followTarget.position + orbitPivotOffset;
            if (aimTarget == null) return playerPivot;
            return Vector3.Lerp(playerPivot, aimTarget.position, aimBlend);
        }

        private void SeedOrbitFromCurrentCamera()
        {
            if (cameraTransform == null || followTarget == null) return;

            Vector3 pivot = ResolveOrbitPivot();
            Vector3 toCam = cameraTransform.position - pivot;
            if (toCam.sqrMagnitude < 0.0001f)
            {
                toCam = -followTarget.forward * orbitDistance;
            }

            Vector3 flat = new Vector3(toCam.x, 0f, toCam.z);
            float yaw = flat.sqrMagnitude > 0.0001f ? Mathf.Atan2(toCam.x, toCam.z) * Mathf.Rad2Deg : 0f;
            float horizontal = flat.magnitude;
            float pitch = Mathf.Atan2(toCam.y, Mathf.Max(0.0001f, horizontal)) * Mathf.Rad2Deg;

            orbitYaw = yaw;
            orbitPitch = Mathf.Clamp(pitch, orbitPitchMin, orbitPitchMax);
            orbitSeeded = true;
        }

        private void UpdateCursor(bool orbiting)
        {
            if (!manageCursor) return;

            bool shouldLock = orbiting && Time.timeScale > 0f;
            CursorLockMode desired = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
            if (Cursor.lockState != desired) Cursor.lockState = desired;
            bool wantVisible = !shouldLock;
            if (Cursor.visible != wantVisible) Cursor.visible = wantVisible;
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos || followTarget == null || cameraTransform == null)
            {
                return;
            }

            if (activeArea != null && activeArea.CameraAnchor != null)
            {
                Gizmos.color = fixedGizmoColor;
                Gizmos.DrawLine(followTarget.position, activeArea.CameraAnchor.position);
                Gizmos.DrawSphere(activeArea.CameraAnchor.position, 0.12f);
                return;
            }

            if (useOrbitWhenFree)
            {
                Vector3 playerPivot = followTarget.position + orbitPivotOffset;
                Vector3 pivot = aimTarget != null ? Vector3.Lerp(playerPivot, aimTarget.position, aimBlend) : playerPivot;
                Gizmos.color = orbitGizmoColor;
                Gizmos.DrawWireSphere(pivot, orbitDistance);
                Gizmos.DrawSphere(pivot, 0.08f);
                if (aimTarget != null)
                {
                    Gizmos.DrawLine(playerPivot, aimTarget.position);
                    Gizmos.DrawSphere(aimTarget.position, 0.07f);
                }
                return;
            }

            Gizmos.color = followGizmoColor;
            Vector3 desiredPosition = followTarget.position + followTarget.TransformDirection(followOffset);
            Gizmos.DrawLine(followTarget.position, desiredPosition);
            Gizmos.DrawSphere(desiredPosition, 0.1f);
        }
    }
}
