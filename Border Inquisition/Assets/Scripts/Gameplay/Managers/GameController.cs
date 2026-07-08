using Gameplay.Helpers;

namespace Gameplay.Managers
{
    public class GameController : Singleton<GameController>
    {
        private Player _player;
        
        protected override void Awake()
        {
            base.Awake();
        }
        
    }
}