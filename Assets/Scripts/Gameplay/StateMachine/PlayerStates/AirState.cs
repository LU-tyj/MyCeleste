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
            //Debug.Log("Air State");
        }
        
        public override void FixedUpdate()
        {
            player.HandleMovement();
            player.HandleJump();
        }
        
        public override void OnExit()
        {
            //Debug.Log("Exit Air State");
        }
    }
}