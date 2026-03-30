using UnityEngine;

namespace Platformer
{
    public class PhysicalCheck : MonoBehaviour
    {
        private BoxCollider2D col;
        
        [Header("Ground Check Settings")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float groundCheckDistance = 0.1f;
        [Range(0f, 0.5f)] [SerializeField] private float groundWidthShrink = 0.1f; // 宽度缩减量
        
        [Header("Wall Check Settings")]
        [SerializeField] private LayerMask wallLayer;
        [SerializeField] private float wallCheckDistance = 0.1f;
        [Range(0f, 0.5f)] [SerializeField] private float wallHeightShrink = 0.1f; // 高度缩减量

        [Header("State")]
        public bool isGround;
        public bool isWall;
        public bool isHang;

        private void Awake()
        {
            col = GetComponent<BoxCollider2D>();
        }

        private void Update()
        {
            CheckGround();
            CheckWall();
            CheckHang();
        }

        private void CheckGround()
        {
            // 尺寸设计：宽度 = 碰撞体宽度 - 左右缩减；高度 = 极小值
            // 缩减宽度是为了防止角色贴墙时，地面检测盒碰到了墙壁导致误判为在地上
            Vector2 groundCheckSize = new Vector2(col.size.x - groundWidthShrink, 0.01f);
            
            // 起始点：碰撞体底部中心
            Vector2 checkOrigin = (Vector2)col.bounds.center + Vector2.down * (col.bounds.extents.y);

            isGround = Physics2D.BoxCast(
                checkOrigin,
                groundCheckSize,
                0,
                Vector2.down,
                groundCheckDistance,
                groundLayer);
        }

        private void CheckWall()
        {
            float faceDir = Mathf.Sign(transform.localScale.x);
            
            // 尺寸设计：宽度 = 极小值；高度 = 碰撞体高度 - 上下缩减
            // 缩减高度是为了防止检测盒碰到天花板或地板边缘
            Vector2 wallCheckSize = new Vector2(0.01f, col.size.y - wallHeightShrink);
            
            // 起始点：碰撞体面朝方向的侧边中心
            Vector2 checkOrigin = (Vector2)col.bounds.center + Vector2.right * (faceDir * col.bounds.extents.x);

            isWall = Physics2D.BoxCast(
                checkOrigin,
                wallCheckSize,
                0,
                Vector2.right * faceDir,
                wallCheckDistance,
                wallLayer);
            
        }

        private void CheckHang()
        {
            float faceDir = Mathf.Sign(transform.localScale.x);
            Vector2 checkOrigin = (Vector2)transform.position + Vector2.right * (faceDir * col.bounds.extents.x);
            
            
            isHang = !Physics2D.Raycast(
                checkOrigin,
                Vector2.right * faceDir,
                wallCheckDistance,
                wallLayer);
        }

        // 补全 Gizmos，这对于调试 BoxCast 至关重要
        private void OnDrawGizmosSelected()
        {
            if (col == null) col = GetComponent<BoxCollider2D>();

            // 绘制地面检测
            Gizmos.color = isGround ? Color.green : Color.red;
            Vector2 gSize = new Vector2(col.size.x - groundWidthShrink, 0.01f);
            Vector2 gOrigin = (Vector2)col.bounds.center + Vector2.down * (col.bounds.extents.y + groundCheckDistance);
            Gizmos.DrawWireCube(gOrigin, gSize);

            // 绘制墙体检测
            float faceDir = Mathf.Sign(transform.localScale.x);
            Gizmos.color = isWall ? Color.green : Color.blue;
            Vector2 wSize = new Vector2(0.01f, col.size.y - wallHeightShrink);
            Vector2 wOrigin = (Vector2)col.bounds.center + Vector2.right * (faceDir * (col.bounds.extents.x + wallCheckDistance));
            Gizmos.DrawWireCube(wOrigin, wSize);
            
            // 绘制爬墙状态检测
            Gizmos.color = isWall ? Color.green : Color.blue;
            Gizmos.DrawRay((Vector2)transform.position + Vector2.right * (faceDir * col.bounds.extents.x) , Vector2.right * faceDir * wallCheckDistance);
        }
    }
}
