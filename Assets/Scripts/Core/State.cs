public abstract class State
{ 
    public virtual void OnEnter() { }
    public virtual void OnExit() { }
    public virtual void LogicalUpdate() { }
    public virtual void PhysicsUpdate() { }
}
