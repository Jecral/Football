
using Football.Logic.GameObjects.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Football.Logic.Conflicts
{
    class TackleConflictHandler
    {
        /* Returns a list with all players who will tackle in this round. */
        public List<Player> SearchTacklePlayers(Player[] allPlayers)
        {
            List<Player> tacklePlayers = new List<Player>();
            foreach (Player player in allPlayers)
            {
                ActionType type;
                Pathfinding pathfinding = new Pathfinding();
                //returns the last action type which the player will execute in this round.
                pathfinding.PlayerAtSpecificRound(player, 1, out type);
                if (type == ActionType.Tackle)
                {
                    tacklePlayers.Add(player);
                }
            }
            return tacklePlayers;
        }

        /* Returns the winner player of this tackle conflict */
        public Player TackleConflictWinner(Player tacklePlayer, Player tackledPlayer)
        {
            double tackleStrength = tacklePlayer.ConflictStrength(ActionType.Tackle);
            double tackledStrength = tackledPlayer.ConflictStrength(ActionType.Tackle);

            return (tackleStrength > tackledStrength) ? tacklePlayer : tackledPlayer;
        }
    }
}
