using UnityEngine;

namespace Platformer
{
    public class JumpState : BaseState
    {
        public JumpState(PlayerController player, Animator animator) : base(player, animator)
        {
        }

        public override void OnEnter()
        {
            // animator.CrossFade(JumpHash, crossFadeDuration);
            //Debug.Log("OnEnter JumpState");
            player.ApplyInitialJumpStats();
        }

        public override void FixedUpdate()
        {
            player.HandleJump();
            player.HandleMovement();
        }

        public override void OnExit()
        {
            //Debug.Log("OnExit JumpState");
            player.ApplyEndOfJumpStats();
        }
    }
}