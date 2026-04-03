using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Solid 基类 —— 可碰撞的关卡几何体，支持移动（移动平台、压缩机等）。
///
/// 核心规则（来自蔚蓝 / TowerFall 原文）：
///  · Solid 移动时保证到达目标位置，无论途中是否有 Actor 阻挡。
///  · 对路上的 Actor 进行「推动」或「携带」处理。
///  · 推动优先级高于携带。
///  · 临时将自身设为不可碰撞，避免被自己推动的 Actor 卡住。
/// </summary>
public class Solid : MonoBehaviour
{
    // ── 碰撞盒 ──────────────────────────────────────────────────
    [Header("Collider (integer pixels)")]
    [SerializeField] private Vector2Int colliderOffset = Vector2Int.zero;
    [SerializeField] private Vector2Int colliderSize   = new Vector2Int(16, 8);

    // 整数位置
    protected Vector2Int position;

    // 浮点余量
    private float xRemainder;
    private float yRemainder;

    // 是否参与碰撞检测（Move 期间暂时关闭）
    public bool Collidable { get; set; } = true;

    // ── 属性 ────────────────────────────────────────────────────
    public RectInt Bounds => new RectInt(
        position.x + colliderOffset.x,
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
        position = Vector2Int.RoundToInt(transform.position);
        SyncTransform();
        PhysicsWorld.Instance.RegisterSolid(this);
    }

    protected virtual void OnDestroy()
    {
        PhysicsWorld.Instance?.UnregisterSolid(this);
    }

    // ── 核心移动 API ─────────────────────────────────────────────

    /// <summary>
    /// 移动 Solid，同时处理对 Actor 的推动与携带。
    /// </summary>
    public void Move(float x, float y)
    {
        xRemainder += x;
        yRemainder += y;

        int moveX = Mathf.RoundToInt(xRemainder);
        int moveY = Mathf.RoundToInt(yRemainder);

        if (moveX == 0 && moveY == 0) return;

        // 在移动前收集所有"骑在"此 Solid 上的 Actor（携带列表）
        List<Actor> riding = GetAllRidingActors();

        // 临时禁用碰撞，避免被推动/携带的 Actor 卡在自身上
        Collidable = false;

        // ── X 轴移动 ──
        if (moveX != 0)
        {
            xRemainder -= moveX;
            position.x += moveX;
            SyncTransform();

            if (moveX > 0)
            {
                // 向右移动
                foreach (var actor in PhysicsWorld.Instance.AllActors)
                {
                    if (Bounds.Overlaps(actor.Bounds))
                    {
                        // 推动：推到与 Solid 左边缘齐平
                        actor.MoveX(Right - actor.Left, actor.Squish);
                    }
                    else if (riding.Contains(actor))
                    {
                        // 携带：给完整速度，无碰撞回调
                        actor.MoveX(moveX, null);
                    }
                }
            }
            else
            {
                // 向左移动
                foreach (var actor in PhysicsWorld.Instance.AllActors)
                {
                    if (Bounds.Overlaps(actor.Bounds))
                    {
                        actor.MoveX(Left - actor.Right, actor.Squish);
                    }
                    else if (riding.Contains(actor))
                    {
                        actor.MoveX(moveX, null);
                    }
                }
            }
        }

        // ── Y 轴移动 ──
        if (moveY != 0)
        {
            yRemainder -= moveY;
            position.y += moveY;
            SyncTransform();

            if (moveY > 0)
            {
                // 向上移动
                foreach (var actor in PhysicsWorld.Instance.AllActors)
                {
                    if (Bounds.Overlaps(actor.Bounds))
                    {
                        actor.MoveY(Top - actor.Bottom, actor.Squish);
                    }
                    else if (riding.Contains(actor))
                    {
                        actor.MoveY(moveY, null);
                    }
                }
            }
            else
            {
                // 向下移动
                foreach (var actor in PhysicsWorld.Instance.AllActors)
                {
                    if (Bounds.Overlaps(actor.Bounds))
                    {
                        actor.MoveY(Bottom - actor.Top, actor.Squish);
                    }
                    else if (riding.Contains(actor))
                    {
                        actor.MoveY(moveY, null);
                    }
                }
            }
        }

        // 恢复碰撞
        Collidable = true;
    }

    // ── 工具方法 ────────────────────────────────────────────────

    private List<Actor> GetAllRidingActors()
    {
        var list = new List<Actor>();
        foreach (var actor in PhysicsWorld.Instance.AllActors)
        {
            if (actor.IsRiding(this))
                list.Add(actor);
        }
        return list;
    }

    protected void SyncTransform()
    {
        transform.position = new Vector3(position.x, position.y, transform.position.z);
    }

    public void SetPosition(Vector2Int pos)
    {
        position   = pos;
        xRemainder = 0f;
        yRemainder = 0f;
        SyncTransform();
    }

    // ── Gizmo ────────────────────────────────────────────────────
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Vector2Int pos = Application.isPlaying
            ? position
            : Vector2Int.RoundToInt(transform.position);

        RectInt b = new RectInt(
            pos.x + colliderOffset.x,
            pos.y + colliderOffset.y,
            colliderSize.x,
            colliderSize.y
        );

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(
            new Vector3(b.x + b.width  * 0.5f, b.y + b.height * 0.5f, 0),
            new Vector3(b.width, b.height, 0)
        );
    }
#endif
}
