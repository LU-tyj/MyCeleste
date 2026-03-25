using System;
using UnityEngine;

public class PlayerWalkState : PlayerGroundState
{
    public PlayerWalkState(PlayerController playerController, PlayerStateMachine playerStateMachine, string animName) : base(playerController, playerStateMachine, animName)
    {
    }
    
    public override void LogicalUpdate()
    {
        base.LogicalUpdate();
        
        CheckMovement();
    }

    public override void PhysicsUpdate()
    {
        ApplyStandardMovement(_playerController.moveSpeed);
    }
    
    
    private void CheckMovement()
    {

        if (_playerController.inputDirection.x == 0f)
        {
            _playerStateMachine.ChangeState(_playerController.idleState);
        }

        if (Math.Abs(_playerController.inputDirection.x) > 0.5f)
        {
            _playerStateMachine.ChangeState(_playerController.runState);
        }
    }
}
