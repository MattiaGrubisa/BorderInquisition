namespace Gameplay.Helpers
{
    public interface IState
    {
        StateMachine StateMachine { get; set; }
        void OnEnter();
        void OnExit();
        void OnUpdate();
    }
}