using System;
using System.Collections.Generic;
using UnityEngine;
using Utils;

namespace Platformer
{
    public class PlayerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private Animator animator;
        [SerializeField] private InputReader input;
        [SerializeField] private PhysicCheck physicCheck;

        [Header("Stats")]
        [SerializeField] private PlayerMovementStats movementStats;

        [Header("PlayerMovement")]
        [SerializeField] private Vector2 movement;
        private Vector2 playerVelocity;

        private StateMachine stateMachine;

        private List<Timer> timers;
        private CountdownTimer jumpTimer;
        private CountdownTimer jumpBufferTimer;
        private CountdownTimer jumpCoyoteTimer;

        private bool isGrounded;
        private bool isCeil;

        public CountdownTimer JumpTimer        => jumpTimer;
        public CountdownTimer JumpBufferTimer  => jumpBufferTimer;
        public CountdownTimer JumpCoyoteTimer  => jumpCoyoteTimer;
        public PlayerMovementStats Stats       => movementStats;

        // ── 像素移动 ──────────────────────────────────────────────
        // PPU = Pixels Per Unit，与 Sprite 导入设置保持一致
        private const int PPU = 16;
        private float xRemainder;
        private float yRemainder;

        // 碰撞盒收缩量：避免紧贴墙时误判，值要小于 1/PPU
        private const float SkinWidth = 0.02f;

        #region Unity Methods
        private void Awake()
        {
            rb        = GetComponent<Rigidbody2D>();
            animator  = GetComponent<Animator>();
            input     = new InputReader();
            physicCheck = GetComponent<PhysicCheck>();

            physicCheck.OnLanded     += HandleLanded;
            physicCheck.OnLeftGround += HandleLeftGround;
            physicCheck.OnHitCeiling += HandleHitCeiling;

            SetTimers();
            SetStateMachine();
        }

        private void OnDestroy()
        {
            physicCheck.OnLanded     -= HandleLanded;
            physicCheck.OnLeftGround -= HandleLeftGround;
            physicCheck.OnHitCeiling -= HandleHitCeiling;
        }

        private void Start() => input.EnablePlayerActions();

        private void OnEnable()
        {
            input.Jump += OnJump;
            movementStats.Calculate();
        }

        private void OnDisable() => input.Jump -= OnJump;

        private void Update()
        {
            movement = input.Direction;
            stateMachine.Update();
            HandleTimers();
        }

        private void FixedUpdate()
        {
            // 1. 碰撞检测
            physicCheck.CheckCollisions();
            isGrounded = physicCheck.IsGrounded;
            isCeil     = physicCheck.IsCeil;

            // 2. 状态机物理更新（内部调用 HandleMovement / HandleJump）
            stateMachine.FixedUpdate();

            // 3. 像素级移动（所有速度在状态机里算好后统一应用）
            ApplyMovement();

            // 4. 动画同步
            UpdateAnimator();
        }
        #endregion

        #region Physics Event Handlers
        private void HandleLanded()
        {
            // 落地：清除向下速度，保留向上（防止从下往上穿台阶时被清零）
            if (playerVelocity.y < 0f)
                playerVelocity.y = 0f;
        }

        private void HandleLeftGround()
        {
            jumpCoyoteTimer.Start();
        }

        private void HandleHitCeiling()
        {
            jumpTimer.Stop();
            playerVelocity.y = 0f;
        }
        #endregion

        #region Initialize
        private void SetTimers()
        {
            jumpTimer       = new CountdownTimer(movementStats.JumpDuration);
            jumpCoyoteTimer = new CountdownTimer(movementStats.jumpCoyoteDuration);
            jumpBufferTimer = new CountdownTimer(movementStats.jumpBufferDuration);

            timers = new List<Timer> { jumpTimer, jumpCoyoteTimer, jumpBufferTimer };
        }

        private void SetStateMachine()
        {
            stateMachine = new StateMachine();

            var locomotionState = new LocomotionState(this, animator);
            var jumpState       = new JumpState(this, animator);
            var airState        = new AirState(this, animator);

            At(locomotionState, jumpState, new FuncPredicate(CanJump));
            At(locomotionState, airState,  new FuncPredicate(ReturnToAir));
            At(jumpState,       airState,  new FuncPredicate(() => !jumpTimer.IsRunning));
            At(airState,        jumpState, new FuncPredicate(CanCoyoteJump));

            Any(locomotionState, new FuncPredicate(ReturnToLocomotionState));

            stateMachine.SetState(locomotionState);
        }

        private bool ReturnToLocomotionState() =>
            isGrounded && !jumpTimer.IsRunning && !jumpBufferTimer.IsRunning;

        private bool ReturnToAir()    => !isGrounded && !jumpTimer.IsRunning;
        private bool CanJump()        => jumpBufferTimer.IsRunning && isGrounded && !jumpTimer.IsRunning;
        private bool CanCoyoteJump()  => jumpBufferTimer.IsRunning && jumpCoyoteTimer.IsRunning && !jumpTimer.IsRunning;

        private void HandleTimers()
        {
            foreach (var timer in timers)
                timer.Tick(Time.deltaTime);
        }

        private void UpdateAnimator()
        {
            animator.SetFloat("SpeedX", playerVelocity.x);
            animator.SetFloat("SpeedY", playerVelocity.y);
        }
        #endregion

        #region Jump
        public void HandleJump()
        {
            // ── 顶点悬停 ────────────────────────────────────────
            // jumpApexDuration 是从 JumpDuration 末尾倒数的窗口，
            // 进入窗口时清零 Y 速度制造短暂悬停感
            if (jumpTimer.IsRunning && jumpTimer.Compare(movementStats.jumpApexDuration))
            {
                playerVelocity.y = 0f;
            }

            if (jumpTimer.IsRunning)
            {
                // ── 上升阶段 ────────────────────────────────────
                // Gravity 应为负值（向下），上升期间速度逐渐减小趋向 0
                playerVelocity.y += movementStats.Gravity * Time.fixedDeltaTime;
            }
            else
            {
                // ── 下落阶段 ────────────────────────────────────
                // 限制最大下落速度（maxFallSpeed 应为负值）
                if (playerVelocity.y < movementStats.maxFallSpeed)
                {
                    playerVelocity.y = movementStats.maxFallSpeed;
                    return;
                }

                // 下落时加强重力；若跳跃键提前松开则用更强的修正系数
                float gravity = movementStats.Gravity;
                gravity *= playerVelocity.y < 0f
                    ? movementStats.gravityMultiplier
                    : movementStats.jumpEndEarlyGravityModifier;

                playerVelocity.y += gravity * Time.fixedDeltaTime;
            }
        }

        private void OnJump(bool performed)
        {
            if (performed)
                jumpBufferTimer.Start();
            else if (jumpTimer.IsRunning)
                jumpTimer.Stop();
        }

        public void ApplyInitialJumpVelocity()
        {
            playerVelocity.y = movementStats.InitialJumpVelocity;
            jumpTimer.Start();
            jumpCoyoteTimer.Stop();
            jumpBufferTimer.Stop();
        }
        #endregion

        #region Horizontal
        public void HandleMovement()
        {
            playerVelocity.x = movement.x * movementStats.runSpeed;
        }
        #endregion

        #region Pixel Movement

        private void ApplyMovement()
        {
            MoveX(playerVelocity.x * Time.fixedDeltaTime, OnHitWall);
            MoveY(playerVelocity.y * Time.fixedDeltaTime, OnHitVertical);
        }

        private void OnHitWall()
        {
            playerVelocity.x = 0f;
        }

        private void OnHitVertical()
        {
            // 方向由 playerVelocity.y 判断：
            // 向上撞天花板 → HandleHitCeiling 已经清零
            // 向下落地    → HandleLanded 已经清零
            // 这里作为保险兜底
            playerVelocity.y = 0f;
        }

        /// <summary>
        /// 水平像素移动：每次 1 像素（1/PPU Unit），逐步检测碰撞。
        /// </summary>
        private void MoveX(float amount, Action onCollide)
        {
            xRemainder += amount;
            int move = Mathf.RoundToInt(xRemainder);
            if (move == 0) return;

            xRemainder -= move;
            int sign = (int)Mathf.Sign(move);

            while (move != 0)
            {
                Vector2 nextPos = rb.position + new Vector2(sign / (float)PPU, 0f);

                // OverlapBox 以角色中心为基准检测，收缩 SkinWidth 避免紧贴时误判
                bool hit = Physics2D.OverlapBox(
                    nextPos,
                    (Vector2)physicCheck.col.bounds.size - Vector2.one * SkinWidth,
                    0f,
                    physicCheck.groundLayer
                );

                if (!hit)
                {
                    rb.position = nextPos;
                    move -= sign;
                }
                else
                {
                    onCollide?.Invoke();
                    xRemainder = 0f;
                    break;
                }
            }
        }

        /// <summary>
        /// 垂直像素移动：每次 1 像素（1/PPU Unit），逐步检测碰撞。
        /// </summary>
        private void MoveY(float amount, Action onCollide)
        {
            yRemainder += amount;
            int move = Mathf.RoundToInt(yRemainder);
            if (move == 0) return;

            yRemainder -= move;
            int sign = (int)Mathf.Sign(move);

            while (move != 0)
            {
                Vector2 nextPos = rb.position + new Vector2(0f, sign / (float)PPU);

                bool hit = Physics2D.OverlapBox(
                    nextPos,
                    (Vector2)physicCheck.col.bounds.size - Vector2.one * SkinWidth,
                    0f,
                    physicCheck.groundLayer
                );

                if (!hit)
                {
                    rb.position = nextPos;
                    move -= sign;
                }
                else
                {
                    onCollide?.Invoke();
                    yRemainder = 0f;
                    break;
                }
            }
        }
        #endregion

        private void At(IState from, IState to, IPredicate condition) =>
            stateMachine.AddTransition(from, to, condition);
        private void Any(IState to, IPredicate condition) =>
            stateMachine.AddAnyTransition(to, condition);
    }
}