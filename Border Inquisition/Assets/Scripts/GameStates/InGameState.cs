using System;
using System.Collections.Generic;
using Gameplay.Managers;
using IState = Gameplay.Helpers.IState;
using StateMachine = Gameplay.Helpers.StateMachine;

namespace GameStates
{
    public class InGameState : IState
    {
        private StateMachine _inGameStateMachine;
        private FirstPhase _firstPhase;
        private SecondPhase _secondPhase;
        private ThirdPhase _thirdPhase;

        public StateMachine StateMachine { get; set; }

        public void OnEnter()
        {
            _inGameStateMachine = new StateMachine();
            _firstPhase = new FirstPhase();
            _secondPhase = new SecondPhase();
            _thirdPhase = new ThirdPhase();
            
            _inGameStateMachine.ChangeState(_firstPhase);
            _inGameStateMachine.Completed += NextPhase;
        }

        private void NextPhase(IState obj)
        {
            switch (obj)
            {
                case  FirstPhase:
                    _inGameStateMachine.ChangeState(_secondPhase);
                    break;
                case  SecondPhase:
                    _inGameStateMachine.ChangeState(_thirdPhase);
                    break;
                case  ThirdPhase:
                    _inGameStateMachine.ChangeState(_firstPhase);
                    break;
            }
        }
        
        public void OnExit()
        {
        }

        public void OnUpdate()
        {
            _inGameStateMachine.Update();
        }

        private class FirstPhase : IState
        {
            public StateMachine StateMachine { get; set; }

            public void OnEnter()
            {
                GameController.Instance.PhaseOne();
                // StateMachine.OnCompleted(this);
            }

            public void OnExit()
            {
            }

            public void OnUpdate()
            {
            }
        }

        private class SecondPhase : IState
        {
            public StateMachine StateMachine { get; set; }

            public void OnEnter()
            {
            }

            public void OnExit()
            {
            }

            public void OnUpdate()
            {
            }
        }

        private class ThirdPhase : IState
        {
            public StateMachine StateMachine { get; set; }

            public void OnEnter()
            {
            }

            public void OnExit()
            {
            }

            public void OnUpdate()
            {
            }
        }
    }
}