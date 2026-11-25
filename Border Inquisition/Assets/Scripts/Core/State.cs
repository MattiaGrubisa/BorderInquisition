namespace Core
{
    public abstract class State
    {
        public StateMachine Machine { get; internal set; }
        public virtual void OnEnter() {}
        public virtual void OnExit() {}
        public virtual void OnUpdate() {}

        protected void Complete() => Machine?.OnCompleted(this);
    }
}