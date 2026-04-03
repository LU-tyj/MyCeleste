using UnityEngine;

namespace Platformer
{
    public class IdleState : BaseState
    {
        public IdleState(PlayerController player, Animator animator) : base(player, animator)
        {
        }

        public override void OnEnter()
        {
            // animator.CrossFade(idleHash, crossFadeDuration);;
        }

        public override void FixedUpdate()
        {
        }
    }
}