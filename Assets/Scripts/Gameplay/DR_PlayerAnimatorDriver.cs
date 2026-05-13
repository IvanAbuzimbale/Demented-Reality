using UnityEngine;

namespace DementedReality.Gameplay.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class DR_PlayerAnimatorDriver : MonoBehaviour
    {
        [Header("Animator State Names")]
        [SerializeField] private string idleStateName = "Idle";
        [SerializeField] private string walkStateName = "Walk";
        [SerializeField] private string runStateName = "Run";
        [SerializeField] private string crouchIdleStateName = "CrouchIdle";
        [SerializeField] private string crouchWalkStateName = "CrouchWalk";
        [SerializeField] private float animationFadeTime = 0.12f;

        private Animator animator;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            animator.applyRootMotion = false;
        }

        public void PlayState(DR_PlayerLocomotionState state)
        {
            string stateName = GetStateName(state);
            if (string.IsNullOrWhiteSpace(stateName))
            {
                return;
            }

            animator.CrossFadeInFixedTime(stateName, animationFadeTime);
        }

        private string GetStateName(DR_PlayerLocomotionState state)
        {
            if (state == DR_PlayerLocomotionState.Walk)
            {
                return walkStateName;
            }

            if (state == DR_PlayerLocomotionState.Run)
            {
                return runStateName;
            }

            if (state == DR_PlayerLocomotionState.CrouchIdle)
            {
                return crouchIdleStateName;
            }

            if (state == DR_PlayerLocomotionState.CrouchWalk)
            {
                return crouchWalkStateName;
            }

            return idleStateName;
        }
    }
}
