using System;
using Gameplay;
using Gameplay.Managers;
using Save;
using IState = Gameplay.Helpers.IState;
using StateMachine = Gameplay.Helpers.StateMachine;

namespace GameStates
{
    public enum TurnPhase
    {
        Income,
        Attack,
        BuildAndMove
    }

    public enum InGameResult
    {
        GameOver,
        Left
    }

    public class InGameState : SceneState
    {
        private readonly FirstPhase _firstPhase = new FirstPhase();
        private readonly SecondPhase _secondPhase = new SecondPhase();
        private readonly ThirdPhase _thirdPhase = new ThirdPhase();
        private StateMachine _phaseMachine;

        public event Action<TurnPhase> PhaseChanged;

        public MatchSettings Settings { get; set; }
        // A saved match to pick up instead of starting a new one from Settings.
        public MatchSave Resume { get; set; }
        public Player Winner { get; private set; }
        public TurnPhase CurrentPhase { get; private set; }
        public InGameResult Result { get; private set; }

        protected override string SceneName => "WorldMap";

        protected override void OnSceneLoaded()
        {
            Winner = null;
            Result = InGameResult.GameOver;
            GameController.Instance.MatchWon += OnMatchWon;
            GameController.Instance.MatchLost += OnMatchLost;

            _phaseMachine = new StateMachine();
            _phaseMachine.Completed += NextPhase;
            _phaseMachine.StateChanged += OnPhaseChanged;

            if (Resume == null)
            {
                GameController.Instance.StartNewMatch(Settings);
                _phaseMachine.ChangeState(_firstPhase);
            }
            else if (MatchSnapshot.TryRestore(GameController.Instance, Resume))
            {
                Resume = null;
                _phaseMachine.ChangeState(_secondPhase);
            }
            else
            {
                Resume = null;
                SaveSystem.Delete();
                Result = InGameResult.Left;
                StateMachine.OnCompleted(this);
            }
        }

        protected override void OnSceneUpdate() => _phaseMachine.Update();

        protected override void OnSceneExit()
        {
            _phaseMachine.Stop();
            _phaseMachine.Completed -= NextPhase;
            _phaseMachine.StateChanged -= OnPhaseChanged;
            GameController.Instance.MatchWon -= OnMatchWon;
            GameController.Instance.MatchLost -= OnMatchLost;
        }

        public void EndPhase()
        {
            if (IsSceneLoaded)
                (_phaseMachine.CurrentState as Phase)?.End();
        }

        // Leaves the match from the pause menu; the save from the start of this turn stays for Continue.
        public void Leave()
        {
            if (!IsSceneLoaded)
                return;

            Result = InGameResult.Left;
            StateMachine.OnCompleted(this);
        }

        private void NextPhase(IState phase)
        {
            switch (phase)
            {
                case FirstPhase:
                    if (!GameController.Instance.IsOver)
                        SaveSystem.Write(MatchSnapshot.Capture(GameController.Instance));
                    _phaseMachine.ChangeState(_secondPhase);
                    break;
                case SecondPhase:
                    _phaseMachine.ChangeState(_thirdPhase);
                    break;
                case ThirdPhase:
                    GameController.Instance.NextPlayer();
                    _phaseMachine.ChangeState(_firstPhase);
                    break;
            }
        }

        private void OnPhaseChanged(IState phase)
        {
            CurrentPhase = ((Phase)phase).Kind;
            PhaseChanged?.Invoke(CurrentPhase);
        }

        private void OnMatchWon(Player winner)
        {
            SaveSystem.Delete();
            Winner = winner;
            Result = InGameResult.GameOver;
            StateMachine.OnCompleted(this);
        }

        // No human is left; GameOver shows it without a winner.
        private void OnMatchLost()
        {
            SaveSystem.Delete();
            Winner = null;
            Result = InGameResult.GameOver;
            StateMachine.OnCompleted(this);
        }

        // A phase waits for End Phase unless it ends itself; the phase only runs its own logic.
        private abstract class Phase : IState
        {
            public StateMachine StateMachine { get; set; }
            public abstract TurnPhase Kind { get; }

            public virtual void OnEnter() { }
            public virtual void OnExit() { }
            public virtual void OnUpdate() { }

            public void End() => StateMachine.OnCompleted(this);
        }

        // Income is not a phase the player waits in: the queues are processed, the die is rolled and
        // paid out, and the turn moves straight on to the attack. The HUD shows the roll.
        private class FirstPhase : Phase
        {
            public override TurnPhase Kind => TurnPhase.Income;

            public override void OnEnter()
            {
                GameController.Instance.PhaseOne();
                End();
            }
        }

        private class SecondPhase : Phase
        {
            public override TurnPhase Kind => TurnPhase.Attack;
        }

        // Queue buildings and soldiers, trade, and make the turn's one move, in any order.
        private class ThirdPhase : Phase
        {
            public override TurnPhase Kind => TurnPhase.BuildAndMove;
        }
    }
}
