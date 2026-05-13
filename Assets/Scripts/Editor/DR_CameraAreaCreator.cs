#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using DementedReality.Gameplay.Player;

namespace DementedReality.Editor
{
    public static class DR_CameraAreaCreator
    {
        [MenuItem("GameObject/Demented Reality/Camera Area", false, 10)]
        private static void CreateCameraArea(MenuCommand menuCommand)
        {
            GameObject area = new GameObject("Camera Area");
            area.transform.SetParent(menuCommand.context is GameObject parent ? parent.transform : null);
            area.transform.localPosition = Vector3.zero;
            area.transform.localRotation = Quaternion.identity;
            area.transform.localScale = Vector3.one;

            BoxCollider collider = area.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(5f, 3f, 5f);

            DR_CameraArea cameraArea = area.AddComponent<DR_CameraArea>();

            GameObject anchor = new GameObject("Camera Anchor");
            anchor.transform.SetParent(area.transform);
            anchor.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            anchor.transform.localRotation = Quaternion.identity;

            cameraArea.SetCameraAnchor(anchor.transform);

            Selection.activeGameObject = area;
            Undo.RegisterCreatedObjectUndo(area, "Create Camera Area");
        }
    }
}
#endif