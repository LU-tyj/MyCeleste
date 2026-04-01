using UnityEngine;

namespace Platformer
{
    [CreateAssetMenu(fileName = "PlayerMovementStats", menuName = "Platformer/Movement Stats")]
    public class PlayerMovementStats : ScriptableObject
    {
        // ─────────────────────────────────────────────
        //  Horizontal
        // ─────────────────────────────────────────────
        [Header("Horizontal")]
        [Range(1f, 20f)]  public float moveSpeed = 6f;
        [Range(1f, 30f)]  public float runSpeed  = 10f;

        // ─────────────────────────────────────────────
        //  Jump Design Parameters（设计参数，驱动所有运行时值）
        // ─────────────────────────────────────────────
        [Header("Jump — Design Parameters")]
        [Tooltip("最大跳跃高度（单位：世界坐标）")]
        [Range(0.5f, 15f)]  public float maxJumpHeight = 4f;

        [Tooltip("从起跳到顶点所需时间（秒）")]
        [Range(0.1f, 2f)]   public float timeTillJumpApex = 0.4f;

        [Tooltip("顶点悬空持续时间（秒），0 = 关闭悬空）")]
        [Range(0f, 0.5f)]   public float jumpApexDuration = 0.1f;

        [Tooltip("最大下落速度（负值）")]
        [Range(-30f, -1f)]  public float maxFallSpeed = -20f;

        // ─────────────────────────────────────────────
        //  Gravity Modifiers
        // ─────────────────────────────────────────────
        [Header("Gravity Modifiers")]
        [Tooltip("下落阶段重力倍率（> 1 更「重」）")]
        [Range(1f, 10f)]    public float gravityMultiplier = 3f;

        [Tooltip("提前松开跳跃键时的重力倍率")]
        [Range(1f, 10f)]    public float jumpEndEarlyGravityModifier = 3f;

        // ─────────────────────────────────────────────
        //  Jump Assist
        // ─────────────────────────────────────────────
        [Header("Jump Assist")]
        [Tooltip("离地后仍可起跳的宽容时间（土狼时间）")]
        [Range(0f, 0.5f)]   public float jumpCoyoteDuration = 0.2f;

        [Tooltip("落地前预按跳跃仍有效的时间（跳跃缓冲）")]
        [Range(0f, 0.5f)]   public float jumpBufferDuration = 0.1f;

        // ─────────────────────────────────────────────
        //  Arc Visualization（编辑器弧线可视化设置）
        // ─────────────────────────────────────────────
        [Tooltip("弧线分辨率：每段时间步内的采样点数，越大越精细")]
        [Range(5, 100)]     public int   arcResolution = 30;

        [Tooltip("可视化总步数（与 arcResolution 共同决定精细程度）")]
        [Range(10, 200)]    public int   visualizationSteps = 60;

        [Tooltip("预览方向：true = 向右，false = 向左")]
        public bool drawRight = true;

        [Tooltip("弧线碰到地面 / 墙时自动截断")]
        public bool stopOnCollision = true;

        [Tooltip("碰撞检测使用的图层（通常选 Ground）")]
        public LayerMask groundLayer;

        [Tooltip("弧线上升段颜色")]
        public Color arcAscendColor  = new Color(0.2f, 0.9f, 0.2f, 0.85f);

        [Tooltip("弧线悬停段颜色")]
        public Color arcApexColor    = new Color(1.0f, 0.8f, 0.1f, 0.85f);

        [Tooltip("弧线下降段颜色")]
        public Color arcDescendColor = new Color(0.9f, 0.3f, 0.2f, 0.85f);

        // ─────────────────────────────────────────────
        //  Runtime — Read-only（由 Calculate 填充）
        // ─────────────────────────────────────────────
        [Header("Runtime (Read-Only / Debug)")]
        [SerializeField, HideInInspector] private float initialJumpVelocity;
        [SerializeField, HideInInspector] private float gravity;
        [SerializeField, HideInInspector] private float jumpDuration;

        // ── 公开只读属性 ───────────────────────────────
        public float InitialJumpVelocity => initialJumpVelocity;
        public float Gravity             => gravity;
        public float JumpDuration        => jumpDuration;

        /// <summary>
        /// 从设计参数推导所有运行时物理常量。
        /// 在 PlayerController.OnEnable 以及编辑器修改后调用。
        /// </summary>
        public void Calculate()
        {
            // v0 = 2h / t_apex
            initialJumpVelocity = 2f * maxJumpHeight / timeTillJumpApex;

            // g = -2h / t_apex²  （负值，方向向下）
            gravity = -2f * maxJumpHeight / (timeTillJumpApex * timeTillJumpApex);

            // 总跳跃计时器时长 = 上升 + 悬空
            jumpDuration = timeTillJumpApex + jumpApexDuration;
        }

#if UNITY_EDITOR
        private void OnValidate() => Calculate();
#endif
    }
}