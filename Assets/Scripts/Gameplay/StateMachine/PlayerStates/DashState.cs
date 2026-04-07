using UnityEngine;

namespace Platformer
{
    public class DashState : BaseState
    {
        public DashState(PlayerController player, Animator animator) : base(player, animator)
        {
        }
        
        public override void OnEnter()
        {
            // animator.CrossFade(JumpHash, crossFadeDuration);
            player.ApplyInitialDashStats();
        }
        
        public override void FixedUpdate()
        {
            player.HandleDash();
        }

        public override void OnExit()
        {
            //Debug.Log("OnExit DashState");
            player.ApplyEndOfDashStats();
        }
    }
}