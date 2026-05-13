using UnityEngine;

namespace DementedReality.Gameplay.Player
{
    [DisallowMultipleComponent]
    public sealed class DR_CameraDirector : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform followTarget;
        [SerializeField] private Transform cameraTransform;

        [Header("Normal Camera")]
        [SerializeField] private Vector3 followOffset = new Vector3(0f, 1.8f, -4.5f);
        [SerializeField] private Vector3 lookOffset = new Vector3(0f, 1.4f, 0f);
        [SerializeField] private float followPositionSpeed = 12f;
        [SerializeField] private float followRotationSpeed = 12f;
        [SerializeField, Range(0.1f, 20f)] private float followYawSpeed = 6f;

        [Header("Fixed Camera Zone")]
        [SerializeField] private float fixedPositionSpeed = 10f;
        [SerializeField] private float fixedRotationSpeed = 10f;
        [SerializeField] private Vector3 fixedLookOffset = new Vector3(0f, 1.4f, 0f);

        [Header("Debug")]
        [SerializeField] private bool drawGizmos = true;
        [SerializeField] private Color followGizmoColor = new Color(0.3f, 1f, 0.35f, 1f);
        [SerializeField] private Color fixedGizmoColor = new Color(1f, 0.45f, 0.2f, 1f);

        private DR_CameraArea activeArea;

        private void Awake()
        {
            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        private void LateUpdate()
        {
            if (cameraTransform == null || followTarget == null)
            {
                return;
            }

            if (activeArea != null)
            {
                UpdateFixedCamera(activeArea);
                return;
            }

            UpdateFollowCamera();
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

            Gizmos.color = followGizmoColor;
            Vector3 desiredPosition = followTarget.position + followTarget.TransformDirection(followOffset);
            Gizmos.DrawLine(followTarget.position, desiredPosition);
            Gizmos.DrawSphere(desiredPosition, 0.1f);
        }

        public void SetFollowTarget(Transform target)
        {
            followTarget = target;
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
    }
}
