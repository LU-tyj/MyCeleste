using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Components")]
    public Rigidbody2D rb;
    public Collider2D col;
    public Animator anim;
    public SpriteRenderer sr;
    public PhysicalCheck physicalCheck;
    public InputController inputController;

    public Vector2 inputDirection;
    public float faceDirection;
    
    [Header("Basic Settings")]
    public float moveSpeed;
    public float airSpeed;
    public float runSpeed;
    public float jumpForce;
    public float slideSpeed;
    public float climbSpeed;
    public float dashSpeed;
    public float dashCD;
    
    private float _dashTimer = 0.0f;
    
    private PlayerStateMachine playerStateMachine;
    public PlayerIdleState idleState; 
    public PlayerJumpState jumpState;
    public PlayerAirState airState;
    public PlayerRunState runState;
    public PlayerWallState wallState;
    public PlayerDashState dashState;

    [Header("State")]
    public bool jumpTrigger = false;
    public bool wallTrigger = false;
    public bool dashTrigger = false;
    public bool isGrounded;
    public bool isWall;
    public bool isHang;
    
    public PhysicsMaterial2D normalMaterial;
    public PhysicsMaterial2D wallMaterial;
    
    #region Unity Functions
    private void Awake()
    {
        // Components Reference
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        physicalCheck = GetComponent<PhysicalCheck>();
        inputController = new InputController();
        
        // Player States
        playerStateMachine = new PlayerStateMachine();
        idleState = new PlayerIdleState(this, playerStateMachine, "Idle");
        jumpState = new PlayerJumpState(this, playerStateMachine, "Jump");
        airState = new PlayerAirState(this, playerStateMachine, "Air");
        runState = new PlayerRunState(this, playerStateMachine, "Run");
        wallState = new PlayerWallState(this, playerStateMachine, "Wall");
        dashState = new PlayerDashState(this, playerStateMachine, "Dash");
        

        inputController.Gameplay.Jump.started += context => OnJumpStarted();
        inputController.Gameplay.GrabWall.started += context => OnGrabStarted();
        inputController.Gameplay.GrabWall.canceled += context => OnGrabCanceled();
        inputController.Gameplay.Dash.started += context => OnDashStarted();
    }

    private void Start()
    {
        playerStateMachine.InitialiseStateMachine(idleState);
    }
    
    private void OnEnable()
    {
        inputController.Enable();
    }

    private void OnDisable()
    {
        inputController.Disable();
    }

    private void Update()
    {
        inputDirection = inputController.Gameplay.Move.ReadValue<Vector2>();
        faceDirection = transform.localScale.x >= 0 ? 1 : -1;
        anim.SetFloat("yInput", inputDirection.y);
        
        BoolStateUpdate();
        CheckMaterial();
        DashCdCounter();
        
        playerStateMachine.currentState.LogicalUpdate();
    }

    private void FixedUpdate()
    {
        playerStateMachine.currentState.PhysicsUpdate();
    }
    #endregion

    #region Update State
    private void OnJumpStarted()
    {
        jumpTrigger = true;
    }

    private void OnGrabStarted()
    {
        wallTrigger = true;
    }
    
    private void OnGrabCanceled()
    {
        wallTrigger = false;
    }
    
    private void OnDashStarted()
    {
        if (_dashTimer > 0) return; 
        dashTrigger = true;
        _dashTimer = dashCD;

    }

    private void DashCdCounter()
    {
        if (_dashTimer <= 0) return;
        _dashTimer -= Time.deltaTime;
    }

    private void BoolStateUpdate()
    {
        IsGrounded();
        IsWall();
        IsHang();
    }
    
    private void IsGrounded()
    {
        isGrounded = physicalCheck.isGround;
    }

    private void IsWall()
    {
        isWall = physicalCheck.isWall;
    }

    private void IsHang()
    {
        isHang = physicalCheck.isHang;
    }
    #endregion

    private void CheckMaterial()
    {
        col.sharedMaterial = isGrounded ?  normalMaterial : wallMaterial;
    }

    public void HandleMovement()
    {
        
    }

    public void HandleJump()
    {
        
    }
    
}
