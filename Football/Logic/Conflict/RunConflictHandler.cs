using Football.Logic.Conflicts;
using Football.Logic.GameObjects.Player;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Football.Logic
{
    class RunConflictHandler
    {
        /* Returns a list containing all run-conflicts of this round. */
        public List<RunConflict> SearchRunConflicts(Player[] allPlayers)
        {
            List<LastPoint> lastPoints = new List<LastPoint>();
            List<RunConflict> conflicts = new List<RunConflict>();
            foreach (Player player in allPlayers)
            {
                if (player.PlayerActions.Count > 0 && player.PlayerActions.Peek().Type == ActionType.Run && player.PlayerActions.Peek().WayToTarget.Count > 0)
                {
                    LastPoint lastPoint = new LastPoint(player, GetAffectedPoint(player));
                    Player conflictPlayer = SearchRunConflictPlayer(lastPoints, lastPoint.TargetPoint);
                    
                    if (conflictPlayer != null)
                    {
                        RunConflict conflict = SearchRunConflict(conflicts, lastPoint.TargetPoint);
                        conflict.InvolvedPlayers.Add(player);
                        if (!conflict.InvolvedPlayers.Contains(conflictPlayer))
                        {
                            conflict.InvolvedPlayers.Add(conflictPlayer);
                        }
                    }
                    lastPoints.Add(lastPoint);
                }
            }

            return conflicts;
        }

        /* Returns the last point where the player will go to in the next round.  */
        private Point GetAffectedPoint(Player player)
        {
            Pathfinding pathfinding = new Pathfinding();
            ActionType type;
            return pathfinding.PlayerAtSpecificRound(player, 1, out type);
        }

        /* Searches the second player who will go to this point. */
        private Player SearchRunConflictPlayer(List<LastPoint> lastPoints, Point point)
        {
            foreach (LastPoint lastPoint in lastPoints)
            {
                if (lastPoint.TargetPoint.Equals(point))
                {
                    return lastPoint.AffectedPlayer;
                }
            }

            return null;
        }

        /* Returns an empty conflict if there is no conflict with this conflict-point. */
        private RunConflict SearchRunConflict(List<RunConflict> conflicts, Point point)
        {
            foreach (RunConflict conflict in conflicts)
            {
                if (conflict.ConflictPoint.Equals(point))
                {
                    return conflict;
                }
                
            }
            RunConflict emptyConflict = new RunConflict(point);
            conflicts.Add(emptyConflict);
            return emptyConflict;
        }

        /* Checks whether the conflict list contains this player. */
        public bool ContainsPlayer(List<RunConflict> conflicts, Player searchPlayer)
        {
            foreach (RunConflict conflict in conflicts)
            {
                if (conflict.InvolvedPlayers.Contains(searchPlayer))
                {
                    return true;
                }
            }

            return false;
        }

        /* Returns the winner of this run conflict. */
        public Player RunConflictWinner(RunConflict conflict)
        {
            Player strongestPlayer = null;
            double bestConflictStrength = 0;
            Random rnd = new Random();
            foreach (Player involvedPlayer in conflict.InvolvedPlayers)
            {
                double tmpConflictStrength = involvedPlayer.ConflictStrength(conflict.ConflictType);
                if (strongestPlayer == null || tmpConflictStrength > bestConflictStrength)
                {
                    bestConflictStrength = tmpConflictStrength;
                    strongestPlayer = involvedPlayer;
                }
            }

            return strongestPlayer;
        }

    }
}
