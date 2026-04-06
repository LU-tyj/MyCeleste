using UnityEngine;

namespace Core
{
    public class Solid : MonoBehaviour
    {
        /*
         * 固体对象（墙壁/地板/天花板）
         * - 持有 RectInt collider
         * - 注册到 PhysicsWorld
         * - 可扩展：移动 Solid（平台）/ 触发器（无碰撞，仅事件）
         */

        // 整数位置
        protected Vector2Int position;

        [Header("Collider Settings")] [SerializeField]
        private Vector2Int colliderOffset = new Vector2Int();

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
            PhysicsWorld.Instance?.RegisterSolid(this);
        }

        public void Move(float x, float y)
        {
            
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

    }
}