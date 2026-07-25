using Gameplay.Helpers;
using Gameplay.Managers;

namespace GameStates
{
    public class GameStateMachine : Singleton<GameStateMachine>
    {
        private StateMachine _gameStateMachine;
        private MainMenuState _mainMenuState;
        private InGameState _inGameState;
        private GameOverState _gameOverState;
        private LobbyState _lobbyState;

        protected override void Awake()
        {
            base.Awake();
            _gameStateMachine = new StateMachine();
            _mainMenuState = new MainMenuState();
            _inGameState = new InGameState();
            _gameOverState = new GameOverState();
            _lobbyState = new LobbyState();
            
            _gameStateMachine.ChangeState(_mainMenuState);
        }

        private void Update()
        {
            _gameStateMachine.Update();
        }
    }
}