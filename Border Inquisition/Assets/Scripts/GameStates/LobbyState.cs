using Gameplay;

namespace GameStates
{
    public enum LobbyResult
    {
        StartMatch,
        Back
    }

    public class LobbyState : SceneState
    {
        protected override string SceneName => "Lobby";

        public LobbyResult Result { get; private set; }
        public MatchSettings Settings { get; private set; }

        public void StartMatch(MatchSettings settings)
        {
            if (!IsSceneLoaded) return;

            Settings = settings;
            Result = LobbyResult.StartMatch;
            StateMachine.OnCompleted(this);
        }

        public void Back()
        {
            if (!IsSceneLoaded) return;

            Result = LobbyResult.Back;
            StateMachine.OnCompleted(this);
        }
    }
}
