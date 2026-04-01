using UnityEngine;

namespace Platformer
{
    [CreateAssetMenu(fileName = "PlayerMovementStats", menuName = "Platformer/Movement Stats")]
    public class PlayerMovementStats : ScriptableObject
    {
        [Header("Horizontal")]
        public float moveSpeed = 6f;
        public float runSpeed = 10f;

        [Header("Jump Base")]
        public float maxJumpHeight = 4f;
        public float jumpApexDuration = 0.1f;
        public float maxFallSpeed = -10f;

        [Header("Gravity")]
        public float gravityMultiplier = 3f;
        public float jumpEndEarlyGravityModifier = 3f;

        [Header("Assist")]
        public float jumpCoyoteDuration = 0.2f;
        public float jumpBufferDuration = 0.1f;

        [Header("Runtime (Debug Only)")]
        [SerializeField] private float initialJumpVelocity;
        [SerializeField] private float gravity;
        [SerializeField] private float jumpBeforeApexDuration;
        [SerializeField] private float jumpDuration;

        // 👉 初始化计算（核心）
        public void Calculate()
        {
            gravity = - (initialJumpVelocity * initialJumpVelocity) / (2 * maxJumpHeight);

            jumpBeforeApexDuration = - initialJumpVelocity / gravity;

            jumpDuration = jumpBeforeApexDuration + jumpApexDuration;
        }

        public float InitialJumpVelocity => initialJumpVelocity;
        public float Gravity => gravity;
        public float JumpBeforeApex => jumpBeforeApexDuration;
        public float JumpDuration => jumpDuration;
    }
}