using UnityEngine;

namespace Platformer
{
    public class AirState : BaseState
    {
        public AirState(PlayerController player, Animator animator) : base(player, animator)
        {
        }
        
        public override void OnEnter()
        {
            // animator.CrossFade(AirHash, crossFadeDuration);
        }
        
        public override void FixedUpdate()
        {
            player.HandleMovement();
            player.HandleJump();
        }
    }
}