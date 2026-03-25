using UnityEngine;

public class PlayerState : State
{
    protected PlayerStateMachine _playerStateMachine;
    protected PlayerController _playerController;
    protected string _animName;
    
    public PlayerState(PlayerController playerController,  PlayerStateMachine playerStateMachine, string animName)
    {
        _playerController = playerController;
        _playerStateMachine = playerStateMachine;
        _animName = animName;
    }

    public virtual void OnEnter()
    {
        _playerController.anim.SetBool(_animName, true);
    }

    public virtual void OnExit()
    {
        _playerController.anim.SetBool(_animName, false);
    }

    public virtual void LogicalUpdate()
    {
        CheckWallGrab(); 
        CheckDash();
    }

    public virtual void PhysicsUpdate()
    {
        
    }
    
    protected virtual void ApplyStandardMovement(float speed = 0.0f)
    {
        float xDir = _playerController.inputDirection.normalized.x;
        float xVel = xDir * speed;
        _playerController.rb.linearVelocityX = xVel;
        
        // 实现图像翻转
        if (xDir > 0)
        {
            _playerController.transform.localScale = new Vector3(1, 1, 1);
        }
        else if (xDir < 0)
        {
            _playerController.transform.localScale = new Vector3(-1, 1, 1);
        }
    }

    private void CheckWallGrab()
    {
        if (_playerStateMachine.currentState == _playerController.wallState) return;
        if (_playerController.wallTrigger)
        {
            if (_playerController.isWall)
            {
                _playerStateMachine.ChangeState(_playerController.wallState);
                
            }
            else if  (_playerController.isHang)
            {
                _playerController.wallTrigger = false;
            }
        }
    }

    private void CheckDash()
    {
        if (_playerStateMachine.currentState == _playerController.dashState) return;
        if (_playerController.dashTrigger)
        { 
            _playerStateMachine.ChangeState(_playerController.dashState);
        }
    }
}