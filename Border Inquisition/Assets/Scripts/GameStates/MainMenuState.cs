namespace GameStates
{
    public enum MainMenuResult
    {
        Play,
        Quit
    }

    public class MainMenuState : SceneState
    {
        protected override string SceneName => "MainMenu";

        public MainMenuResult Result { get; private set; }

        public void Finish(MainMenuResult result)
        {
            if (!IsSceneLoaded) return;

            Result = result;
            StateMachine.OnCompleted(this);
        }
    }
}
