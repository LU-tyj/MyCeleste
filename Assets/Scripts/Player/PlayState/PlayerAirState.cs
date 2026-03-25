public class PlayerAirState : PlayerState
{
    public PlayerAirState(PlayerController playerController, PlayerStateMachine playerStateMachine, string animName) : base(playerController, playerStateMachine, animName)
    {
    }

    public override void LogicalUpdate()
    {
        base.LogicalUpdate();

        if (_playerController.isGrounded)
        {
            _playerStateMachine.ChangeState(_playerController.idleState);
        }
    }
    
    public override void PhysicsUpdate()
    {
        ApplyStandardMovement(_playerController.airSpeed);
    }
}
