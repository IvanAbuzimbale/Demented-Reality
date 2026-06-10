using UnityEngine;
using UnityEngine.UI;

namespace DementedReality.Gameplay.Player
{
    /// <summary>
    /// Shows/hides a fixed crosshair (reticle) while aiming. The crosshair stays
    /// where you place it on the Canvas (screen center by default) — you aim by
    /// turning the camera until it sits on the target, not by moving the cursor.
    ///
    /// Visibility is toggled on the Image's renderer (not the GameObject's active
    /// state), so this works even if the component sits on the crosshair object
    /// itself — it never disables the GameObject it lives on.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DR_CrosshairUI : MonoBehaviour
    {
        [SerializeField] private DR_PlayerAimController aimController;
        [SerializeField] private RectTransform crosshair;
        [SerializeField] private bool hideWhenNotAiming = true;

        private Graphic[] graphics;

        private void Awake()
        {
            if (aimController == null)
            {
                aimController = FindAnyObjectByType<DR_PlayerAimController>();
            }

            if (aimController == null)
            {
                Debug.LogWarning("DR_CrosshairUI: no DR_PlayerAimController found — crosshair will never show.", this);
            }
            if (crosshair == null)
            {
                Debug.LogWarning("DR_CrosshairUI: 'Crosshair' is not assigned — nothing to show or hide.", this);
            }
            else
            {
                graphics = crosshair.GetComponentsInChildren<Graphic>(true);
            }
        }

        private void Update()
        {
            if (graphics == null || graphics.Length == 0)
            {
                return;
            }

            bool show = !hideWhenNotAiming || (aimController != null && aimController.IsAiming);

            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] != null && graphics[i].enabled != show)
                {
                    graphics[i].enabled = show;
                }
            }
        }
    }
}
