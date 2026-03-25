using UnityEngine;

public class PlayerStateMachine : StateMachine
{
    public PlayerState currentState;

    public void InitialiseStateMachine(PlayerState playerState)
    {
        currentState = playerState;
        currentState.OnEnter();
    }

    public void ChangeState(PlayerState newState)
    {
        if (_currentState == newState)
            return;
        currentState.OnExit();
        currentState = newState;
        currentState.OnEnter();
    }
}
