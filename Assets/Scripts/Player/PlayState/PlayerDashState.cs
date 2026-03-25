using UnityEngine;

public class PlayerDashState : PlayerState
{
    private Vector2 _dashDirection;
    private float _dashDuration;
    private float _dashDurationTimer;
    
    
    public PlayerDashState(PlayerController playerController, PlayerStateMachine playerStateMachine, string animName) : base(playerController, playerStateMachine, animName)
    {
    }

    public override void OnEnter()
    {
        base.OnEnter();
        
        _playerController.dashTrigger = false;
        _dashDirection = _playerController.inputDirection.normalized;
        _playerController.rb.gravityScale = 0;
        _dashDuration = 0.25f;
        _dashDurationTimer = _dashDuration;
        
        if (_dashDirection == Vector2.zero)
        {
            _dashDirection = _playerController.faceDirection * Vector2.right;
        }
        
        _playerController.rb.linearVelocity = _playerController.dashSpeed * _dashDirection;
    }
    
    public override void OnExit()
    {
        base.OnExit();
        
        _playerController.rb.linearVelocity = Vector3.zero;
        _playerController.rb.gravityScale = 1;
        _playerController.jumpTrigger = false;
    }

    public override void LogicalUpdate()
    {
        base.LogicalUpdate();

        if (_dashDurationTimer > 0.0f)
        {
            _dashDurationTimer -= Time.deltaTime;
        }
        else
        {
            if (_playerController.isGrounded)
            {
                _playerStateMachine.ChangeState(_playerController.idleState);
            }
            else
            {
                _playerStateMachine.ChangeState(_playerController.airState);
            }
            
        }
        
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        
        _playerController.rb.linearVelocity = _playerController.dashSpeed * _dashDirection;
    }
}
