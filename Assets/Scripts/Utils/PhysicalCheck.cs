using UnityEngine;

namespace Platformer
{
    public class PhysicCheck : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BoxCollider2D col;

        [Header("Collision Settings")]
        public LayerMask groundLayer;
        [SerializeField] private float groundCheckDistance = 0.05f;
        [SerializeField] private float ceilCheckDistance   = 0.05f;
        [SerializeField] private float wallCheckDistance   = 0.05f;

        public bool IsGrounded  { get; private set; }
        public bool IsCeil      { get; private set; }
        /// <summary>左侧紧贴墙壁</summary>
        public bool IsWallLeft  { get; private set; }
        /// <summary>右侧紧贴墙壁</summary>
        public bool IsWallRight { get; private set; }
        /// <summary>任意一侧紧贴墙壁</summary>
        public bool IsWall => IsWallLeft || IsWallRight;
        /// <summary>紧贴的墙壁方向（-1 左，+1 右，0 无）</summary>
        public int WallDirection => IsWallRight ? 1 : IsWallLeft ? -1 : 0;

        public event System.Action OnLanded;
        public event System.Action OnLeftGround;
        public event System.Action OnHitCeiling;

        private bool _queriesCache;

        private void Awake()
        {
            col = GetComponent<BoxCollider2D>();
            _queriesCache = Physics2D.queriesStartInColliders;
        }

        // ── 手动计算碰撞盒在世界空间的中心和尺寸 ──────────────────
        public Vector2 ColCenter => (Vector2)transform.position + col.offset;
        public Vector2 ColSize   => col.size;
        public Vector2 ColOffset => col.offset;

        /// <summary>
        /// 由 PlayerController.FixedUpdate 调用，保持与物理步骤同步。
        /// 必须在 PlayerController 的 CommitPosition() 之前调用，
        /// 这样 transform.position 已经是本帧最新位置。
        /// （FixedUpdate 的顺序在 Step 0 同步 _position 后、ApplyMovement 前调用，
        ///  所以 CheckCollisions 读到的是上一帧末的位置，行为与有 Rigidbody 时一致）
        /// </summary>
        public void CheckCollisions()
        {
            Physics2D.queriesStartInColliders = false;

            bool groundHit = Physics2D.BoxCast(
                ColCenter, ColSize, 0f,
                Vector2.down, groundCheckDistance, groundLayer);

            bool ceilHit = Physics2D.BoxCast(
                ColCenter, ColSize, 0f,
                Vector2.up, ceilCheckDistance, groundLayer);

            bool wallLeftHit = Physics2D.BoxCast(
                ColCenter, ColSize, 0f,
                Vector2.left, wallCheckDistance, groundLayer);

            bool wallRightHit = Physics2D.BoxCast(
                ColCenter, ColSize, 0f,
                Vector2.right, wallCheckDistance, groundLayer);

            IsWallLeft  = wallLeftHit;
            IsWallRight = wallRightHit;

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

            Physics2D.queriesStartInColliders = _queriesCache;
        }
    }
}