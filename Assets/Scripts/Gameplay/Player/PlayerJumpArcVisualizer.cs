using UnityEngine;

namespace Platformer
{
    /// <summary>
    /// 在 Scene 视图中绘制玩家的跳跃弧线预览。
    /// 将此组件添加到 Player GameObject，并将同一个
    /// PlayerMovementStats ScriptableObject 拖入即可。
    ///
    /// 弧线颜色含义：
    ///   绿色 → 上升段
    ///   黄色 → 顶点悬停段
    ///   红色 → 下降段（完整跳跃）
    ///   洋红 → 下降段（提前松键短跳）
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PlayerJumpArcVisualizer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerMovementStats moveStats;

        [Header("Visualization")]
        [Tooltip("同时绘制提前松键的短跳弧线")]
        [SerializeField] private bool showEarlyRelease = true;

        [Tooltip("短跳弧线颜色")]
        [SerializeField] private Color earlyReleaseColor = new Color(1f, 0.3f, 1f, 0.7f);

        private Collider2D _col;

        private void Awake() => _col = GetComponent<Collider2D>();

        private void OnDrawGizmos()
        {
            if (moveStats == null) return;

            moveStats.Calculate();

            _col ??= GetComponent<Collider2D>();
            if (_col == null) return;

            // 完整跳跃弧线（持键到顶点）
            DrawJumpArc(
                moveStats.runSpeed,
                earlyRelease:    false,
                moveStats.arcAscendColor,
                moveStats.arcApexColor,
                moveStats.arcDescendColor);

            // 短跳弧线（立即松键）
            if (showEarlyRelease)
            {
                DrawJumpArc(
                    moveStats.runSpeed,
                    earlyRelease:    true,
                    moveStats.arcAscendColor,
                    moveStats.arcApexColor,
                    earlyReleaseColor);
            }
        }

        // ─────────────────────────────────────────────────────────
        //  核心弧线绘制
        //
        //  与 PlayerController.HandleJump 的三阶段完全对应：
        //
        //  阶段一（jumpTimer 运行中）：
        //    上升段  → v0 + g·t     （g = moveStats.Gravity，负值）
        //    顶点段  → Y 固定，X 继续移动
        //
        //  阶段二（jumpTimer 停止）：
        //    完整跳跃 → g × gravityMultiplier
        //    提前松键 → g × jumpEndEarlyGravityModifier
        //
        //  注意：早期版本把 jumpEndEarlyGravityModifier 误用于"还在上升时"，
        //  导致实际高度低于弧线。现已修正为下落段才应用倍率。
        // ─────────────────────────────────────────────────────────
        private void DrawJumpArc(
            float moveSpeed,
            bool  earlyRelease,
            Color ascendColor,
            Color apexColor,
            Color descendColor)
        {
            // ── 起点与初速度 ──────────────────────────────────────
            Vector2 startPos = new Vector2(
                _col.bounds.center.x,
                _col.bounds.min.y);

            float   hSpeed   = moveStats.drawRight ? moveSpeed : -moveSpeed;
            Vector2 velocity = new Vector2(hSpeed, moveStats.InitialJumpVelocity);

            // 上升阶段的基础重力（负值）
            float g       = moveStats.Gravity;
            float tApex   = moveStats.timeTillJumpApex;
            float tSuspend = moveStats.jumpApexDuration;

            // 提前松键时不经历完整上升，t_apex_actual 取 0（立即松键最极端情况）
            // 这里选择绘制"立即松键"的极限短跳作为参考线
            float tAscend = earlyRelease ? 0f : tApex;

            // 到顶点时的 Y 位移（仅上升段的抛体公式）
            float yAtApex = velocity.y * tAscend + 0.5f * g * tAscend * tAscend;

            // 时间步长：将总可视化步数映射到合理时间范围
            float timeStep  = tApex / moveStats.arcResolution;
            Vector2 prevPos = startPos;

            for (int i = 0; i < moveStats.visualizationSteps; i++)
            {
                float t = i * timeStep;
                Vector2 disp;

                if (!earlyRelease && t < tAscend)
                {
                    // ── 上升段 ─────────────────────────────────────
                    Gizmos.color = ascendColor;
                    disp = new Vector2(hSpeed * t,
                                       velocity.y * t + 0.5f * g * t * t);
                }
                else if (!earlyRelease
                         && t < tAscend + tSuspend)
                {
                    // ── 顶点悬停段 ─────────────────────────────────
                    Gizmos.color = apexColor;
                    float tSusp = t - tAscend;
                    disp = new Vector2(hSpeed * (tAscend + tSusp),
                                       yAtApex);   // Y 固定在顶点高度
                }
                else
                {
                    // ── 下落段 ─────────────────────────────────────
                    Gizmos.color = descendColor;

                    // 下落从顶点悬停结束后开始，初速度 = 0
                    float tDesc = earlyRelease
                        ? t                                   // 提前松键：从 t=0 就下落
                        : t - (tAscend + tSuspend);

                    // 选用对应的重力倍率（与 HandleJump 保持一致）
                    float gMultiplier = earlyRelease
                        ? moveStats.jumpEndEarlyGravityModifier
                        : moveStats.gravityMultiplier;

                    float xDesc = hSpeed * (earlyRelease ? 0f : tAscend + tSuspend)
                                + hSpeed * tDesc;

                    // 下落 Y = 顶点 Y + 0.5 * g * multiplier * t_desc²
                    // （从顶点出发，初速度 = 0）
                    float yDesc = yAtApex
                                + 0.5f * g * gMultiplier * tDesc * tDesc;

                    disp = new Vector2(xDesc, yDesc);
                }

                Vector2 drawPoint = startPos + disp;

                // ── 碰撞截断 ───────────────────────────────────────
                if (moveStats.stopOnCollision)
                {
                    Vector2 dir  = drawPoint - prevPos;
                    float   dist = dir.magnitude;

                    if (dist > 0f)
                    {
                        RaycastHit2D hit = Physics2D.Raycast(
                            prevPos, dir.normalized, dist, moveStats.groundLayer);

                        if (hit.collider != null)
                        {
                            Gizmos.DrawLine(prevPos, hit.point);
                            Gizmos.color = Color.white;
                            Gizmos.DrawSphere(hit.point, 0.05f);
                            return;
                        }
                    }
                }

                Gizmos.DrawLine(prevPos, drawPoint);
                prevPos = drawPoint;

                // 落回起点高度以下时停止
                if (drawPoint.y < startPos.y - 0.1f
                    && t > (earlyRelease ? 0f : tAscend))
                    break;
            }
        }
    }
}