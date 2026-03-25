using System;
using UnityEngine;

public class PlayerRunState : PlayerGroundState
{
    public PlayerRunState(PlayerController playerController, PlayerStateMachine playerStateMachine, string animName) : base(playerController, playerStateMachine, animName)
    {
    }
    
    public override void LogicalUpdate()
    {
        base.LogicalUpdate();
        
        CheckMovement();
    }

    public override void PhysicsUpdate()
    {
        ApplyStandardMovement(_playerController.runSpeed);
    }
    
    
    private void CheckMovement()
    {

        if (_playerController.inputDirection.x == 0f)
        {
            _playerStateMachine.ChangeState(_playerController.idleState);
        }
    }
    
}
