using System;
using System.Collections.Generic;
using UnityEngine;
using Utils;

namespace Platformer
{
    public class PlayerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator    animator;
        [SerializeField] private InputReader input;
        [SerializeField] private PhysicCheck physicCheck;

        [Header("Stats")]
        [SerializeField] private PlayerMovementStats movementStats;

        [Header("PlayerMovement")]
        [SerializeField] private Vector2 movement;
        private Vector2 playerVelocity;

        // ── 自管理位置（替代 rb.position）────────────────────────
        // 含义：角色 GameObject 的世界坐标（即 transform.position 的 xy）。
        // 在 FixedUpdate 末尾通过 CommitPosition() 写回 transform.position。
        private Vector2 _position;

        private StateMachine stateMachine;

        private List<Timer>    timers;
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
        private const int   PPU       = 16;
        private const float SkinWidth = 0.02f;

        private float xRemainder;
        private float yRemainder;

        // ── Debug 可视化 ──────────────────────────────────────────
        private Vector2 _lastMoveXCheckPos;
        private Vector2 _lastMoveXCheckSize;
        private Vector2 _lastMoveYCheckPos;
        private Vector2 _lastMoveYCheckSize;

        #region Unity Methods
        private void Awake()
        {
            animator    = GetComponent<Animator>();
            input       = new InputReader();
            physicCheck = GetComponent<PhysicCheck>();

            // 用 transform 初始化自管理位置
            _position = transform.position;

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
            // 0. 同步 _position（防止外部直接修改 transform 导致漂移）
            //    如果 transform 只由本脚本控制，可以删掉这行。
            _position = transform.position;

            // 1. 碰撞检测
            physicCheck.CheckCollisions();
            isGrounded = physicCheck.IsGrounded;
            isCeil     = physicCheck.IsCeil;

            // 2. 状态机物理更新
            stateMachine.FixedUpdate();

            // 3. 像素级移动
            ApplyMovement();

            // 4. 将 _position 写回 transform（物理移动的唯一出口）
            CommitPosition();

            // 5. 动画同步
            UpdateAnimator();
        }

        /// <summary>把内部位置写回 transform，保留 Z 轴不变。</summary>
        private void CommitPosition()
        {
            transform.position = new Vector3(_position.x, _position.y, transform.position.z);
        }
        #endregion

        #region Physics Event Handlers
        private void HandleLanded()
        {
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

        // 记录是否提前松开了跳跃键（用于下落段选择重力倍率）
        private bool _jumpEndedEarly;

        public void HandleJump()
        {
            // ── 阶段一：jumpTimer 运行中 = 上升段 ─────────────────
            // PlayerMovementStats 的物理模型：
            //   v0 = 2h / t_apex，g = -2h / t_apex²
            // 意味着从 v0 开始每帧施加一次 g，经过 t_apex 秒后速度恰好为 0。
            // jumpTimer 时长 = timeTillJumpApex + jumpApexDuration，
            // 当剩余时间 ≤ jumpApexDuration 时进入顶点悬停（速度清零）。
            if (jumpTimer.IsRunning)
            {
                // 顶点悬停窗口：强制清零 Y 速度，制造短暂飘浮感
                if (jumpTimer.Compare(movementStats.jumpApexDuration))
                {
                    playerVelocity.y = 0f;
                    return; // 悬停期间不施加任何重力
                }

                // 上升段：施加基础重力（负值），速度从 v0 逐渐减小趋向 0
                // 与可视化公式 displacement += 0.5 * g * t² 对应
                playerVelocity.y += movementStats.Gravity * Time.fixedDeltaTime;
                return;
            }

            // ── 阶段二：jumpTimer 已停止 = 下落段 ─────────────────
            // 限制最大下落速度
            if (playerVelocity.y <= movementStats.maxFallSpeed)
            {
                playerVelocity.y = movementStats.maxFallSpeed;
                return;
            }

            // 根据是否提前松键选择重力倍率：
            //   提前松键（_jumpEndedEarly）→ jumpEndEarlyGravityModifier（更强，短跳）
            //   自然下落                   → gravityMultiplier（正常下落手感）
            float multiplier = _jumpEndedEarly
                ? movementStats.jumpEndEarlyGravityModifier
                : movementStats.gravityMultiplier;

            playerVelocity.y += movementStats.Gravity * multiplier * Time.fixedDeltaTime;
        }

        private void OnJump(bool performed)
        {
            if (performed)
            {
                jumpBufferTimer.Start();
                _jumpEndedEarly = false;   // 新一次起跳，重置标志
            }
            else if (jumpTimer.IsRunning)
            {
                // 提前松键：停止 jumpTimer，让下落段用更强重力
                jumpTimer.Stop();
                _jumpEndedEarly = true;
            }
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
            MoveX(playerVelocity.x * Time.fixedDeltaTime * PPU, OnHitWall);
            MoveY(playerVelocity.y * Time.fixedDeltaTime * PPU, OnHitVertical);
        }

        private void OnHitWall()     => playerVelocity.x = 0f;
        private void OnHitVertical() => playerVelocity.y = 0f;

        private Vector2 GetCheckCenter(Vector2 nextPos)
        {
            return nextPos + physicCheck.col.offset;
        }

        // 用 col.size 代替 col.bounds.size，不依赖物理引擎刷新
        private Vector2 CheckSize => physicCheck.col.size - Vector2.one * SkinWidth;

        /// <summary>水平像素移动，每步 1px（1/PPU Unit）。</summary>
        private void MoveX(float amount, Action onCollide)
        {
            xRemainder += amount;
            int move = Mathf.RoundToInt(xRemainder);
            if (move == 0) return;

            xRemainder -= move;
            int sign = (int)Mathf.Sign(move);

            while (move != 0)
            {
                Vector2 nextPos     = _position + new Vector2(sign / (float)PPU, 0f);
                Vector2 checkCenter = GetCheckCenter(nextPos);
                Vector2 checkSize   = CheckSize;

                _lastMoveXCheckPos  = checkCenter;
                _lastMoveXCheckSize = checkSize;

                bool hit = Physics2D.OverlapBox(
                    checkCenter, checkSize, 0f, physicCheck.groundLayer);

                if (!hit)
                {
                    _position = nextPos;
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

        /// <summary>垂直像素移动，每步 1px（1/PPU Unit）。</summary>
        private void MoveY(float amount, Action onCollide)
        {
            yRemainder += amount;
            int move = Mathf.RoundToInt(yRemainder);
            if (move == 0) return;

            yRemainder -= move;
            int sign = (int)Mathf.Sign(move);

            while (move != 0)
            {
                Vector2 nextPos     = _position + new Vector2(0f, sign / (float)PPU);
                Vector2 checkCenter = GetCheckCenter(nextPos);
                Vector2 checkSize   = CheckSize;

                _lastMoveYCheckPos  = checkCenter;
                _lastMoveYCheckSize = checkSize;

                bool hit = Physics2D.OverlapBox(
                    checkCenter, checkSize, 0f, physicCheck.groundLayer);

                if (!hit)
                {
                    _position = nextPos;
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

        private void OnDrawGizmos()
        {
            if (physicCheck == null || physicCheck.col == null) return;

            Vector2 origin = Application.isPlaying ? _position : (Vector2)transform.position;

            // 绿色：当前碰撞盒
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(origin + physicCheck.col.offset, CheckSize);

            if (!Application.isPlaying) return;

            // 红色：MoveX 最后检测框
            if (_lastMoveXCheckSize != Vector2.zero)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(_lastMoveXCheckPos, _lastMoveXCheckSize);
            }

            // 蓝色：MoveY 最后检测框
            if (_lastMoveYCheckSize != Vector2.zero)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireCube(_lastMoveYCheckPos, _lastMoveYCheckSize);
            }
        }
    }
}