using System.Collections;
using UnityEngine;

namespace DementedReality.Gameplay.Player
{
    /// <summary>
    /// 3D hitscan shooter. The weapon is drawn while aiming (DR_PlayerAimController)
    /// and shots travel toward the cursor aim-point. Weapon animations (pull-out,
    /// holding, reload) play on a dedicated upper-body Animator layer so the player
    /// can move while armed. The gunshot itself is procedural (raycast + muzzle
    /// flash + recoil kick) since there is no dedicated fire clip.
    ///
    /// If no aim controller is assigned the gun is simply always out and fires
    /// along firePoint.forward.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DR_PlayerInputReader))]
    [RequireComponent(typeof(Animator))]
    public sealed class DR_PlayerShooter : MonoBehaviour
    {
        [Header("Firing")]
        [SerializeField] private bool automaticFire = false;
        [SerializeField] private float fireRate = 6f;
        [SerializeField] private float range = 50f;
        [SerializeField] private int damage = 1;
        [SerializeField] private LayerMask hitLayers = ~0;
        [Tooltip("Only allow firing while aiming. Off = hip-fire along firePoint.forward is allowed too.")]
        [SerializeField] private bool requireAimToFire = true;

        [Header("Ammo")]
        [SerializeField] private int magazineSize = 15;
        [SerializeField] private bool autoReloadWhenEmpty = true;

        [Header("Audio (SFX names registered in AudioManager)")]
        [SerializeField] private string shootSFX = "Shoot";
        [SerializeField] private string emptySFX = "EmptyGun";
        [SerializeField] private string reloadSFX = "Reload";

        [Header("References")]
        [SerializeField] private DR_PlayerAimController aimController;
        [Tooltip("Empty transform at the muzzle, pointing +Z (forward).")]
        [SerializeField] private Transform firePoint;
        [SerializeField] private GameObject muzzleFlashPrefab;
        [SerializeField] private GameObject impactPrefab;
        [SerializeField] private float spawnedEffectLifetime = 2f;

        [Header("Animator (upper-body layer)")]
        [SerializeField] private int upperBodyLayerIndex = 1;
        [SerializeField] private float layerBlendSpeed = 8f;
        [SerializeField] private string pullOutTrigger = "PullOut";
        [SerializeField] private string reloadTrigger = "Reload";
        [Tooltip("Seconds the pull-out clip takes before the gun is ready to fire.")]
        [SerializeField] private float pullOutDuration = 0.8f;
        [Tooltip("Seconds the reload clip takes before ammo is refilled.")]
        [SerializeField] private float reloadDuration = 1.5f;

        [Header("Procedural Recoil")]
        [Tooltip("The gun transform to kick on each shot. Leave empty to disable recoil.")]
        [SerializeField] private Transform recoilPivot;
        [SerializeField] private Vector3 recoilKickEuler = new Vector3(-12f, 0f, 0f);
        [SerializeField] private Vector3 recoilKickOffset = new Vector3(0f, 0f, -0.04f);
        [SerializeField] private float recoilReturnSpeed = 14f;

        private DR_PlayerInputReader inputReader;
        private Animator animator;

        private bool armed;          // true once the pull-out finished and the gun is ready
        private bool busy;           // true while pulling out or reloading
        private float nextFireTime;
        private int ammoInMag;
        private float currentLayerWeight;

        private Vector3 recoilBaseLocalPosition;
        private Quaternion recoilBaseLocalRotation;
        private Vector3 recoilPositionOffset;
        private Quaternion recoilRotationOffset = Quaternion.identity;

        public int AmmoInMagazine => ammoInMag;
        public int MagazineSize => magazineSize;
        public bool IsArmed => armed;

        private void Awake()
        {
            inputReader = GetComponent<DR_PlayerInputReader>();
            animator = GetComponent<Animator>();

            if (aimController == null)
            {
                aimController = GetComponent<DR_PlayerAimController>();
            }
            if (firePoint == null)
            {
                firePoint = transform;
            }
            if (recoilPivot != null)
            {
                recoilBaseLocalPosition = recoilPivot.localPosition;
                recoilBaseLocalRotation = recoilPivot.localRotation;
            }

            ammoInMag = magazineSize;
        }

        private void Start()
        {
            SetUpperBodyWeight(0f);
        }

        private void Update()
        {
            HandleWeaponState();
            HandleFiring();
            HandleReload();
            BlendUpperBodyLayer();
        }

        private void LateUpdate()
        {
            ApplyRecoil(Time.deltaTime);
        }

        // Draw the gun when aiming begins, holster it when aiming ends.
        // With no aim controller the gun is simply always wanted.
        private void HandleWeaponState()
        {
            bool wantWeapon = aimController == null || aimController.IsAiming;

            if (wantWeapon && !armed && !busy)
            {
                StartCoroutine(PullOutRoutine());
            }
            else if (!wantWeapon && armed && !busy)
            {
                armed = false; // holster: the upper-body layer blends back down
            }
        }

        private void HandleFiring()
        {
            if (!armed || busy)
            {
                return;
            }

            if (requireAimToFire && aimController != null && !aimController.IsAiming)
            {
                return;
            }

            bool wantsToShoot = automaticFire ? inputReader.ShootHeld : inputReader.ShootPressedThisFrame;
            if (!wantsToShoot)
            {
                return;
            }

            if (ammoInMag <= 0)
            {
                // Dry-fire: react only to an actual press so it doesn't spam in auto mode.
                if (inputReader.ShootPressedThisFrame)
                {
                    PlaySfx(emptySFX);
                    if (autoReloadWhenEmpty)
                    {
                        StartCoroutine(ReloadRoutine());
                    }
                }
                return;
            }

            if (Time.time < nextFireTime)
            {
                return;
            }

            Fire();
            nextFireTime = Time.time + 1f / Mathf.Max(0.01f, fireRate);
        }

        private void HandleReload()
        {
            if (busy || !armed)
            {
                return;
            }

            if (inputReader.ReloadPressedThisFrame && ammoInMag < magazineSize)
            {
                StartCoroutine(ReloadRoutine());
            }
        }

        private void Fire()
        {
            ammoInMag--;
            PlaySfx(shootSFX);

            if (muzzleFlashPrefab != null)
            {
                GameObject flash = Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation, firePoint);
                if (spawnedEffectLifetime > 0f)
                {
                    Destroy(flash, spawnedEffectLifetime);
                }
            }

            AddRecoilKick();

            Vector3 origin = firePoint.position;
            Vector3 direction = firePoint.forward;
            if (aimController != null && aimController.TryGetAimDirection(origin, out Vector3 aimDir))
            {
                direction = aimDir;
            }

            if (Physics.Raycast(origin, direction, out RaycastHit hit, range, hitLayers))
            {
                if (hit.collider.TryGetComponent<Health>(out Health health)
                    && health.canTakeDamage
                    && health.GetCurrentHealth() > 0)
                {
                    health.TakeDamage(damage);
                }

                if (impactPrefab != null)
                {
                    GameObject impact = Instantiate(impactPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                    if (spawnedEffectLifetime > 0f)
                    {
                        Destroy(impact, spawnedEffectLifetime);
                    }
                }
            }
        }

        private IEnumerator PullOutRoutine()
        {
            busy = true;
            TriggerAnimator(pullOutTrigger);

            yield return new WaitForSeconds(pullOutDuration);

            armed = true;
            busy = false;
            // Small cooldown so drawing the gun does not also fire on the same frame.
            nextFireTime = Time.time + 1f / Mathf.Max(0.01f, fireRate);
        }

        private IEnumerator ReloadRoutine()
        {
            busy = true;
            TriggerAnimator(reloadTrigger);
            PlaySfx(reloadSFX);

            yield return new WaitForSeconds(reloadDuration);

            ammoInMag = magazineSize;
            busy = false;
        }

        private void TriggerAnimator(string triggerName)
        {
            if (animator != null && !string.IsNullOrWhiteSpace(triggerName))
            {
                animator.SetTrigger(triggerName);
            }
        }

        private void PlaySfx(string sfxName)
        {
            if (!string.IsNullOrWhiteSpace(sfxName) && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(sfxName);
            }
        }

        private void BlendUpperBodyLayer()
        {
            float target = (armed || busy) ? 1f : 0f;
            currentLayerWeight = Mathf.MoveTowards(currentLayerWeight, target, layerBlendSpeed * Time.deltaTime);
            SetUpperBodyWeight(currentLayerWeight);
        }

        private void SetUpperBodyWeight(float weight)
        {
            if (animator != null && upperBodyLayerIndex > 0 && upperBodyLayerIndex < animator.layerCount)
            {
                animator.SetLayerWeight(upperBodyLayerIndex, weight);
            }
        }

        private void AddRecoilKick()
        {
            if (recoilPivot == null)
            {
                return;
            }

            recoilPositionOffset += recoilKickOffset;
            recoilRotationOffset *= Quaternion.Euler(recoilKickEuler);
        }

        private void ApplyRecoil(float deltaTime)
        {
            if (recoilPivot == null)
            {
                return;
            }

            recoilPositionOffset = Vector3.Lerp(recoilPositionOffset, Vector3.zero, recoilReturnSpeed * deltaTime);
            recoilRotationOffset = Quaternion.Slerp(recoilRotationOffset, Quaternion.identity, recoilReturnSpeed * deltaTime);

            recoilPivot.localPosition = recoilBaseLocalPosition + recoilPositionOffset;
            recoilPivot.localRotation = recoilBaseLocalRotation * recoilRotationOffset;
        }
    }
}
