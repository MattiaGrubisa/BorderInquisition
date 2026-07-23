using System.Collections.Generic;
using Gameplay.Helpers;

namespace Gameplay.Managers
{
    public class GameController : Singleton<GameController>
    {
        private List<Player> _player;
        private Combat _combat;

        private void HandleAttack(Player player, Country from,  Country to)
        {
            var result = _combat.AttemptAttack(player, from, to);
            int attackerLosses = 0;
            int defenderLosses = 0;
            for (int i = 0; i < result.AttackerWins.Length; i++)
            {
                if (result.AttackerWins[i] == true)
                    defenderLosses++;
                else
                    attackerLosses++;
            }
            
            HandleLosses(attackerLosses, from, defenderLosses, to);
        }

        private void HandleLosses(int attackerLosses, Country from, int defenderLosses, Country to)
        {
            for (int i = 0; i < attackerLosses; i++)
            {
                
            }
        }
        
        protected override void Awake()
        {
            base.Awake();
        }
        
    }
}