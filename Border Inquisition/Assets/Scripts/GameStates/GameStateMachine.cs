using System;
using Gameplay;
using Gameplay.Helpers;
using UnityEngine;

namespace GameStates
{
    // Lives in the persistent Bootstrap scene; every other scene is loaded additively by a state.
    public class GameStateMachine : Singleton<GameStateMachine>
    {
        private StateMachine _gameStateMachine;
        private MainMenuState _mainMenuState;
        private LobbyState _lobbyState;
        private InGameState _inGameState;
        private GameOverState _gameOverState;

        protected override void Awake()
        {
            base.Awake();
            _gameStateMachine = new StateMachine();
            _mainMenuState = new MainMenuState();
            _lobbyState = new LobbyState();
            _inGameState = new InGameState();
            _gameOverState = new GameOverState();

            _gameStateMachine.Completed += OnStateCompleted;
            _gameStateMachine.ChangeState(_mainMenuState);
        }

        private void Update()
        {
            _gameStateMachine.Update();
        }

        private void OnStateCompleted(IState state)
        {
            switch (state)
            {
                case MainMenuState when _mainMenuState.Result == MainMenuResult.Quit:
                    QuitApplication();
                    break;
                case MainMenuState:
                    _gameStateMachine.ChangeState(_lobbyState);
                    break;
                case LobbyState when _lobbyState.Result == LobbyResult.Back:
                    _gameStateMachine.ChangeState(_mainMenuState);
                    break;
                case LobbyState:
                    _inGameState.Settings = _lobbyState.Settings;
                    _gameStateMachine.ChangeState(_inGameState);
                    break;
                case InGameState:
                    _gameOverState.Winner = _inGameState.Winner;
                    _gameStateMachine.ChangeState(_gameOverState);
                    break;
                case GameOverState when _gameOverState.Result == GameOverResult.Rematch:
                    _gameStateMachine.ChangeState(_lobbyState);
                    break;
                case GameOverState:
                    _gameStateMachine.ChangeState(_mainMenuState);
                    break;
            }
        }

        private static void QuitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #region UI commands

        // UI calls these without knowing which state is active; an inactive state ignores the call.
        public void Play() => _mainMenuState.Finish(MainMenuResult.Play);
        public void Quit() => _mainMenuState.Finish(MainMenuResult.Quit);
        public void StartMatch(int playerCount) => _lobbyState.StartMatch(new MatchSettings(playerCount));
        public void LeaveLobby() => _lobbyState.Back();
        public void EndPhase() => _inGameState.EndPhase();
        public void Rematch() => _gameOverState.Finish(GameOverResult.Rematch);
        public void ReturnToMainMenu() => _gameOverState.Finish(GameOverResult.MainMenu);

        #endregion

        #region Queries

        public event Action<TurnPhase> PhaseChanged
        {
            add => _inGameState.PhaseChanged += value;
            remove => _inGameState.PhaseChanged -= value;
        }

        public TurnPhase CurrentPhase => _inGameState.CurrentPhase;
        public Player Winner => _gameOverState.Winner;

        #endregion
    }
}
