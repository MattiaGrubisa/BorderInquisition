using Gameplay.Helpers;

namespace GameStates
{
    public class MainMenuState : IState
    {
        private StateMachine _mainMenuStateMachine;
        private MenuState _menuState;
        private OptionsState _optionsState;

        public StateMachine StateMachine { get; set; }

        public void OnEnter()
        {
            _mainMenuStateMachine = new StateMachine();
            _menuState = new MenuState();
            _optionsState = new OptionsState();
        }

        public void OnExit()
        {
            throw new System.NotImplementedException();
        }

        public void OnUpdate()
        {
            throw new System.NotImplementedException();
        }

        private class MenuState : IState
        {
            public StateMachine StateMachine { get; set; }

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

        private class OptionsState : IState
        {
            public StateMachine StateMachine { get; set; }

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