using System;
using UnityEngine;

/// <summary>
/// Actor 基类 —— 所有需要在关卡中移动并与 Solid 碰撞的对象都继承此类。
///
/// 核心规则（来自蔚蓝 / TowerFall 原文）：
///  · 位置使用整数像素；浮点余量累积后再取整移动。
///  · 每次只移动 1 像素，逐格检测，保证永不与 Solid 重叠。
///  · 不自带速度/重力，由子类管理并传入 MoveX / MoveY。
///  · 所有碰撞计算在像素空间进行，渲染时通过 /PPU 还原为 Unity 世界坐标。
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class Actor : MonoBehaviour
{
    // ── 碰撞盒（整数像素坐标，Pivot 在左下角）──────────────────
    [Header("Collider (integer pixels)")]
    [SerializeField] private Vector2Int colliderOffset = Vector2Int.zero;
    [SerializeField] private Vector2Int colliderSize   = new Vector2Int(8, 8);

    // 整数位置（像素）
    protected Vector2Int position;

    // 浮点余量累加器（像素单位）
    private float xRemainder;
    private float yRemainder;

    // ── 属性 ────────────────────────────────────────────────────
    public Vector2Int Position => position;

    /// <summary>整数 AABB（像素坐标空间）</summary>
    public RectInt Bounds => new RectInt(
        position.x + colliderOffset.x - colliderSize.x / 2,
        position.y + colliderOffset.y,
        colliderSize.x,
        colliderSize.y
    );

    public int Left   => Bounds.xMin;
    public int Right  => Bounds.xMax;
    public int Top    => Bounds.yMax;
    public int Bottom => Bounds.yMin;

    // ── Unity 生命周期 ──────────────────────────────────────────
    protected virtual void Awake()
    {
        // Unity 世界坐标 → 像素坐标（/PPU）
        position = Vector2Int.RoundToInt(transform.position / PhysicsWorld.PPU);
        SyncTransform();

        PhysicsWorld.Instance.RegisterActor(this);
    }

    protected virtual void OnDestroy()
    {
        PhysicsWorld.Instance?.UnregisterActor(this);
    }

    // ── 核心移动 API ─────────────────────────────────────────────

    /// <summary>
    /// 水平移动（像素单位）。
    /// </summary>
    /// <param name="amount">移动量（可为浮点像素）</param>
    /// <param name="onCollide">碰到 Solid 时的回调（可为 null）</param>
    public void MoveX(float amount, Action onCollide)
    {
        xRemainder += amount;
        int move = Mathf.RoundToInt(xRemainder);
        if (move == 0) return;

        xRemainder -= move;
        int sign = (int)Mathf.Sign(move);

        while (move != 0)
        {
            // 碰撞检测在像素空间进行
            var nextBounds = new RectInt(
                Bounds.x + sign, Bounds.y,
                Bounds.width, Bounds.height
            );

            if (!PhysicsWorld.Instance.SolidOverlap(nextBounds))
            {
                position.x += sign;
                move       -= sign;
                SyncTransform();  // 像素 → 世界坐标同步
            }
            else
            {
                onCollide?.Invoke();
                break;
            }
        }
    }

    /// <summary>
    /// 垂直移动（像素单位）。
    /// </summary>
    /// <param name="amount">移动量（可为浮点像素，正值向上）</param>
    /// <param name="onCollide">碰到 Solid 时的回调（可为 null）</param>
    public void MoveY(float amount, Action onCollide)
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
                move       -= sign;
                SyncTransform();
            }
            else
            {
                onCollide?.Invoke();
                break;
            }
        }
    }

    // ── Solid 交互接口 ──────────────────────────────────────────

    /// <summary>
    /// 判断该 Actor 是否"骑在" solid 上（即该 Actor 站在 solid 顶部）。
    /// 像素空间比较。
    /// </summary>
    public virtual bool IsRiding(Solid solid)
    {
        RectInt below = new RectInt(Bounds.x, Bounds.y - 1, Bounds.width, Bounds.height);
        return solid.Bounds.Overlaps(below);
    }

    /// <summary>
    /// 被两个 Solid 夹住时调用。默认行为：销毁自身。
    /// </summary>
    public virtual void Squish()
    {
        Debug.Log($"[Actor] {name} was squished!");
        Destroy(gameObject);
    }

    // ── 工具方法 ────────────────────────────────────────────────

    /// <summary>
    /// 将整数像素坐标写回 Unity Transform。
    /// 像素坐标 * PPU = Unity 世界坐标。
    /// </summary>
    protected void SyncTransform()
    {
        transform.position = new Vector3(
            position.x * PhysicsWorld.PPU,
            position.y * PhysicsWorld.PPU,
            transform.position.z
        );
    }

    /// <summary>强制设置整数像素位置（清零余量）</summary>
    public void SetPosition(Vector2Int pos)
    {
        position   = pos;
        xRemainder = 0f;
        yRemainder = 0f;
        SyncTransform();
    }

    // ── Gizmo（编辑器可视化）─────────────────────────────────────
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // 编辑器下从 Transform 反算像素坐标
        Vector2Int pos = Application.isPlaying
            ? position
            : Vector2Int.RoundToInt(transform.position / PhysicsWorld.PPU);

        // 在像素空间计算 AABB
        RectInt b = new RectInt(
            pos.x + colliderOffset.x - colliderSize.x / 2,
            pos.y + colliderOffset.y,
            colliderSize.x,
            colliderSize.y
        );

        float ppu = PhysicsWorld.PPU;

        Gizmos.color = Color.green;
        // ✅ 像素坐标 → Unity 世界坐标（*PPU）再绘制
        Gizmos.DrawWireCube(
            new Vector3((b.x + b.width  * 0.5f) * ppu,
                        (b.y + b.height * 0.5f) * ppu,
                        0f),
            new Vector3(b.width  * ppu,
                        b.height * ppu,
                        0f)
        );
    }
#endif
}