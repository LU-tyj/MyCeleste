using UnityEngine;

/// <summary>
/// 移动平台示例 —— 继承 Solid，在两个路径点之间来回移动。
/// 演示 Solid.Move 的携带与推动行为。
/// </summary>
public class MovingPlatform : Solid
{
    [Header("Waypoints (local offset from start)")]
    [SerializeField] private Vector2 pointA = Vector2.zero;
    [SerializeField] private Vector2 pointB = new Vector2(80f, 0f);

    [SerializeField] private float speed = 60f; // px/s

    private Vector2 startPosition;
    private Vector2 target;
    private int     direction = 1; // 1 = A→B, -1 = B→A

    protected override void Awake()
    {
        base.Awake();
        startPosition = new Vector2(position.x, position.y);
        target        = startPosition + pointB;
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // 当前位置（浮点，用于平滑插值计算）
        Vector2 currentPos = new Vector2(position.x, position.y);
        Vector2 delta       = target - currentPos;
        float   dist        = delta.magnitude;
        float   step        = speed * dt;

        if (step >= dist)
        {
            // 到达目标，切换方向
            float overshoot = step - dist;
            direction = -direction;
            target    = direction == 1
                ? startPosition + pointB
                : startPosition + pointA;

            // 先移动到终点，再补偿超出量
            Move(delta.x, delta.y);

            Vector2 newDelta = (target - new Vector2(position.x, position.y)).normalized;
            Move(newDelta.x * overshoot, newDelta.y * overshoot);
        }
        else
        {
            Vector2 moveDir = delta.normalized;
            Move(moveDir.x * step, moveDir.y * step);
        }
    }

    // ── Gizmo：绘制路径 ──────────────────────────────────────────
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Vector2 origin = Application.isPlaying
            ? new Vector2(position.x, position.y) - (Vector2)pointA  // 还原起点
            : (Vector2)transform.position;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin + pointA, origin + pointB);
        Gizmos.DrawSphere(origin + pointA, 2f);
        Gizmos.DrawSphere(origin + pointB, 2f);
    }
#endif
}
