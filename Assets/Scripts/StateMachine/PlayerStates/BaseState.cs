using UnityEngine;

namespace Platformer
{
    public abstract class BaseState : IState
    {
        protected readonly PlayerController player;
        protected readonly Animator animator;
        
        protected static readonly int JumpHash = Animator.StringToHash("Jump");
        protected static readonly int LocomotionHash = Animator.StringToHash("Locomotion");
        // protected static readonly int RunHash = Animator.StringToHash("Run");
        // protected static readonly int idleHash = Animator.StringToHash("Idle");
        
        protected const float crossFadeDuration = 0.0f;

        protected BaseState(PlayerController player, Animator animator)
        {
            this.player = player;
            this.animator = animator;
        }
        public virtual void OnEnter()
        {
            // noop
        }

        public virtual void OnExit()
        {
            // noop
        }

        public virtual void Update()
        {
            // noop
        }

        public virtual void FixedUpdate()
        {
            // noop
        }
    }
}