public abstract class StateMachine
{
    public State _currentState;

    public void EnterState(State newState)
    {
        _currentState =  newState;
        _currentState.OnEnter();
    }

    public void ChangeState(State newState)
    {
        _currentState.OnExit();
        _currentState = newState;
        _currentState.OnEnter();
    }
}
