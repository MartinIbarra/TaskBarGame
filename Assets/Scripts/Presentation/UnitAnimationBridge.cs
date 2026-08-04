using System.Collections.Generic;
using UnityEngine;

namespace TaskbarTactics.Presentation
{
    public sealed class UnitAnimationBridge : MonoBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int HitHash = Animator.StringToHash("Hit");
        private static readonly int IsDeadHash = Animator.StringToHash("IsDead");

        [SerializeField] private Animator animator;

        private readonly HashSet<int> parameters = new HashSet<int>();
        private bool isDead;

        public void Configure(Animator targetAnimator)
        {
            animator = targetAnimator;
            CacheParameters();
        }

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            CacheParameters();
        }

        public void PlayAttack()
        {
            if (!isDead && animator != null && parameters.Contains(AttackHash))
            {
                animator.SetTrigger(AttackHash);
            }
        }

        public void PlayHit()
        {
            if (!isDead && animator != null && parameters.Contains(HitHash))
            {
                animator.SetTrigger(HitHash);
            }
        }

        public void SetMovement(float normalizedSpeed)
        {
            if (animator != null && parameters.Contains(SpeedHash))
            {
                animator.SetFloat(SpeedHash, isDead ? 0f : Mathf.Clamp01(normalizedSpeed));
            }
        }

        public void SetDead(bool isDead)
        {
            this.isDead = isDead;
            if (animator != null && parameters.Contains(IsDeadHash))
            {
                if (isDead)
                {
                    SetMovement(0f);
                    if (parameters.Contains(AttackHash))
                    {
                        animator.ResetTrigger(AttackHash);
                    }

                    if (parameters.Contains(HitHash))
                    {
                        animator.ResetTrigger(HitHash);
                    }
                }

                animator.SetBool(IsDeadHash, isDead);
            }
        }

        private void CacheParameters()
        {
            parameters.Clear();
            if (animator == null)
            {
                return;
            }

            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                parameters.Add(parameter.nameHash);
            }
        }
    }
}
