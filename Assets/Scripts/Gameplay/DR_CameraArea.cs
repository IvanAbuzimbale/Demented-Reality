using UnityEngine;

namespace DementedReality.Gameplay.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class DR_CameraArea : MonoBehaviour
    {
        [Header("Camera Anchor")]
        [SerializeField] private Transform cameraAnchor;
        [SerializeField] private int priority = 0;
        [SerializeField] private Color gizmoColor = new Color(0.2f, 0.9f, 1f, 0.18f);
        [SerializeField] private Color gizmoWireColor = new Color(0.2f, 0.9f, 1f, 0.9f);
        [SerializeField] private DR_CameraDirector cameraDirector;

        public Transform CameraAnchor => cameraAnchor;
        public int Priority => priority;

        public void SetCameraAnchor(Transform anchor)
        {
            cameraAnchor = anchor;
        }

        private void Reset()
        {
            Collider areaCollider = GetComponent<Collider>();
            areaCollider.isTrigger = true;
        }

        private void Awake()
        {
            if (cameraDirector == null)
            {
                cameraDirector = FindFirstObjectByType<DR_CameraDirector>();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            if (cameraDirector != null)
            {
                cameraDirector.EnterArea(this);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            if (cameraDirector != null)
            {
                cameraDirector.ExitArea(this);
            }
        }

        private void OnDrawGizmos()
        {
            Collider areaCollider = GetComponent<Collider>();
            if (areaCollider == null)
            {
                return;
            }

            Bounds bounds = areaCollider.bounds;
            Gizmos.color = gizmoColor;
            Gizmos.DrawCube(bounds.center, bounds.size);

            Gizmos.color = gizmoWireColor;
            Gizmos.DrawWireCube(bounds.center, bounds.size);

            if (cameraAnchor != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(cameraAnchor.position, 0.1f);
                Gizmos.DrawLine(bounds.center, cameraAnchor.position);
            }
        }
    }
}
