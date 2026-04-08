using System;
using System.Collections.Generic;
using UnityEngine;
using Utils;

namespace Platformer
{
    public class PlayerController : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private Animator animator;

        [SerializeField] private InputReader input;
        [SerializeField] private PhysicCheck physicCheck;

        [Header("Stats")] [SerializeField] private PlayerMovementStats movementStats;

        [Header("PlayerMovement")] [SerializeField]
        private Vector2 movement;

        private Vector2 faceDir = Vector2.right;
        private Vector2 playerVelocity;
        private int dashCount;

        private Vector2 _position;

        private StateMachine stateMachine;

        private List<Timer> timers;
        private CountdownTimer jumpTimer;
        private CountdownTimer jumpCDTimer;
        private CountdownTimer jumpBufferTimer;
        private CountdownTimer jumpCoyoteTimer;
        private CountdownTimer dashTimer;

        private bool isGrounded;
        private bool isCeil;
        private bool canCorrection = false;
        private bool jumpEndedEarly = false;
        private bool isWall;
        private bool isGrabPerformed = false;

        // ── 墙壁状态 ──────────────────────────────────────────────
        /// <summary>当前紧贴的墙壁方向（+1 右，-1 左，0 无）</summary>
        public int WallDir { get; private set; }
        /// <summary>角色是否面朝墙壁方向</summary>
        public bool IsFacingWall => WallDir != 0 && faceDir.x == WallDir;
        /// <summary>角色是否面朝墙壁外侧</summary>
        public bool IsFacingAwayFromWall => WallDir != 0 && faceDir.x == -WallDir;

        // ── 体力系统 ──────────────────────────────────────────────
        private float currentStamina;
        /// <summary>体力耗尽时为 true，下滑阻力不断减小</summary>
        private bool isExhausted;
        /// <summary>供 UI / Debug 读取当前体力（0~1 归一化）</summary>
        public float StaminaNormalized => currentStamina / movementStats.maxStamina;

        // ── 蹬墙跳锁定 ────────────────────────────────────────────
        /// <summary>蹬墙跳后短暂禁止重新抓墙，避免立即重进 GrabState</summary>
        private CountdownTimer wallJumpLockTimer;

        // ── 像素移动 ──────────────────────────────────────────────
        private const int PPU = 32;
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
            animator = GetComponent<Animator>();
            input = new InputReader();
            physicCheck = GetComponent<PhysicCheck>();

            // 用 transform 初始化自管理位置
            _position = transform.position;

            physicCheck.OnLanded += HandleLanded;
            physicCheck.OnLeftGround += HandleLeftGround;
            physicCheck.OnHitCeiling += HandleHitCeiling;

            SetTimers();
            SetStateMachine();
        }

        private void OnDestroy()
        {
            physicCheck.OnLanded -= HandleLanded;
            physicCheck.OnLeftGround -= HandleLeftGround;
            physicCheck.OnHitCeiling -= HandleHitCeiling;
        }

        private void Start()
        {
            input.EnablePlayerActions();
            dashCount = movementStats.dashToConsume;
        }

    private void OnEnable()
        {
            input.Jump += OnJump;
            input.Dash += OnDash;
            input.Grab += OnGrab;
            movementStats.Calculate();
        }
        
        private void OnDisable()
        {
            input.Jump -= OnJump;
            input.Dash -= OnDash;
            input.Grab -= OnGrab;
        }

        private void Update()
        {
            movement = input.Direction;
            stateMachine.Update();
            HandleTimers();
        }

        private void FixedUpdate()
        {
            // 0. 同步 _position（防止外部直接修改 transform 导致漂移）
            _position = transform.position;

            // 1. 碰撞检测
            physicCheck.CheckCollisions();
            isGrounded = physicCheck.IsGrounded;
            isCeil     = physicCheck.IsCeil;
            WallDir    = physicCheck.WallDirection;

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
            playerVelocity.y = 0f;
            dashCount = movementStats.dashToConsume;
            // 落地恢复体力
            currentStamina = movementStats.maxStamina;
            isExhausted    = false;
        }

        private void HandleLeftGround()
        {
            if (jumpTimer.IsRunning) return;
            
            jumpCoyoteTimer.Start();
        }

        private void HandleHitCeiling()
        {
            jumpTimer.Stop();
        }
        #endregion

        #region Initialize
        private void SetTimers()
        {
            jumpTimer       = new CountdownTimer(movementStats.JumpDuration);
            jumpCDTimer       = new CountdownTimer(movementStats.jumpCD);
            jumpCoyoteTimer = new CountdownTimer(movementStats.jumpCoyoteDuration);
            jumpBufferTimer = new CountdownTimer(movementStats.jumpBufferDuration);
            dashTimer       = new CountdownTimer(movementStats.dashDuration);
            wallJumpLockTimer = new CountdownTimer(0.25f);   // 蹬墙跳后 0.25s 内不能重新抓墙

            timers = new List<Timer> { jumpTimer, jumpCDTimer, jumpCoyoteTimer, jumpBufferTimer, dashTimer, wallJumpLockTimer };
        }

        private void SetStateMachine()
        {
            stateMachine = new StateMachine();

            var locomotionState = new LocomotionState(this, animator);
            var jumpState       = new JumpState(this, animator);
            var airState        = new AirState(this, animator);
            var dashState       = new DashState(this, animator);
            var wallState       = new WallState(this, animator);
            var grabState       = new GrabState(this, animator);

            At(locomotionState, jumpState, new FuncPredicate(CanJump));
            At(locomotionState, airState,  new FuncPredicate(ReturnToAir));
            At(jumpState,       airState,  new FuncPredicate(() => !jumpTimer.IsRunning));
            At(airState,        jumpState, new FuncPredicate(() => CanCoyoteJump() || CanJump()));
            At(dashState, locomotionState, new FuncPredicate(() => !dashTimer.IsRunning && isGrounded));
            At(dashState, airState,        new FuncPredicate(() => !dashTimer.IsRunning && !isGrounded));

            // ── 墙壁相关 ──────────────────────────────────────────
            // Slide：空中 + 紧贴墙 + 朝墙移动 + 没按抓墙 + 无蹬墙跳锁定 → WallState
            At(airState, wallState, new FuncPredicate(CanEnterWallSlide));
            // Grab：空中 + 紧贴墙 + 按下抓墙键 + 无蹬墙跳锁定 → GrabState
            At(airState,  grabState, new FuncPredicate(CanEnterGrab));
            At(wallState, grabState, new FuncPredicate(CanEnterGrab));
            // 从 GrabState 切换回 Slide
            At(grabState, wallState, new FuncPredicate(() => !isGrabPerformed && CanEnterWallSlide()));
            // 离墙 → 回到空中
            At(wallState, airState, new FuncPredicate(() => !physicCheck.IsWall || isGrounded));
            At(grabState, airState, new FuncPredicate(() => !physicCheck.IsWall || isGrounded));

            Any(locomotionState, new FuncPredicate(ReturnToLocomotionState));
            Any(dashState,       new FuncPredicate(() => dashTimer.IsRunning));

            stateMachine.SetState(locomotionState);
        }

        private bool ReturnToLocomotionState()
        {
            return isGrounded && !jumpTimer.IsRunning && !jumpBufferTimer.IsRunning && !dashTimer.IsRunning;
        }

        private bool ReturnToAir()    => !isGrounded && !jumpTimer.IsRunning;
        private bool CanJump()
        {
            return jumpBufferTimer.IsRunning && isGrounded && !jumpTimer.IsRunning && !jumpCDTimer.IsRunning;
        }

        private bool CanCoyoteJump()  => jumpBufferTimer.IsRunning && jumpCoyoteTimer.IsRunning && !jumpTimer.IsRunning;

        // ── 墙壁状态判断 ──────────────────────────────────────────
        private bool CanEnterWallSlide() =>
            physicCheck.IsWall && !isGrabPerformed && !wallJumpLockTimer.IsRunning &&
            movement.x != 0 && Mathf.Sign(movement.x) == physicCheck.WallDirection;

        private bool CanEnterGrab() =>
            !isGrounded && physicCheck.IsWall && isGrabPerformed && !wallJumpLockTimer.IsRunning;

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

        #region Dash
        public void HandleDash()
        {
            var dashDir = movement != Vector2.zero ? movement.normalized : faceDir;
            playerVelocity = dashDir * movementStats.dashSpeed;
        }

        public void ApplyInitialDashStats()
        {
            canCorrection = true;
        }

        public void ApplyEndOfDashStats()
        {
            canCorrection = false;
            playerVelocity = Vector2.zero;
            playerVelocity.x = Mathf.MoveTowards(playerVelocity.x, 0, movementStats.acceleration * Time.fixedDeltaTime);
            playerVelocity.y = Mathf.MoveTowards(playerVelocity.y, 0, movementStats.acceleration * Time.fixedDeltaTime);
            if (isGrounded) dashCount = movementStats.dashToConsume;
        }
        
        private void OnDash(bool performed)
        {
            if (performed && dashCount > 0)
            {
                dashCount--;
                dashTimer.Start();
            }
        }
        #endregion

        #region Wall Grab & Climb & Slide & Jump

        // ── WallState（Slide）────────────────────────────────────
        public void HandleWallSlide()
        {
            // 减速下滑：将 Y 速度朝 wallSlideSpeed 靠拢
            playerVelocity.x = 0f;
            playerVelocity.y = Mathf.MoveTowards(
                playerVelocity.y,
                movementStats.wallSlideSpeed,
                movementStats.deceleration * Time.fixedDeltaTime);
        }

        public void HandleWallJump()
        {
            // WallState 下也可蹬墙跳（朝外按跳键）
            if (!jumpBufferTimer.IsRunning) return;
            if (!IsFacingAwayFromWall)      return;

            PerformWallJump();
        }

        public void ApplyInitialWallStats()
        {
            playerVelocity.x = 0f;
        }

        public void ApplyEndOfWallStats() { }

        // ── GrabState（抓墙 / 爬墙）────────────────────────────
        /// <summary>进入 GrabState 时调用：初始化体力、清零速度。</summary>
        public void ApplyInitialGrabStats()
        {
            // 若还有体力就保留，让连续爬墙能持续扣除
            if (currentStamina <= 0f)
            {
                isExhausted    = true;
                currentStamina = 0f;
            }
            playerVelocity = Vector2.zero;
        }

        /// <summary>离开 GrabState 时调用。</summary>
        public void ApplyEndOfGrabStats() { }

        /// <summary>GrabState 每帧 FixedUpdate 调用：处理爬墙 + 体力消耗 + 蹬墙跳。</summary>
        public void HandleGrab()
        {
            playerVelocity.x = 0f;   // 始终贴墙

            if (isExhausted)
            {
                // 体力耗尽：下滑阻力逐渐减小，速度趋向 wallExhaustedSlideSpeed
                playerVelocity.y = Mathf.Lerp(
                    playerVelocity.y,
                    movementStats.wallExhaustedSlideSpeed,
                    movementStats.exhaustedLerpSpeed * Time.fixedDeltaTime);
                return;
            }

            // ── 体力充足时 ──────────────────────────────────────
            float drain;
            float targetY;

            if (movement.y > 0.1f)           // 按上：向上爬
            {
                targetY = movementStats.wallClimbSpeed;
                drain   = movementStats.staminaDrainClimb;
            }
            else if (movement.y < -0.1f)     // 按下：主动往下
            {
                targetY = movementStats.wallCreepDownSpeed;
                drain   = movementStats.staminaDrainHold;
            }
            else                             // 静止悬挂
            {
                targetY = 0f;
                drain   = movementStats.staminaDrainHold;
            }

            playerVelocity.y = Mathf.MoveTowards(
                playerVelocity.y, targetY,
                movementStats.acceleration * Time.fixedDeltaTime);

            // 消耗体力
            currentStamina -= drain * Time.fixedDeltaTime;
            if (currentStamina <= 0f)
            {
                currentStamina = 0f;
                isExhausted    = true;
            }

            // ── 蹬墙跳 ──────────────────────────────────────────
            HandleGrabJump();
        }

        private void HandleGrabJump()
        {
            if (!jumpBufferTimer.IsRunning) return;

            if (IsFacingAwayFromWall)
            {
                // 面朝外：蹬墙跳，解除抓墙
                PerformWallJump();
            }
            else
            {
                // 面朝墙 / 面朝上：原地向上弹跳，消耗额外体力
                if (!isExhausted)
                {
                    currentStamina = Mathf.Max(0f, currentStamina - movementStats.wallJumpStaminaCost);
                    if (currentStamina <= 0f) isExhausted = true;
                }
                // 给一个向上的速度，利用普通跳跃 timer
                playerVelocity.y = movementStats.InitialJumpVelocity;
                jumpTimer.Start();
                jumpCDTimer.Start();
                jumpBufferTimer.Stop();
            }
        }

        /// <summary>蹬墙跳：给予向外水平速度 + 纵向速度，启动跳跃 timer。</summary>
        private void PerformWallJump()
        {
            // 水平方向：朝墙外
            playerVelocity.x = -WallDir * movementStats.wallJumpHorizontalSpeed;
            playerVelocity.y  = movementStats.InitialJumpVelocity * movementStats.wallJumpVerticalMultiplier;

            jumpTimer.Start();
            jumpCDTimer.Start();
            jumpCoyoteTimer.Stop();
            jumpBufferTimer.Stop();

            // 蹬墙跳锁定：短暂禁止重新进入 WallState / GrabState
            wallJumpLockTimer.Start();

            // 更新朝向为墙外
            faceDir = new Vector2(-WallDir, 0f);
        }
        
        private void OnGrab(bool performed)
        {
            if (performed)
            {
                isGrabPerformed = true;
            }
            else
            {
                isGrabPerformed = false;
            }
        }
        

        #endregion
        
        #region Jump
        public void HandleJump()
        {
            if (jumpTimer.IsRunning)
            {
                // 顶点悬停窗口：强制清零 Y 速度，制造短暂飘浮感
                if (jumpTimer.Compare(movementStats.jumpApexDuration))
                {
                    playerVelocity.y = 0f;
                    return; // 悬停期间不施加任何重力
                }

                // 上升段：施加基础重力（负值），速度从 v0 逐渐减小趋向 0
                playerVelocity.y += movementStats.Gravity * Time.fixedDeltaTime;
                return;
            }

            // 限制最大下落速度
            if (playerVelocity.y <= movementStats.maxFallSpeed)
            {
                playerVelocity.y = movementStats.maxFallSpeed;
                return;
            }

            float multiplier = jumpEndedEarly
                ? movementStats.jumpEndEarlyGravityModifier
                : movementStats.gravityMultiplier;

            playerVelocity.y += movementStats.Gravity * multiplier * Time.fixedDeltaTime;
        }

        private void OnJump(bool performed)
        {
            if (performed)
            {
                jumpBufferTimer.Start();
                jumpEndedEarly = false;
            }
            else if (jumpTimer.IsRunning)
            {
                jumpTimer.Stop();
                jumpEndedEarly = true;
            }
        }

        public void ApplyInitialJumpStats()
        {
            playerVelocity.y = movementStats.InitialJumpVelocity;
            jumpTimer.Start();
            jumpCDTimer.Start();
            jumpCoyoteTimer.Stop();
            jumpBufferTimer.Stop();
            
            canCorrection = true;
        }

        public void ApplyEndOfJumpStats()
        {
            canCorrection = false;
        }
        #endregion

        #region Horizontal
        public void HandleMovement()
        {
            faceDir = movement.x != 0 ? new Vector2(Mathf.Sign(movement.x), 0f) : faceDir;
            
            float targetVelocityX = movement.x * movementStats.runSpeed;
    
            float acceleration = (Mathf.Abs(movement.x) > 0.01f)
                ? movementStats.acceleration
                : movementStats.deceleration;
    
            playerVelocity.x = Mathf.MoveTowards(playerVelocity.x, targetVelocityX, acceleration * Time.fixedDeltaTime);
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
            return nextPos + physicCheck.ColOffset;
        }

        private Vector2 CheckSize => physicCheck.ColSize - Vector2.one * SkinWidth;

        private bool TryCornerCorrectionX(int sign)
        {
            if (!canCorrection) return false;
            for (int i = 1; i <= movementStats.maxCorrection; i++)
            {
                // 分别尝试向上、向下偏移 i px
                for (int dir = -1; dir <= 1; dir += 2)
                {
                    float offsetY = dir * i / (float)PPU;
                    Vector2 testPos = _position + new Vector2(sign / (float)PPU, offsetY);

                    Vector2 checkCenter = GetCheckCenter(testPos);
                    bool canMove = !Physics2D.OverlapBox(
                        checkCenter, CheckSize, 0f, physicCheck.groundLayer);

                    if (canMove)
                    {
                        // 垂直方向同步修正 remainder，避免下一帧抖动
                        yRemainder = 0f;
                        _position  = new Vector2(_position.x, _position.y + offsetY);
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        private bool TryCornerCorrectionY(int sign)
        {
            if (!canCorrection) return false;
            for (int i = 1; i <= movementStats.maxCorrection; i++)
            {
                // 分别尝试向左、向右偏移 i px
                for (int dir = -1; dir <= 1; dir += 2)
                {
                    float offsetX = dir * i / (float)PPU;
                    Vector2 testPos = _position + new Vector2(offsetX, sign / (float)PPU);

                    Vector2 checkCenter = GetCheckCenter(testPos);
                    bool canMove = !Physics2D.OverlapBox(
                        checkCenter, CheckSize, 0f, physicCheck.groundLayer);

                    if (canMove)
                    {
                        // 水平方向同步修正 remainder，避免下一帧抖动
                        xRemainder = 0f;
                        _position  = new Vector2(_position.x + offsetX, _position.y);
                        return true;
                    }
                }
            }

            return false;
        }

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
                    if (TryCornerCorrectionX(sign))
                    {
                        // 修正成功，继续剩余的水平移动
                        move -= sign;
                        continue;
                    }
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
                    if (TryCornerCorrectionY(sign))
                    {
                        // 修正成功，继续剩余的垂直移动
                        move -= sign;
                        continue;
                    }
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
            if (physicCheck == null) return;

            Vector2 origin = Application.isPlaying ? _position : (Vector2)transform.position;

            // 绿色：当前碰撞盒
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(origin + physicCheck.ColOffset, CheckSize);

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