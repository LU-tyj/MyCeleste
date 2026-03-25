using UnityEngine;

public class PlayerGroundState : PlayerState
{
    public PlayerGroundState(PlayerController playerController, PlayerStateMachine playerStateMachine, string animName) : base(playerController, playerStateMachine, animName)
    {
    }

    public override void LogicalUpdate()
    {
        base.LogicalUpdate();
        
        Check();
    }
    
    // 判断是否在空中
    private void Check()
    {
        if (_playerController.jumpTrigger)
        {
            if (_playerController.isGrounded)
            {
                _playerStateMachine.ChangeState(_playerController.jumpState);
            }
            else
            {
                _playerController.jumpTrigger = false;
            }
        }
        if (!_playerController.isGrounded && _playerController.rb.linearVelocityY <= -0.1f)
        {
            _playerStateMachine.ChangeState(_playerController.airState);
        }
    }
    
    protected override void ApplyStandardMovement(float speed = 0.0f)
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
}