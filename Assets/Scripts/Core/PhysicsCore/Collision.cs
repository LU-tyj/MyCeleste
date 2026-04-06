using UnityEngine;

namespace Core
{
    public class Collision
    {
        /*
         * 纯碰撞数据组件（无 position）
         * 使用 RectInt.Overlaps
         */

        // === 配置数据 ===
        private Vector2Int offset;   // 相对 position（MiddleBottom）
        private Vector2Int size;

        private bool isTrigger;

        public bool IsTrigger => isTrigger;

        public Collision(Vector2Int offset, Vector2Int size, bool isTrigger = false)
        {
            this.offset = offset;
            this.size = size;
            this.isTrigger = isTrigger;
        }

        /// <summary>
        /// 根据“外部传入的 position（MiddleBottom）”计算 Bounds
        /// </summary>
        public RectInt GetBounds(Vector2Int position)
        {
            int halfSize = size.x / 2;
            return new RectInt(
                position.x - halfSize + offset.x,
                position.y + offset.y,
                size.x,
                size.y
            );
        }

        /// <summary>
        /// Overlap 检测（由 Actor 提供 position）
        /// </summary>
        public bool Overlap(Vector2Int selfPos, RectInt other)
        {
            return GetBounds(selfPos).Overlaps(other);
        }
    }
}