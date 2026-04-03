using UnityEngine;

namespace PhysicsCore
{
    public class PhysicsWorld:MonoBehaviour
    {
        /*
         * 管理所有 Actor / Solid 列表
         * 提供 Overlap(RectInt) 查询接口（核心）
         * 提供 BoxCast / CheckCollision
         * 提供 Register / Remove（生命周期管理）
         * 可扩展：空间分区（优化性能）
         */
        public static PhysicsWorld Instance {get; private set;}

        [SerializeField] private int pixelsPerUnit = 16;
        
        public static int PPU => Instance.pixelsPerUnit;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
    }
}