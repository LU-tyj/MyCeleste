using UnityEngine;


public class PlayerWallState : PlayerState
{
    public PlayerWallState(PlayerController playerController, PlayerStateMachine playerStateMachine, string animName) : base(playerController, playerStateMachine, animName)
    {
    }

    public override void OnEnter()
    {
        base.OnEnter();
        
        _playerController.rb.gravityScale = 0;
        _playerController.rb.linearVelocity = Vector2.zero;
    }

    public override void OnExit()
    {
        base.OnExit();
        
        _playerController.anim.SetBool("Hang",  false);
        _playerController.rb.gravityScale = 1;
    }
    
    public override void LogicalUpdate()
    {
        base.LogicalUpdate();
        Check();
        
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        Climb();
    }

    private void Check()
    {
        // 离开墙
        if (_playerController.wallTrigger == false)
        {
            if (_playerController.isGrounded)
            {
                _playerStateMachine.ChangeState(_playerController.idleState);
            }
            else
            {
                _playerStateMachine.ChangeState(_playerController.airState);
            }
            return;
        }

        // 跳跃
        if (_playerController.jumpTrigger)
        {
            if (_playerController.isWall)
            {
                _playerController.wallTrigger = false;
                _playerStateMachine.ChangeState(_playerController.jumpState);
                return; 
            }
            else
            {
                _playerController.jumpTrigger = false;
            }
        }

        // Hang 控制
        if (_playerController.inputDirection.y == 0.0f && _playerController.isHang)
        {
            _playerController.anim.SetBool("Hang", true);
        }
        else
        {
            _playerController.anim.SetBool("Hang", false);
        }

        if (!_playerController.isWall &&  _playerController.isHang)
        {
            if (_playerController.isGrounded) _playerStateMachine.ChangeState(_playerController.idleState);
            else _playerStateMachine.ChangeState(_playerController.airState);
        }
    }

    private void Climb()
    {
        if (_playerController.inputDirection.y >= 0.1f)
        {
            _playerController.rb.linearVelocityY = _playerController.climbSpeed;
        }

        if (_playerController.inputDirection.y == 0.0f)
        {
            _playerController.rb.linearVelocityY = 0.0f;
        }
        
        if (_playerController.inputDirection.y < -0.1f)
        {
            _playerController.rb.linearVelocityY = _playerController.slideSpeed;
        }

        // 判断是否到顶，并自动爬上
        if (!_playerController.isWall && !_playerController.isHang)
        {
            Vector3 offset =
                new Vector3(0.2f * _playerController.faceDirection, 0.3f, 0.0f);

            Vector3 targetPos = _playerController.transform.position + offset;

            _playerController.transform.position = Vector3.Lerp(
                _playerController.transform.position,
                targetPos,
                _playerController.climbSpeed
            );
        }
    }
    
    
}
