using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Helpers
{
    public class StateMachine
    {
        private IState _currentState;

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