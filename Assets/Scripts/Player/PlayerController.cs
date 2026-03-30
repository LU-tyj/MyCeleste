using System.Collections.Generic;
using UnityEngine;
using Utils;

namespace Platformer
{
    public class PlayerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BoxCollider2D col;
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private Animator animator;
        [SerializeField] private InputReader input;

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float runSpeed = 15f;

        [Header("Jump & Air Settings")]
        [SerializeField] private float jumpForce = 10f;
        [SerializeField] private float jumpDuration = 0.5f;
        [SerializeField] private float gravityMultiplier = 3f;
        [SerializeField] private float jumpCoyoteDuration = 0.2f;
        [SerializeField] private float jumpBufferDuration = 0.1f;
        // 来自代码1：松键后对上升阶段施加的重力倍率
        [SerializeField] private float jumpEndEarlyGravityModifier = 3f;

        private List<Timer> timers;
        private CountdownTimer jumpTimer;
        private CountdownTimer jumpBufferTimer;
        private CountdownTimer jumpCoyoteTimer;

        [Header("Collision Settings")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float ceilCheckDistance;
        [SerializeField] private float groundCheckDistance;
        [SerializeField] private bool isGrounded;
        [SerializeField] private bool isCeil;
        private bool colliderCache;

        [Header("PlayerMovement")]
        [SerializeField] private Vector2 movement;

        private StateMachine stateMachine;

        private float jumpVelocity;
        // 来自代码1：标记玩家是否提前松键
        private bool endedJumpEarly;

        private void Awake()
        {
            col = GetComponent<BoxCollider2D>();
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            input = new InputReader();

            colliderCache = Physics2D.queriesStartInColliders;

            SetTimers();
            SetStateMachine();
        }

        private void Update()
        {
            movement = input.Direction.normalized;
            stateMachine.Update();
            HandleTimers();
            UpdateAnimator();
        }

        private void FixedUpdate()
        {
            CheckCollisions();
            // Buffer 命中检测移到物理步骤开头，确保同帧 HandleJump 能拿到正确状态
            TryConsumeJumpBuffer();
            stateMachine.FixedUpdate();
        }

        private void Start() => input.EnablePlayerActions();

        private void OnEnable() => input.Jump += OnJump;
        private void OnDisable() => input.Jump -= OnJump;

        void SetTimers()
        {
            jumpTimer = new CountdownTimer(jumpDuration);
            jumpCoyoteTimer = new CountdownTimer(jumpCoyoteDuration);
            jumpBufferTimer = new CountdownTimer(jumpBufferDuration);

            timers = new List<Timer> { jumpTimer, jumpCoyoteTimer, jumpBufferTimer };
        }

        void SetStateMachine()
        {
            stateMachine = new StateMachine();

            var locomotionState = new LocomotionState(this, animator);
            var jumpState = new JumpState(this, animator);

            At(locomotionState, jumpState,
                new FuncPredicate(() => jumpTimer.IsRunning || ReturnToAir()));
            Any(locomotionState, new FuncPredicate(ReturnToLocomotionState));

            stateMachine.SetState(locomotionState);
        }

        private bool ReturnToLocomotionState() => isGrounded && !jumpTimer.IsRunning;

        private bool ReturnToAir() => !isGrounded && !jumpTimer.IsRunning;

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

        void At(IState from, IState to, IPredicate condition) =>
            stateMachine.AddTransition(from, to, condition);
        void Any(IState to, IPredicate condition) =>
            stateMachine.AddAnyTransition(to, condition);

        #region Collisions

        private void CheckCollisions()
        {
            Physics2D.queriesStartInColliders = false;

            bool groundHit = Physics2D.BoxCast(
                col.bounds.center, col.bounds.size, 0f,
                Vector2.down, groundCheckDistance, groundLayer);
            bool ceilHit = Physics2D.BoxCast(
                col.bounds.center, col.bounds.size, 0f,
                Vector2.up, ceilCheckDistance, groundLayer);

            // 撞天花板，立即停止跳跃计时并清零上升速度
            if (!isCeil && ceilHit)
            {
                isCeil = true;
                jumpTimer.Stop();
                jumpVelocity = 0f;
            }
            else if (isCeil && !ceilHit)
            {
                isCeil = false;
            }

            // 落地
            if (!isGrounded && groundHit)
            {
                isGrounded = true;
                endedJumpEarly = false; // 落地后重置
                rb.linearVelocityY = Mathf.Max(rb.linearVelocityY, 0f);
            }
            // 离地：启动 coyote 计时
            else if (isGrounded && !groundHit)
            {
                isGrounded = false;
                jumpCoyoteTimer.Start();
            }

            Physics2D.queriesStartInColliders = colliderCache;
        }

        #endregion

        #region Actions

        // 修复：Buffer 消费移出 CheckCollisions，在 FixedUpdate 开头单独调用
        private void TryConsumeJumpBuffer()
        {
            if (jumpBufferTimer.IsRunning && isGrounded && !jumpTimer.IsRunning)
            {
                ExecuteJump();
                jumpBufferTimer.Stop();
            }
        }

        public void HandleMovement()
        {
            rb.linearVelocityX = movement.x * runSpeed;
        }

        public void HandleJump()
        {
            // 落地且不在跳跃中：归零并交还控制权
            if (!jumpTimer.IsRunning && isGrounded)
            {
                jumpVelocity = 0f;
                return;
            }

            if (!jumpTimer.IsRunning || endedJumpEarly || rb.linearVelocityY < 0f)
            {
                // 计算本帧重力增量
                float gravity = Physics2D.gravity.y * gravityMultiplier;

                if (endedJumpEarly && jumpVelocity > 0f)
                    gravity *= jumpEndEarlyGravityModifier;

                jumpVelocity += gravity * Time.fixedDeltaTime;
            }
            // jumpTimer 运行中且未早结束：保持 ExecuteJump 赋的初速，不累加重力（jumpTimer 到期后自然走上面分支开始下落）

            rb.linearVelocityY = jumpVelocity;
        }

        private void OnJump(bool performed)
        {
            if (performed)
            {
                jumpBufferTimer.Start();
                TryJump();
            }
            else if (jumpTimer.IsRunning)
            {
                endedJumpEarly = true;
                jumpTimer.Stop();
            }
        }

        private void TryJump()
        {
            bool canCoyote = jumpCoyoteTimer.IsRunning && !isGrounded && !jumpTimer.IsRunning;

            if (canCoyote || isGrounded)
            {
                ExecuteJump();
                jumpBufferTimer.Stop();
            }
        }
        
        private void ExecuteJump()
        {
            endedJumpEarly = false;
            jumpVelocity = jumpForce;
            jumpTimer.Start();
            jumpCoyoteTimer.Stop();
        }

        #endregion
    }
}