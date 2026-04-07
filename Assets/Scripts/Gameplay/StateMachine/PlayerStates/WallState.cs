using UnityEngine;

namespace Platformer
{
    public class WallState : BaseState
    {
        public WallState(PlayerController player, Animator animator) : base(player, animator)
        {
        }
        
        public override void OnEnter()
        {
            animator.CrossFade(WallHash, crossFadeDuration);
            //Debug.Log("Entered WallState");
            
            player.ApplyInitialWallStats();
        }
        
        public override void FixedUpdate()
        {
            player.HandleWallSlide();
            player.HandleWallJump();
        }

        public override void OnExit()
        {
            player.ApplyEndOfWallStats();
        }
    }
}