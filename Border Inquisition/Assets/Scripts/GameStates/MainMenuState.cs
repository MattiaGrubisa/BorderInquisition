using Save;

namespace GameStates
{
    public enum MainMenuResult
    {
        Play,
        Continue,
        Quit
    }

    public class MainMenuState : SceneState
    {
        protected override string SceneName => "MainMenu";

        public MainMenuResult Result { get; private set; }

        // The match to pick up, when the result is Continue.
        public MatchSave Save { get; private set; }

        public void Finish(MainMenuResult result)
        {
            if (!IsSceneLoaded) return;

            Result = result;
            StateMachine.OnCompleted(this);
        }

        // Nothing happens when there is no save or it cannot be read.
        public void Continue()
        {
            if (!IsSceneLoaded || !SaveSystem.TryRead(out var save)) return;

            Save = save;
            Finish(MainMenuResult.Continue);
        }
    }
}
