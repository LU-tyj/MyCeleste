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

        private List<Timer> timers;
        private CountdownTimer jumpTimer;
        private CountdownTimer jumpBufferTimer;
        private CountdownTimer jumpCoyoteTimer;

        [Header("PlayerMovement")]
        [SerializeField] private Vector2 movement;

        private StateMachine stateMachine;
        private Vector2 playerVelocity;

        private bool isGrounded;
        private bool isCeil;

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
            movement = input.Direction;
            stateMachine.Update();
            HandleTimers();
            UpdateAnimator();
        }

        private void FixedUpdate()
        {
            physicCheck.CheckCollisions();

            isGrounded = physicCheck.IsGrounded;
            isCeil     = physicCheck.IsCeil;

            TryConsumeJumpBuffer();
            stateMachine.FixedUpdate();
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
        #endregion

        #region Jump
        private void TryConsumeJumpBuffer()
        {
            if (jumpBufferTimer.IsRunning && isGrounded && !jumpTimer.IsRunning)
            {
                ExecuteJump();
                jumpBufferTimer.Stop();
            }
        }

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
                TryJump();
            }
            else if (jumpTimer.IsRunning)
            {
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
            playerVelocity.y = movementStats.InitialJumpVelocity;
            jumpTimer.Start();
            jumpCoyoteTimer.Stop();
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