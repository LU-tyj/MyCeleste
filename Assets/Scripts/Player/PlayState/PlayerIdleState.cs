using System;
using UnityEngine;

public class PlayerIdleState : PlayerGroundState
{
    public PlayerIdleState(PlayerController playerController, PlayerStateMachine playerStateMachine, string animName) : base(playerController, playerStateMachine, animName)
    {
    }

    public override void LogicalUpdate()
    {
        base.LogicalUpdate();
        
        CheckMovement();
    }
    
    public override void PhysicsUpdate()
    {
        ApplyStandardMovement();
    }
    
    private void CheckMovement()
    {
        if (Math.Abs(_playerController.inputDirection.x) >= 0.5)
        {
            _playerStateMachine.ChangeState(_playerController.runState);
        }
    }
}
