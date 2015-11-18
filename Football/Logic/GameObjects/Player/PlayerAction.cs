using Football.Logic.ArtificialIntelligence;
using Football.Logic.GameObjects.Player;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Football.Logic
{
    public enum ActionType
    { 
        Shoot, 
        Run,
        Pass, 
        Tackle,
        Throw,
        Nothing
    };

    class PlayerAction
    {
        public bool IsPreviewAction { get; set; }
        public bool IsTemporary { get; set; }
        public bool IsActionToGetBall { get; set; }
        public Player AffectedPlayer { get; set; }
        public Point TargetPoint { get; set; }
        public ActionType Type { get; set; }
        public Queue<Point> WayToTarget { get; set; }
        public bool TargetPointChanged { get; set; }

        public PlayerAction(Player player, Point targetPoint, ActionType actionType)
        {
            AffectedPlayer = player;
            this.TargetPoint = targetPoint;
            WayToTarget = new Queue<Point>();
            Type = actionType;
        }

        /* If the player wants to tackle another player, the new target point will be the location the tackled player will
         * have in the amount of rounds which this player needs to get to the current position of the players he wants to tackle. */
        public void UpdateTargetPoint(Point currentLocation, int speed)
        {
            if(Type == ActionType.Tackle && AffectedPlayer != null)
            {
                Pathfinding distanceCalcer = new Pathfinding();
                ActionType type;
                int distance = (int) Pathfinding.DistanceBetweenPoints(currentLocation, AffectedPlayer.Location);
                Point target = distanceCalcer.PlayerAtSpecificRound(AffectedPlayer, distance / speed, out type);
                TargetPointChanged = target != TargetPoint;
                TargetPoint = target;
            }
        }
    }
}
