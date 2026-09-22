using Gameplay;

namespace GameStates
{
    public enum GameOverResult
    {
        MainMenu,
        Rematch
    }

    public class GameOverState : SceneState
    {
        protected override string SceneName => "GameOver";

        public Player Winner { get; set; }
        public GameOverResult Result { get; private set; }

        public void Finish(GameOverResult result)
        {
            if (!IsSceneLoaded) return;

            Result = result;
            StateMachine.OnCompleted(this);
        }
    }
}
