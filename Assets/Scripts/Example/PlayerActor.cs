using UnityEngine;

/// <summary>
/// 玩家示例 —— 继承 Actor，演示如何利用 MoveX / MoveY 实现：
///  · 水平移动（带加速/摩擦力）
///  · 重力 + 跳跃
///  · 土狼时间（Coyote Time）
///  · 跳跃缓冲（Jump Buffer）
///  · 蔚蓝风格的可变跳跃高度（松开跳跃键时截断纵向速度）
/// </summary>
public class PlayerActor : Actor
{
    // ── 移动参数 ─────────────────────────────────────────────────
    [Header("Horizontal Movement")]
    [SerializeField] private float maxSpeedX      = 90f;   // px/s
    [SerializeField] private float acceleration   = 1000f; // px/s²
    [SerializeField] private float friction       = 800f;  // px/s²（无输入时）
    [SerializeField] private float airFriction    = 400f;

    [Header("Jump")]
    [SerializeField] private float jumpSpeed      = 200f;  // px/s
    [SerializeField] private float gravity        = 500f;  // px/s²
    [SerializeField] private float maxFallSpeed   = 300f;  // px/s（向下最大）
    [SerializeField] private float jumpCutFactor  = 0.5f;  // 松开跳跃键时纵速乘以此值

    [Header("Coyote / Buffer")]
    [SerializeField] private float coyoteTime     = 0.1f;  // s
    [SerializeField] private float jumpBufferTime = 0.1f;  // s

    // ── 运行时状态 ───────────────────────────────────────────────
    private Vector2 velocity;
    private bool    onGround;
    private float   coyoteTimer;
    private float   jumpBufferTimer;
    private bool    wasOnGroundLastFrame;

    // ── Unity 生命周期 ──────────────────────────────────────────
    protected override void Awake()
    {
        base.Awake();
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // 1. 落地检测
        onGround = CheckOnGround();

        // 土狼时间计时
        if (wasOnGroundLastFrame && !onGround)
            coyoteTimer = coyoteTime;
        else if (onGround)
            coyoteTimer = coyoteTime;
        else
            coyoteTimer -= dt;

        wasOnGroundLastFrame = onGround;

        // 2. 跳跃缓冲
        if (Input.GetButtonDown("Jump"))
            jumpBufferTimer = jumpBufferTime;
        else
            jumpBufferTimer -= dt;

        // 3. 水平输入
        float inputX = Input.GetAxisRaw("Horizontal");
        float curFriction = onGround ? friction : airFriction;

        if (Mathf.Abs(inputX) > 0.01f)
            velocity.x = Mathf.MoveTowards(velocity.x, inputX * maxSpeedX, acceleration * dt);
        else
            velocity.x = Mathf.MoveTowards(velocity.x, 0f, curFriction * dt);

        // 4. 跳跃触发
        bool canJump = coyoteTimer > 0f;
        if (jumpBufferTimer > 0f && canJump)
        {
            velocity.y      = jumpSpeed;
            coyoteTimer     = 0f;
            jumpBufferTimer = 0f;
        }

        // 5. 可变跳跃高度：松开时截断上升速度
        if (Input.GetButtonUp("Jump") && velocity.y > 0f)
            velocity.y *= jumpCutFactor;

        // 6. 重力
        if (!onGround)
            velocity.y = Mathf.Max(velocity.y - gravity * dt, -maxFallSpeed);
        else if (velocity.y < 0f)
            velocity.y = 0f;

        // 7. 调用父类移动（每轴独立，带碰撞回调）
        MoveX(velocity.x * dt, OnHitWall);
        MoveY(velocity.y * dt, OnHitCeiling);
    }

    // ── 碰撞回调 ────────────────────────────────────────────────

    private void OnHitWall()
    {
        velocity.x = 0f;
    }

    private void OnHitCeiling()
    {
        // 向上碰头或向下落地
        if (velocity.y > 0f)
            velocity.y = 0f;   // 撞天花板
        // 落地由 CheckOnGround 处理，这里不需要额外操作
    }

    // ── 工具方法 ────────────────────────────────────────────────

    /// <summary>检测脚底一像素下方是否有 Solid。</summary>
    private bool CheckOnGround()
    {
        var checkBox = new RectInt(Bounds.x, Bounds.y - 1, Bounds.width, 1);
        return PhysicsWorld.Instance.SolidOverlap(checkBox);
    }

    // ── 覆写 IsRiding：站在顶部才算骑乘 ─────────────────────────
    public override bool IsRiding(Solid solid)
    {
        // 使用与 CheckOnGround 相同逻辑，但只检测指定 Solid
        var checkBox = new RectInt(Bounds.x, Bounds.y - 1, Bounds.width, 1);
        return solid.Collidable && solid.Bounds.Overlaps(checkBox);
    }

    // ── 覆写 Squish：被压死时触发（可改为掉血等）────────────────
    public override void Squish()
    {
        Debug.Log("[Player] Squished! Game Over.");
        // 这里仅打日志；实际游戏中可触发死亡流程
        // base.Squish(); // 调用则销毁 GameObject
    }
}
