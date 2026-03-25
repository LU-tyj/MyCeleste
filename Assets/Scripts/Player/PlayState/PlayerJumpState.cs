using UnityEngine;

public class PlayerJumpState : PlayerState
{
    public PlayerJumpState(PlayerController playerController, PlayerStateMachine playerStateMachine, string animName) : base(playerController, playerStateMachine, animName)
    {
    }

    public override void OnEnter()
    {
        base.OnEnter();
        _playerController.jumpTrigger = false;
        Jump();
        
    }

    public override void LogicalUpdate()
    {
        base.LogicalUpdate();
        if (_playerController.rb.linearVelocityY <= -0.1f)
        {
            _playerStateMachine.ChangeState(_playerController.airState);
            _playerController.jumpTrigger = false;
        }
    }
    
    public override void PhysicsUpdate()
    {
        ApplyStandardMovement(_playerController.airSpeed);
    }

    private void Jump()
    {
        _playerController.rb.AddForce(Vector3.up * _playerController.jumpForce, (ForceMode2D)ForceMode.Impulse);
    }
}
