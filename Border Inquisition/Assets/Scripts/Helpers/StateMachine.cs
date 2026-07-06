using System.Collections.Generic;

namespace StateMachine
{
    public class StateMachine
    {
        private IState _currentState;
        private List<IState> _states;

        public void Update()
        {
            _currentState?.OnUpdate();
        }

        public void ChangeState(IState state)
        {
            if(state == _currentState) return;
            
            _currentState?.OnExit();
            _currentState = state;
            _currentState?.OnEnter();
        }
    }
}