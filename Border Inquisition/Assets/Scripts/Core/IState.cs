namespace Core
{
    public interface IState
    {
        StateMachine StateMachine { get; set; }
        void OnEnter();
        void OnExit();
        void OnUpdate();
    }
}