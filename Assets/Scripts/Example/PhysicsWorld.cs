using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局物理世界单例，管理所有 Actor 和 Solid 的注册与查询。
/// 挂载到场景中任意一个 GameObject 上即可。
/// </summary>
public class PhysicsWorld : MonoBehaviour
{
    public static PhysicsWorld Instance { get; private set; }

    [Header("Pixels Per Unit")]
    [SerializeField] private int pixelsPerUnit = 16;  // 改这里即可换分辨率
    
    public static int PPU => Instance.pixelsPerUnit;
    
    private readonly List<Actor> actors = new List<Actor>();
    private readonly List<Solid> solids = new List<Solid>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ── 注册 / 注销 ──────────────────────────────────────────────
    public void RegisterActor(Actor a)   { if (!actors.Contains(a)) actors.Add(a); }
    public void UnregisterActor(Actor a) { actors.Remove(a); }
    public void RegisterSolid(Solid s)   { if (!solids.Contains(s)) solids.Add(s); }
    public void UnregisterSolid(Solid s) { solids.Remove(s); }

    // ── 查询接口 ─────────────────────────────────────────────────
    public IReadOnlyList<Actor> AllActors => actors;
    public IReadOnlyList<Solid> AllSolids => solids;

    /// <summary>
    /// 检测指定 AABB 是否与任何「可碰撞」的 Solid 重叠。
    /// </summary>
    public bool SolidOverlap(RectInt box, Solid ignore = null)
    {
        foreach (var s in solids)
        {
            if (!s.Collidable) continue;
            if (s == ignore)   continue;
            if (s.Bounds.Overlaps(box)) return true;
        }
        return false;
    }

    /// <summary>
    /// 检测指定 AABB 是否与某个 Actor 的 AABB 重叠。
    /// </summary>
    public bool ActorOverlap(RectInt box, Actor actor)
    {
        return actor.Bounds.Overlaps(box);
    }
}
