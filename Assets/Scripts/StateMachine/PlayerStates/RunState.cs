using UnityEngine;

namespace Platformer
{
    public class RunState : BaseState
    {
        public RunState(PlayerController player, Animator animator) : base(player, animator)
        {
        }

        public override void OnEnter()
        {
            // animator.CrossFade(RunHash, crossFadeDuration);
        }

        public override void FixedUpdate()
        {
            player.HandleMovement();
        }
    }
}