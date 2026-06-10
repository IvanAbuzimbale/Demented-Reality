using UnityEngine;

namespace DementedReality.Gameplay.Player
{
    /// <summary>
    /// Mirrors the shooter's ammo into the HUD. The shooter (DR_PlayerShooter) is
    /// the single source of truth; this polls its ammo and pushes it to HUDManager
    /// whenever it changes (fire, reload, start).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DR_AmmoHudBinder : MonoBehaviour
    {
        [SerializeField] private DR_PlayerShooter shooter;
        [SerializeField] private HUDManager hud;

        private int lastAmmo = -1;
        private int lastMax = -1;

        private void Awake()
        {
            if (shooter == null)
            {
                shooter = FindAnyObjectByType<DR_PlayerShooter>();
            }
            if (hud == null)
            {
                hud = FindAnyObjectByType<HUDManager>();
            }

            if (shooter == null)
            {
                Debug.LogWarning("DR_AmmoHudBinder: no DR_PlayerShooter found — ammo UI will not update.", this);
            }
            if (hud == null)
            {
                Debug.LogWarning("DR_AmmoHudBinder: no HUDManager found — ammo UI will not update.", this);
            }
        }

        private void Update()
        {
            if (shooter == null || hud == null)
            {
                return;
            }

            int ammo = shooter.AmmoInMagazine;
            int max = shooter.MagazineSize;
            if (ammo == lastAmmo && max == lastMax)
            {
                return;
            }

            lastAmmo = ammo;
            lastMax = max;
            hud.SetAmmo(ammo, max);
        }
    }
}
