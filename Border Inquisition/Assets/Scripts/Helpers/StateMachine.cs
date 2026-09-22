using System;

namespace Gameplay.Helpers
{
    public class StateMachine
    {
        private IState _currentState;
        private IState _pendingState;
        private bool _isBusy;

        public event Action<IState> Completed;
        public event Action<IState> StateChanged;

        public IState CurrentState => _currentState;

        public void Update()
        {
            if (_currentState == null) return;

            RunBusy(_currentState.OnUpdate);
            ApplyPendingState();
        }

        // Requested while a state is entering or updating, the change is deferred until that call
        // returns, so states may complete from OnEnter/OnUpdate without re-entering ChangeState.
        public void ChangeState(IState state)
        {
            _pendingState = state;
            if (!_isBusy)
                ApplyPendingState();
        }

        // Exits the current state without entering another; a parent calls this from its own OnExit.
        public void Stop()
        {
            _pendingState = null;
            if (_currentState == null) return;

            RunBusy(_currentState.OnExit);
            _currentState = null;
        }

        public void OnCompleted(IState state) => Completed?.Invoke(state);

        private void ApplyPendingState()
        {
            while (_pendingState != null)
            {
                var next = _pendingState;
                _pendingState = null;
                if (next == _currentState) continue;

                RunBusy(() =>
                {
                    _currentState?.OnExit();
                    _currentState = next;
                    _currentState.StateMachine = this;
                    _currentState.OnEnter();
                    StateChanged?.Invoke(_currentState);
                });
            }
        }

        private void RunBusy(Action action)
        {
            _isBusy = true;
            try
            {
                action();
            }
            finally
            {
                _isBusy = false;
            }
        }
    }
}
