using System.Collections.Generic;
using UnityEngine;

namespace Core
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
        
        private readonly List<Actor> actors = new List<Actor>();
        private readonly List<Solid> solids = new List<Solid>();

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
        
        public void RegisterActor(Actor actor) {if (!actors.Contains(actor)) { actors.Add(actor); }}
        public void UnregisterActor(Actor actor)  {if (actors.Contains(actor)) { actors.Remove(actor); }}
        public void RegisterSolid(Solid solid) {if (!solids.Contains(solid)) { solids.Add(solid); }}
        public void UnregisterSolid(Solid solid) {if (solids.Contains(solid)) { solids.Remove(solid);} }
        
        public IReadOnlyList<Actor> AllActors => actors;
        public IReadOnlyList<Solid> AllSolids => solids;
        
        public bool SolidOverlap(RectInt box, Solid ignore = null)
        {
            return false;
        }

        public bool ActorOverlap(RectInt box, Actor ignore = null)
        {
            return false;
        }
        
    }
}