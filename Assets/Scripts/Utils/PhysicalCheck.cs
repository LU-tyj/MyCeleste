using UnityEngine;

namespace Platformer
{
    public class PhysicCheck : MonoBehaviour
    {
        [Header("References")]
        public BoxCollider2D col;

        [Header("Collision Settings")]
        public LayerMask groundLayer;
        [SerializeField] private float groundCheckDistance = 0.05f;
        [SerializeField] private float ceilCheckDistance  = 0.05f;

        public bool IsGrounded { get; private set; }
        public bool IsCeil     { get; private set; }

        // 触发事件，让 PlayerController 订阅
        public event System.Action OnLanded;
        public event System.Action OnLeftGround;
        public event System.Action OnHitCeiling;

        private bool colliderCache;

        private void Awake()
        {
            col = GetComponent<BoxCollider2D>();
            colliderCache = Physics2D.queriesStartInColliders;
        }

        /// <summary>
        /// 由 PlayerController.FixedUpdate 调用，保持与物理步骤同步。
        /// </summary>
        public void CheckCollisions()
        {
            Physics2D.queriesStartInColliders = false;

            bool groundHit = Physics2D.BoxCast(
                col.bounds.center, col.bounds.size, 0f,
                Vector2.down, groundCheckDistance, groundLayer);

            bool ceilHit = Physics2D.BoxCast(
                col.bounds.center, col.bounds.size, 0f,
                Vector2.up, ceilCheckDistance, groundLayer);

            // ── 天花板 ──────────────────────────────────────────
            if (!IsCeil && ceilHit)
            {
                IsCeil = true;
                OnHitCeiling?.Invoke();
            }
            else if (IsCeil && !ceilHit)
            {
                IsCeil = false;
            }

            // ── 地面 ────────────────────────────────────────────
            if (!IsGrounded && groundHit)
            {
                IsGrounded = true;
                OnLanded?.Invoke();
            }
            else if (IsGrounded && !groundHit)
            {
                IsGrounded = false;
                OnLeftGround?.Invoke();
            }

            Physics2D.queriesStartInColliders = colliderCache;
        }
    }
}