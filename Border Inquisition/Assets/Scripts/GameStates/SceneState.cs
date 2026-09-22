using Gameplay.Helpers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameStates
{
    // An outer game state that owns one additively loaded scene for as long as it is active.
    // Subclasses hook into the scene's lifetime instead of the raw IState callbacks.
    public abstract class SceneState : IState
    {
        private AsyncOperation _loading;

        public StateMachine StateMachine { get; set; }

        protected abstract string SceneName { get; }

        // False until the scene has loaded and again after exit; UI commands check it so that
        // a click reaching an inactive state is ignored.
        protected bool IsSceneLoaded { get; private set; }

        public void OnEnter()
        {
            IsSceneLoaded = false;
            _loading = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Additive);
        }

        public void OnUpdate()
        {
            if (IsSceneLoaded)
            {
                OnSceneUpdate();
                return;
            }

            if (_loading == null || !_loading.isDone) return;

            IsSceneLoaded = true;
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(SceneName));
            OnSceneLoaded();
        }

        public void OnExit()
        {
            if (IsSceneLoaded)
            {
                IsSceneLoaded = false;
                OnSceneExit();
                SceneManager.UnloadSceneAsync(SceneName);
            }
            else if (_loading != null)
            {
                _loading.completed += _ => SceneManager.UnloadSceneAsync(SceneName);
            }
        }

        protected virtual void OnSceneLoaded() { }
        protected virtual void OnSceneUpdate() { }
        protected virtual void OnSceneExit() { }
    }
}
