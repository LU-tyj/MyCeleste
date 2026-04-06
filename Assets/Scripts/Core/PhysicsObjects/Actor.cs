using System;
using UnityEngine;

namespace Core
{
    public class Actor : MonoBehaviour
    {
        /*
         * 可移动对象（玩家/敌人）
         * - 以 MiddleBottom 为 position 定义，以像素单位为基础单位，后使用PPU进行坐标转换
         * 在unity周期开始注册到PhysicsWorld
           - 持有 RectInt collider
           - 持有 Position（int）
           - 维护 xRemainder / yRemainder（子像素）
           - 核心函数：
            → MoveX(float amount, Action onCollide)
                - 余数累计
                - Round → 整数移动
                - 逐像素移动（while）
                - 每步用 PhysicsWorld.Overlap 检测
            → MoveY(...)
            → IsRiding(Solid)
                - 默认：是否站在顶部
         */
        
        // 整数位置
        protected Vector2Int position;
        
        [Header("Collider Settings")]
        [SerializeField] private Vector2Int colliderOffset = new Vector2Int();
        [SerializeField] private Vector2Int colliderSize = new Vector2Int();
        [SerializeField] private bool isTrigger = false;
        
        public Collision col;
        
        private float xRemainder;
        private float yRemainder;
        
        public RectInt Bounds => col.GetBounds(position);
        
        private void Awake()
        {
            position = Vector2Int.RoundToInt(transform.position);
            SyncTransform();
            col = new Collision(colliderOffset, colliderSize, isTrigger);
            PhysicsWorld.Instance?.RegisterActor(this);
        }

        private void OnDestroy()
        {
            PhysicsWorld.Instance?.UnregisterActor(this);
        }
        
        // 移动
        protected void MoveX(float amount, Action onCollide = null)
        {
            xRemainder += amount;
            int move = Mathf.RoundToInt(xRemainder);
            if (move == 0) return;
            
            xRemainder -= move;
            int sign = (int)Mathf.Sign(move);

            while (move != 0)
            {
                var nextBounds = new RectInt(
                    Bounds.x + sign, Bounds.y, 
                    Bounds.width, Bounds.height
                    );
                if (!PhysicsWorld.Instance.SolidOverlap(nextBounds))
                {
                    position.x += sign;
                    move -= sign;
                    SyncTransform();
                }
                else
                {
                    onCollide?.Invoke();
                    break;
                }
            }
        }

        protected void MoveY(float amount, Action onCollide = null)
        {
            yRemainder += amount;
            int move = Mathf.RoundToInt(yRemainder);
            if (move == 0) return;
            
            yRemainder -= move;
            int sign = (int)Mathf.Sign(move);
            
            while (move != 0)
            {
                var nextBounds = new RectInt(
                    Bounds.x, Bounds.y + sign,
                    Bounds.width, Bounds.height
                );
                if (!PhysicsWorld.Instance.SolidOverlap(nextBounds))
                { 
                    position.y += sign;
                    move -= sign;
                    SyncTransform();
                }
                else
                {
                    onCollide?.Invoke();
                    break;
                }
            }
        }


        // 将像素坐标 position 转换回 Unity 世界坐标
        private void SyncTransform()
        {
            float worldX = position.x / (float)PhysicsWorld.PPU;
            float worldY = position.y / (float)PhysicsWorld.PPU;
            transform.position = new Vector3(worldX, worldY, transform.position.z);
        }

        public void SetPosition(Vector2Int pos)
        {
            position = pos;
            xRemainder = 0;
            yRemainder = 0;
            SyncTransform();
        }
        
#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            // col 在 Awake 初始化，编辑器未运行时为 null，直接用 SerializeField 数据临时计算
            Vector2Int previewSize = colliderSize;
            Vector2Int previewPos = Vector2Int.RoundToInt(transform.position * PhysicsWorld.PPU);

            float cx = (previewPos.x + previewSize.x * 0.5f) / PhysicsWorld.PPU;
            float cy = (previewPos.y + previewSize.y * 0.5f) / PhysicsWorld.PPU;

            Vector3 center = new Vector3(cx, cy, 0f);
            Vector3 size = new Vector3(
                previewSize.x / (float)PhysicsWorld.PPU,
                previewSize.y / (float)PhysicsWorld.PPU,
                0f
            );

            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(center, size);
        }
#endif
    }
}