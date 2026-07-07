using Gameplay.Helpers;

namespace GameStates
{
    public class InGameState : IState
    {
        private StateMachine _inGameStateMachine;
        private FirstPhase _firstPhase;
        private SecondPhase _secondPhase;
        private ThirdPhase _thirdPhase;
        
        public void OnEnter()
        {
            _inGameStateMachine = new StateMachine();
            _firstPhase = new FirstPhase();
            _secondPhase = new SecondPhase();
            _thirdPhase = new ThirdPhase();
            
            _inGameStateMachine.ChangeState(_firstPhase);
        }

        public void OnExit()
        {
            throw new System.NotImplementedException();
        }

        public void OnUpdate()
        {
            throw new System.NotImplementedException();
        }

        private class FirstPhase : IState
        {
            public void OnEnter()
            {
                throw new System.NotImplementedException();
            }

            public void OnExit()
            {
                throw new System.NotImplementedException();
            }

            public void OnUpdate()
            {
                throw new System.NotImplementedException();
            }
        }

        private class SecondPhase : IState
        {
            public void OnEnter()
            {
                throw new System.NotImplementedException();
            }

            public void OnExit()
            {
                throw new System.NotImplementedException();
            }

            public void OnUpdate()
            {
                throw new System.NotImplementedException();
            }
        }

        private class ThirdPhase : IState
        {
            public void OnEnter()
            {
                throw new System.NotImplementedException();
            }

            public void OnExit()
            {
                throw new System.NotImplementedException();
            }

            public void OnUpdate()
            {
                throw new System.NotImplementedException();
            }
        }
    }
}