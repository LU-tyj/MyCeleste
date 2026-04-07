using UnityEngine;

namespace Platformer
{
    public class LocomotionState : BaseState
    {
        public LocomotionState(PlayerController player, Animator animator) : base(player, animator)
        {
        }
        
        public override void OnEnter()
        {
            animator.CrossFade(LocomotionHash, crossFadeDuration);
            //Debug.Log("Entered LocomotionState");
        }
        
        public override void FixedUpdate()
        {
            player.HandleMovement();
        }
    }
}