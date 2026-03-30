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
        [SerializeField] private float jumpMaxHeight = 2f;
        [SerializeField] private float gravityMultiplier = 3f;
        [SerializeField] private float jumpCoyoteDuration = 0.2f;
        [SerializeField] private float jumpBufferDuration = 0.1f;
        [SerializeField] private bool jumpHeld;
        [SerializeField] private bool jumpToConsume;
        
        
        private List<Timer> timers;
        private CountdownTimer jumpTimer; // 记录跳跃时的时间
        private CountdownTimer jumpHeldTimer;
        private CountdownTimer jumpBufferTimer; // 记录按下跳跃键的时间
        private CountdownTimer jumpCoyoteTimer; // 记录离开地面后的时间
        
        [Header("Collision Settings")] 
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float ceilCheckDistance;
        [SerializeField] private float groundCheckDistance;
        [SerializeField]private bool isGrounded;
        private bool colliderCache; // 记录是否Physics2D.queriesStartInColliders的原始值，以便在检查碰撞后恢复
        
        [Header("PlayerMovement")]
        [SerializeField] private Vector2 movement;
        [SerializeField] private Vector2 playerVelocity;
        
        private StateMachine stateMachine;
        
        private float jumpVelocity;
        
        private void Awake()
        {
            col = GetComponent<BoxCollider2D>();
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            input = new InputReader();
            
            colliderCache = Physics2D.queriesStartInColliders;
            
            // Setup Timers
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
            stateMachine.FixedUpdate();
        }

        private void Start()
        {
            input.EnablePlayerActions();
        }

        private void OnEnable()
        {
            input.Jump += OnJump;
        }
        
        private void OnDisable()
        {
            input.Jump -= OnJump;
        }

        void SetTimers()
        {
            jumpTimer = new CountdownTimer(jumpDuration);
            jumpCoyoteTimer = new CountdownTimer(jumpCoyoteDuration);
            jumpBufferTimer = new CountdownTimer(jumpBufferDuration);
            
            jumpTimer.OnTimerStart += () => jumpVelocity = jumpForce;
            
            timers = new List<Timer>{jumpTimer, jumpCoyoteTimer, jumpBufferTimer};
        }
        
        void SetStateMachine()
        {
            stateMachine = new StateMachine();
            
            // Declare states
            var locomotionState = new LocomotionState(this, animator);
            var jumpState = new JumpState(this, animator);
            
            // Define transitions
            At(locomotionState, jumpState, new FuncPredicate(() => jumpTimer.IsRunning || ReturnToAir())); // 正在跳或者坠落
            Any(locomotionState, new FuncPredicate(ReturnToLocomotionState));
            
            stateMachine.SetState(locomotionState);
        }

        private bool ReturnToLocomotionState()
        {
            return isGrounded && !jumpTimer.IsRunning;
        }

        private bool ReturnToAir()
        {
            return !isGrounded && rb.linearVelocityY < 0;
        }
        
        private void HandleTimers()
        {
            foreach (var timer in timers)
            {
                timer.Tick(Time.deltaTime);
            }
        }

        private void UpdateAnimator()
        {
            animator.SetFloat("SpeedX", rb.linearVelocityX);
            animator.SetFloat("SpeedY", rb.linearVelocityY);
        }

        void At(IState from, IState to, IPredicate condition) => stateMachine.AddTransition(from, to, condition);
        void Any(IState to, IPredicate condition) => stateMachine.AddAnyTransition(to, condition);

        #region Collisions
        private void CheckCollisions()
        {
            Physics2D.queriesStartInColliders  = false;
            
            bool groundHit = Physics2D.BoxCast(
                col.bounds.center, col.bounds.size, 0f, Vector2.down, groundCheckDistance, groundLayer);
            bool ceilHit = Physics2D.BoxCast(
                col.bounds.center, col.bounds.size, 0f, Vector2.up, ceilCheckDistance, groundLayer);

            if (ceilHit) rb.linearVelocityY = Mathf.Min(rb.linearVelocityY, 0f);

            // Landed Ground
            if (!isGrounded && groundHit)
            {
                isGrounded = true;
                rb.linearVelocityY = Mathf.Max(rb.linearVelocityY, 0f);
            }
            // Left Ground
            else if (isGrounded && !groundHit)
            {
                isGrounded = false;
            }
            
                
            Physics2D.queriesStartInColliders = colliderCache;
        }
        

        #endregion

        #region Actions
        public void HandleMovement()
        {
            rb.linearVelocityX = movement.x * (runSpeed * Time.fixedDeltaTime);
        }

        public void HandleJump()
        {
            if (!jumpTimer.IsRunning && isGrounded)
            {
                jumpVelocity = 0.0f;
                return;
            }

            if (rb.linearVelocityY < 0.0f || !jumpTimer.IsRunning)
            {
                jumpVelocity += Physics2D.gravity.y * gravityMultiplier * Time.fixedDeltaTime;
            }

            if (jumpTimer.IsRunning)
            {
                jumpVelocity = jumpForce;
            }
            
            rb.linearVelocityY = jumpVelocity;
        }
        
        private void OnJump(bool performed)
        {
            if (performed && !jumpTimer.IsRunning && isGrounded)
            {
                jumpTimer.Start();
            }
            else if (!performed && jumpTimer.IsRunning)
            {
                jumpTimer.Stop();
            }
        }
        #endregion
        
    }   
}