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
    ///   红色 → 下降段
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PlayerJumpArcVisualizer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerMovementStats moveStats;

        // 缓存脚部碰撞体（用于确定起点）
        private Collider2D _feetColl;

        private void Awake()
        {
            _feetColl = GetComponent<Collider2D>();
        }

        // ── Gizmos 入口 ───────────────────────────────
        private void OnDrawGizmos()
        {
            if (moveStats == null) return;

            // 确保数值最新（编辑器热重载后可能未调用 Calculate）
            moveStats.Calculate();

            _feetColl ??= GetComponent<Collider2D>();
            if (_feetColl == null) return;

            DrawJumpArc(moveStats.runSpeed, moveStats.arcAscendColor,
                                                       moveStats.arcApexColor,
                                                       moveStats.arcDescendColor);
        }

        // ── 核心弧线绘制 ─────────────────────────────
        /// <summary>
        /// 模拟三段式跳跃（上升 / 悬空 / 下落）并用 Gizmos 逐段绘线。
        /// </summary>
        /// <param name="moveSpeed">水平速度（绝对值）</param>
        /// <param name="ascendColor">上升段颜色</param>
        /// <param name="apexColor">悬停段颜色</param>
        /// <param name="descendColor">下降段颜色</param>
        private void DrawJumpArc(float moveSpeed,
                                  Color ascendColor,
                                  Color apexColor,
                                  Color descendColor)
        {
            // ── 1. 起点与初速度 ───────────────────────
            Vector2 startPosition = new Vector2(
                _feetColl.bounds.center.x,
                _feetColl.bounds.min.y);

            Vector2 previousPosition = startPosition;

            float horizontalSpeed = moveStats.drawRight ? moveSpeed : -moveSpeed;
            Vector2 velocity = new Vector2(horizontalSpeed, moveStats.InitialJumpVelocity);

            // ── 2. 时间步长 ───────────────────────────
            // timeStep：将「上升到顶点」等分为 arcResolution 份
            float timeStep = moveStats.timeTillJumpApex / moveStats.arcResolution;

            // ── 3. 逐步模拟 ───────────────────────────
            for (int i = 0; i < moveStats.visualizationSteps; i++)
            {
                float simulationTime = i * timeStep;
                Vector2 displacement;

                if (simulationTime < moveStats.timeTillJumpApex)
                {
                    // ── 上升段（匀加速，重力向下） ──────
                    Gizmos.color = ascendColor;
                    displacement = velocity * simulationTime
                                 + 0.5f * new Vector2(0f, moveStats.Gravity)
                                        * simulationTime * simulationTime;
                }
                else if (simulationTime < moveStats.timeTillJumpApex + moveStats.jumpApexDuration)
                {
                    // ── 顶点悬停段（Y 不变，X 继续移动） ──
                    Gizmos.color = apexColor;
                    float apexTime = simulationTime - moveStats.timeTillJumpApex;

                    // 先算到顶点时的位移
                    displacement = velocity * moveStats.timeTillJumpApex
                                 + 0.5f * new Vector2(0f, moveStats.Gravity)
                                        * moveStats.timeTillJumpApex * moveStats.timeTillJumpApex;
                    // 悬空期间只有水平移动
                    displacement += new Vector2(horizontalSpeed, 0f) * apexTime;
                }
                else
                {
                    // ── 下降段 ────────────────────────
                    Gizmos.color = descendColor;
                    float descendTime = simulationTime
                                      - (moveStats.timeTillJumpApex + moveStats.jumpApexDuration);

                    // 到顶点时的位移
                    displacement = velocity * moveStats.timeTillJumpApex
                                 + 0.5f * new Vector2(0f, moveStats.Gravity)
                                        * moveStats.timeTillJumpApex * moveStats.timeTillJumpApex;
                    // 悬空期间的水平位移
                    displacement += new Vector2(horizontalSpeed, 0f) * moveStats.jumpApexDuration;
                    // 下落：从顶点速度 = 0 开始重新施加重力
                    displacement += new Vector2(horizontalSpeed, 0f) * descendTime
                                 + 0.5f * new Vector2(0f, moveStats.Gravity * moveStats.gravityMultiplier)
                                        * descendTime * descendTime;
                }

                Vector2 drawPoint = startPosition + displacement;

                // ── 4. 碰撞截断 ──────────────────────
                if (moveStats.stopOnCollision)
                {
                    Vector2 direction = drawPoint - previousPosition;
                    float   distance  = direction.magnitude;

                    if (distance > 0f)
                    {
                        RaycastHit2D hit = Physics2D.Raycast(
                            previousPosition, direction.normalized,
                            distance, moveStats.groundLayer);

                        if (hit.collider != null)
                        {
                            Gizmos.DrawLine(previousPosition, hit.point);
                            // 在碰撞点画一个小球作为终止标记
                            Gizmos.color = Color.white;
                            Gizmos.DrawSphere(hit.point, 0.05f);
                            return; // 碰到地面，停止绘制
                        }
                    }
                }

                // ── 5. 绘线并推进 ────────────────────
                Gizmos.DrawLine(previousPosition, drawPoint);
                previousPosition = drawPoint;

                // 如果已落回地面高度（比起点还低），可提前停止
                if (drawPoint.y < startPosition.y - 0.1f
                    && simulationTime > moveStats.timeTillJumpApex)
                    break;
            }
        }
    }
}