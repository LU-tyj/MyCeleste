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

        public CountdownTimer JumpTimer => jumpTimer;
        public CountdownTimer JumpBufferTimer => jumpBufferTimer;
        public CountdownTimer JumpCoyoteTimer => jumpCoyoteTimer;
        public PlayerMovementStats Stats => movementStats;
        
        #region Unity Methods
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            input = new InputReader();
            physicCheck = GetComponent<PhysicCheck>();

            physicCheck.OnLanded      += HandleLanded;
            physicCheck.OnLeftGround  += HandleLeftGround;
            physicCheck.OnHitCeiling  += HandleHitCeiling;

            SetTimers();
            SetStateMachine();
        }

        private void OnDestroy()
        {
            physicCheck.OnLanded      -= HandleLanded;
            physicCheck.OnLeftGround  -= HandleLeftGround;
            physicCheck.OnHitCeiling  -= HandleHitCeiling;
        }

        private void Update()
        {
            // 1. 获取输入方向
            movement = input.Direction;
            
            // 2. 状态更新
            stateMachine.Update();
            
            // 3. 处理计时器
            HandleTimers();
        }

        private void FixedUpdate()
        {
            // 1. 处理碰撞
            physicCheck.CheckCollisions();

            // 2. 同步状态
            isGrounded = physicCheck.IsGrounded;
            isCeil     = physicCheck.IsCeil;
            
            // 3. 状态机物理更新
            stateMachine.FixedUpdate();
            
            // 4. 物理应用
            ApplyMovement();
        }

        private void Start() => input.EnablePlayerActions();

        private void OnEnable()
        {
            input.Jump += OnJump;
            movementStats.Calculate();
        }

        private void OnDisable() => input.Jump -= OnJump;
        #endregion

        #region Physics Event Handlers
        private void HandleLanded()
        {
            playerVelocity.y = Mathf.Max(playerVelocity.y, 0f);
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
            jumpTimer = new CountdownTimer(movementStats.JumpDuration);
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

            // Locomotion → Jump：有跳跃缓冲且满足起跳条件
            At(locomotionState, jumpState, new FuncPredicate(CanJump));
    
            // Locomotion → Air：离地但没有主动跳跃（走落平台）
            At(locomotionState, airState,  new FuncPredicate(ReturnToAir));
    
            // Jump → Air：跳跃计时结束（顶点之后）
            At(jumpState, airState, new FuncPredicate(() => !jumpTimer.IsRunning));
    
            // Air → Jump：空中消费土狼时间跳跃
            At(airState, jumpState, new FuncPredicate(CanCoyoteJump));

            // 任意 → Locomotion：落地且没有跳跃
            Any(locomotionState, new FuncPredicate(ReturnToLocomotionState));

            stateMachine.SetState(locomotionState);
        }

        private bool ReturnToLocomotionState() => isGrounded && !jumpTimer.IsRunning && !jumpBufferTimer.IsRunning;

        private bool ReturnToAir()
        {
            return !isGrounded && !jumpTimer.IsRunning;
        }

        // 地面起跳条件（给状态机谓词用，纯查询）
        private bool CanJump() =>
            jumpBufferTimer.IsRunning && isGrounded && !jumpTimer.IsRunning;

        // 土狼时间起跳条件
        private bool CanCoyoteJump() =>
            jumpBufferTimer.IsRunning && jumpCoyoteTimer.IsRunning && !jumpTimer.IsRunning;

        private void HandleTimers()
        {
            foreach (var timer in timers) 
                timer.Tick(Time.deltaTime);
        }

        private void UpdateAnimator()
        {
            animator.SetFloat("SpeedX", rb.linearVelocityX);
            animator.SetFloat("SpeedY", rb.linearVelocityY);
        }
        #endregion

        #region Jump

        public void HandleJump()
        {
            // 顶点悬停：清零 Y 速度
            if (jumpTimer.IsRunning && jumpTimer.Compare(movementStats.jumpApexDuration))
            {
                playerVelocity.y = 0f;
            }

            if (jumpTimer.IsRunning)
            {
                // 上升阶段：正常施加重力（减速上升）
                playerVelocity.y += movementStats.Gravity * Time.fixedDeltaTime;
            }
            else
            {
                if (playerVelocity.y < movementStats.maxFallSpeed)
                {
                    playerVelocity.y = movementStats.maxFallSpeed;
                    return;
                }
                // 下落阶段：区分自然下落与提前松键
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
            {
                jumpBufferTimer.Start();
            }
            else if (jumpTimer.IsRunning)
            {
                jumpTimer.Stop();
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

        private void ApplyMovement() => rb.linearVelocity = playerVelocity;

        private void At(IState from, IState to, IPredicate condition) =>
            stateMachine.AddTransition(from, to, condition);
        private void Any(IState to, IPredicate condition) =>
            stateMachine.AddAnyTransition(to, condition);
    }
}