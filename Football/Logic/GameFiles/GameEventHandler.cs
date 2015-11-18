using Football.Logic.GameObjects.Player;
using Football.Logic.ArtificialIntelligence;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Football.Logic.GameFiles.Images;
using Football.GUI.General;
using Football.EventArguments;

namespace Football.Logic
{
    class GameEventHandler
    {
        public GameEventHandler(Game game)
        {
            currentGame = game;
            gameAI = currentGame.GameAI;
            currentGame.CurrentAction = game.CurrentAction;
            lastPlayersInOffside = new List<Player>();

            columns = currentGame.FieldCell.GetLength(0);
            rows = currentGame.FieldCell.GetLength(1);
        }

        private Game currentGame;
        private ArtificialIntelligence.AI gameAI;
        private int columns;
        private int rows;

        private List<Player> lastPlayersInOffside;

        /// <summary>
        /// Raises the ReactToBallPosition()-Method with the points, which are saved in the event arguments.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void BallWillMove(object sender, PointReceivedArgs e)
        {
            ReactToBallPosition(e.EventPoint, e.ExactEventPoint);
        }

        /// <summary>
        /// Sets the game status to Status.FreeKick, gives the fouled player the ball and blocks the foul-room for the other team.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void PlayerFouledAPlayer(object sender, FoulEventArgs e)
        {
            Player fouledPlayer = e.FouledPlayer;
            e.FoulPlayer.HasBall = false;
            e.FoulPlayer.CheckPlayerImage();
            Team tackledTeam = gameAI.PlayersTeam(fouledPlayer);

            Goal firstGoal = currentGame.FirstTeam.TeamGoal;
            Goal secondGoal = currentGame.SecondTeam.TeamGoal;

            HandleFreeKick(fouledPlayer.Location, tackledTeam);

            //checks whether the foul happened in the penalty room
            if (firstGoal.PenaltyRoom.Contains(fouledPlayer.Location) || secondGoal.PenaltyRoom.Contains(fouledPlayer.Location))
            {
                HandlePenaltyKick(fouledPlayer.Location, tackledTeam);
            }
            else
            {
                //HandleFreeKick(fouledPlayer.Location, tackledTeam);
            }
        }

        /* Sets myBall.PlayerContact bool to false. */
        public void PlayerLosesBall(object sender, EventArgs e)
        {
            Player rootPlayer = (Player)sender;
            Team enemyTeam = gameAI.GetEnemyTeam(gameAI.PlayersTeam(rootPlayer));

            //Saves the offside-standing players if the shooting player does not stand in the offside at the moment.
            //Only true if there is a normal game situation or a kick off at the moment.
            if ((currentGame.CurrentAction.GameStatus == Status.Normal || currentGame.CurrentAction.GameStatus == Status.KickOff) && !gameAI.IsOffside(rootPlayer, enemyTeam, 0))
            {
                SaveOffsidePlayers();
            }
            else
            {
                lastPlayersInOffside.Clear();
            }

            currentGame.GameBall.HasPlayerContact = false;
            rootPlayer.HasBall = false;
            if (currentGame.CurrentAction.GameStatus != Status.Normal)
            {
                gameAI.GetEnemyTeam(currentGame.CurrentAction.ActionTeam).IsAllowedToTakeBall = true;
                currentGame.CurrentAction.ActionTeam.IsAllowedToTakeBall = true;
                currentGame.CurrentAction = new GameAction(Status.Normal, currentGame.CurrentAction.ActionTeam, new Point(0, 0));
                Pathfinding.CurrentBlockedRoom = new BlockedRoom(new Rectangle(0, 0, 0, 0), currentGame.FirstTeam);
            }
        }

        /// <summary>
        /// Sets myBall.HasPlayerContact bool to true and the myBall.LastPlayer to the sender of this event
        /// Changes the image of the old player to an image of a player who isn't controling ball as well. 
        /// If the new player stands offside, HandleFreeKick() gets raised.
        /// </summary>
        /// <param name="sender">The player who controls the ball now.</param>
        /// <param name="e"></param>
        public void PlayerGetsBall(object sender, EventArgs e)
        {
            Player rootPlayer = (Player)sender;
            Ball currentBall = currentGame.GameBall;

            //Checks whether another player did control the ball - sets his HasBall-bool to false if true.
            if (currentBall.HasPlayerContact)
            {
                currentBall.LastPlayer.HasBall = false;
            }
            rootPlayer.HasBall = true;

            currentBall.HasPlayerContact = true;
            currentBall.LastPlayer = rootPlayer;
            currentBall.TargetPoint = null;
            currentBall.Speed = 0;
            currentGame.ResetPassTargetsAndActions();

            //Set the game status to goal kick if the goal keeper controls the ball
            if (rootPlayer.Position == PlayerPosition.Goalkeeper)
            {
                Team ownTeam = gameAI.PlayersTeam(rootPlayer);
                Team enemyTeam = gameAI.GetEnemyTeam(ownTeam);
                currentGame.CurrentAction.GameStatus = Status.GoalKick;
                currentGame.CurrentAction.ActionTeam = ownTeam;

                enemyTeam.IsAllowedToTakeBall = false;
            }

            if (currentGame.CurrentAction.GameStatus == Status.ThrowIn)
            {
                currentGame.CurrentAction.ThrowRoom = CalculateRoom(currentBall.Location, currentBall.LastPlayer.ThrowStrength);
            }

            if (lastPlayersInOffside.Contains(rootPlayer))
            {
                TakeBallFromCurrentPlayer();
                Team enemyTeam = gameAI.GetEnemyTeam(gameAI.PlayersTeam(rootPlayer));
                HandleFreeKick(rootPlayer.Location, enemyTeam);
                SetBallToPoint(rootPlayer.Location);
            }
        }

        /// <summary>
        /// If the ball's new position would be inside the field, everything is okay.
        /// But if it is outside of the field, the method will check whether the ball would move into a goal.
        /// </summary>
        /// <param name="gridLocation"></param>
        /// <param name="exactLocation"></param>
        private void ReactToBallPosition(Point gridLocation, Point exactLocation)
        {
            Ball currentBall = currentGame.GameBall;
            //true if the new position is inside the field
            if (gridLocation.X >= 1 && gridLocation.X <= columns - 2 && gridLocation.Y >= 1 && gridLocation.Y <= rows - 2)
            {
                currentBall.SetCoordinates(gridLocation.X, gridLocation.Y);
                currentBall.SetDirectionToAim();
                HandleNormalMove(gridLocation);
            }
            else if (currentGame.FirstTeam.TeamGoal.Contains(gridLocation) || currentGame.SecondTeam.TeamGoal.Contains(gridLocation))
            {
                HandleGoal(gridLocation, exactLocation);
            }
            else if (gridLocation.X <= 0 || gridLocation.X >= columns - 2)
            {
                HandleCornerOrGoalKick(gridLocation, exactLocation);
            }
            else if (gridLocation.Y <= 0 || gridLocation.Y >= rows - 2)
            {
                HandleThrowIn(gridLocation);
            }
        }

        /// <summary>
        /// Blocks the room for the team which tackled another player and gives the tackled team a free kick.
        /// </summary>
        /// <param name="actionPoint"></param>
        /// <param name="tackledTeam"></param>
        private void HandleFreeKick(Point actionPoint, Team tackledTeam)
        {
            BlockRoomAndTeam(actionPoint, tackledTeam, Status.FreeKick);
            currentGame.CurrentAction = new GameAction(Status.FreeKick, tackledTeam, actionPoint);
            SetBallToPoint(actionPoint);
        }

        /// <summary>
        /// Blocks the penalty room for the team which tackled another player and gives the tackled team a penalty kick.
        /// 
        /// **In Development**
        /// </summary>
        /// <param name="actionPoint"></param>
        /// <param name="fouledTeam"></param>
        private void HandlePenaltyKick(Point actionPoint, Team fouledTeam)
        {
        }

        /// <summary>
        /// If a player stands on the grid location in this round and in the next round, he has the chance to get the ball.
        /// </summary>
        /// <param name="gridLocation"></param>
        private void HandleNormalMove(Point gridLocation)
        {
            Player playerOnLocation = StandsPlayerOnLocation(gridLocation);
            if (playerOnLocation != null)
            {
                playerOnLocation.TryToGetBall(gameAI.GameBall);
            }

            if (playerOnLocation == null || !playerOnLocation.HasBall)
            {
                currentGame.GameBall.HasPlayerContact = false;
            }
        }

        /// <summary>
        /// Checks which team shot the goal, increases their goals count and changes the game status to KickOff.
        /// </summary>
        /// <param name="gridLocation"></param>
        /// <param name="exactLocation"></param>
        private void HandleGoal(Point gridLocation, Point exactLocation)
        {
            //Informs the last player that he does not control the ball anymore.
            //We have to do it now because otherwise the player would raise his LosesBall()-event,
            //which would set the Status to Normal.
            if (currentGame.GameBall.LastPlayer != null)
            {
                gameAI.AllPlayers.ToList().ForEach(x => x.HasBall = false);
            }

            currentGame.GameBall.TargetPoint = null;
            currentGame.GameBall.Speed = 0;
            currentGame.GameBall.HasPlayerContact = false;
            currentGame.GameBall.ExactLocation = exactLocation;
            currentGame.GameBall.Location = gridLocation;
            currentGame.GameBall.IsInShootState = false;
            Team goalTeam = (currentGame.SecondTeam.TeamGoal.Contains(gridLocation)) ? currentGame.FirstTeam : currentGame.SecondTeam;
            goalTeam.GoalsCount++;

            currentGame.CurrentAction = new GameAction(Status.KickOff, gameAI.GetEnemyTeam(goalTeam), gridLocation);
            currentGame.SetKickOff = true;

            BallEventImage image = new BallEventImage(currentGame.Round, goalTeam.TeamId, exactLocation);
            currentGame.CurrentReplayManager.Image.GoalImages.Add(image);
            if (currentGame.IsHost && currentGame.IsMultiplayer)
            {
                currentGame.InformAboutGoal(image);
            }

            currentGame.RefreshField();
            //GoalInformationBox.ShowBox();
            MessageBoxIcon icon = MessageBoxIcon.Information;
            MessageBox.Show("Goal!", "Information", MessageBoxButtons.OK, icon);
        }

        /// <summary>
        /// Checks which team has to throw in the ball and changes the game status to ThrowIn.
        /// Blocks the room around the location of the ball as well.
        /// </summary>
        /// <param name="gridLocation"></param>
        private void HandleThrowIn(Point ballLocation)
        {
            if (currentGame.FirstTeam.Players.Contains(currentGame.GameBall.LastPlayer))
            {
                currentGame.CurrentAction = new GameAction(Status.ThrowIn, currentGame.SecondTeam, ballLocation);
            }
            else
            {
                currentGame.CurrentAction = new GameAction(Status.ThrowIn, currentGame.FirstTeam, ballLocation);
            }

            currentGame.RefreshField();
            MessageBoxIcon icon = MessageBoxIcon.Information;
            MessageBox.Show("Outside of the field!", "Information", MessageBoxButtons.OK, icon);

            TakeBallFromCurrentPlayer();
            SetBallToPoint(ballLocation);

            BlockRoomAndTeam(ballLocation, currentGame.CurrentAction.ActionTeam, currentGame.CurrentAction.GameStatus);
        }

        private void HandleCornerOrGoalKick(Point ballLocation, Point exactBallLocation)
        {
            Team actionTeam = currentGame.FirstTeam;
            Status gameStatus;
            bool isCornerKick = false;
            Point actionPoint = ballLocation;

            Team leftTeam = (currentGame.FirstTeam.HasLeftSide) ? currentGame.FirstTeam : currentGame.SecondTeam;
            Team rightTeam = currentGame.GameAI.GetEnemyTeam(leftTeam);

            //check whether it is a corner kick or a goal kick
            if (ballLocation.X <= 0)
            {

                if (leftTeam.Players.Contains(currentGame.GameBall.LastPlayer))
                {
                    isCornerKick = true;
                    actionTeam = rightTeam;
                    actionPoint = (ballLocation.Y < leftTeam.TeamGoal.Location.Y) ? new Point(1, 1) : new Point(1, rows - 2);
                }
                else
                {
                    actionTeam = leftTeam;
                }
            }
            else if (ballLocation.X >= columns - 2)
            {
                if (rightTeam.Players.Contains(currentGame.GameBall.LastPlayer))
                {
                    isCornerKick = true;
                    actionTeam = leftTeam;
                    actionPoint = (ballLocation.Y < rightTeam.TeamGoal.Location.Y) ? new Point(columns - 2, 1) : new Point(columns - 2, rows - 2);
                }
                else
                {
                    actionTeam = rightTeam;
                }
            }

            TakeBallFromCurrentPlayer();

            //create and BallEventImage and informs the other users if this is a multiplayer game.
            BallEventImage image = new BallEventImage(currentGame.Round, actionTeam.TeamId, exactBallLocation);
            if (currentGame.IsHost && currentGame.IsMultiplayer)
            {
                currentGame.InformAboutGoal(image);
            }

            currentGame.RefreshField();
            MessageBoxIcon icon = MessageBoxIcon.Information;
            MessageBox.Show("Outside of the field!", "Information", MessageBoxButtons.OK, icon);

            if (isCornerKick)
            {
                gameStatus = Status.CornerKick;
                SetBallToPoint(actionPoint);
                BlockRoomAndTeam(actionPoint, actionTeam, gameStatus);
            }
            else
            {
                gameStatus = Status.GoalKick;
                GiveBallToPlayer(actionTeam.Goalkeeper);
            }

            currentGame.CurrentAction = new GameAction(gameStatus, actionTeam, ballLocation);
        }

        /// <summary>
        /// Creates a blocked room on the field depending on the next game status in which only one team can go through.
        /// </summary>
        /// <param name="rootPoint">The point where the ball will be in the next round</param>
        /// <param name="allowedTeam">The team which is allowed to go into this room</param>
        /// <param name="nextStatus">The next game status - CornerKick or GoalKick</param>
        private void BlockRoomAndTeam(Point actionPoint, Team allowedTeam, Status nextStatus)
        {
            int blockRange = (nextStatus == Status.CornerKick) ? 6 : 3;
            Rectangle blockedRoom = CalculateRoom(actionPoint, blockRange);

            Pathfinding.CurrentBlockedRoom = new BlockedRoom(blockedRoom, allowedTeam);
            gameAI.GetEnemyTeam(allowedTeam).IsAllowedToTakeBall = false;
        }

        /// <summary>
        /// Creates a rectangle which is within the game field.
        /// </summary>
        /// <param name="rootPoint"></param>
        /// <param name="range">The range around the root point</param>
        /// <returns></returns>
        private Rectangle CalculateRoom(Point rootPoint, int range)
        {
            Rectangle room = new Rectangle();

            int topHeight = (rootPoint.Y - range >= 1) ? range : rootPoint.Y - 1;
            int bottomHeight = ((rows - 2) - (rootPoint.Y - 1) > range) ? range : (rows - 2) - (rootPoint.Y - 1);
            int leftWidth = (rootPoint.X - range >= 1) ? range : rootPoint.X - 1;
            int rightWidth = ((columns - 2) - (rootPoint.X - 1) > range) ? range : (columns - 2) - (rootPoint.X);

            room.X = rootPoint.X - leftWidth;
            room.Y = rootPoint.Y - topHeight;
            room.Width = leftWidth + rightWidth + 1;
            room.Height = topHeight + bottomHeight;

            if (topHeight <= 0)
            {
                room.Height++;
            }

            return room;
        }

        /// <summary>
        /// Sets the location of the ball to the current location of the target player,
        /// deletes the target point of the ball and informs the player that he controls the ball now.
        /// </summary>
        /// <param name="targetPlayer"></param>
        private void GiveBallToPlayer(Player targetPlayer)
        {
            Ball currentBall = currentGame.GameBall;
            Pathfinding pathfinding = new Pathfinding();

            currentBall.Location = targetPlayer.Location;
            currentBall.ExactLocation = pathfinding.GetExactLocation(targetPlayer.Location);
            currentBall.Speed = 0;
            currentBall.TargetPoint = null;
            currentBall.IsInShootState = false;
            currentBall.HasPlayerContact = true;
            currentBall.LastPlayer = targetPlayer;

            targetPlayer.HasBall = true;
        }

        /// <summary>
        /// Sets the game ball to the parameter's location.
        /// </summary>
        /// <param name="targetPoint"></param>
        private void SetBallToPoint(Point targetPoint)
        {
            Ball currentBall = currentGame.GameBall;
            Pathfinding pathfinding = new Pathfinding();

            currentBall.Location = targetPoint;
            currentBall.ExactLocation = pathfinding.GetExactLocation(currentBall.Location);
            currentBall.Speed = 0;
            currentBall.TargetPoint = null;
            currentBall.HasPlayerContact = false;
            currentBall.IsInShootState = false;
        }

        /// <summary>
        /// Takes the ball from the current player so that he doesn't control the ball anymore.
        /// </summary>
        private void TakeBallFromCurrentPlayer()
        {
            Ball currentBall = currentGame.GameBall;

            currentBall.LastPlayer.HasBall = false;
            currentBall.HasPlayerContact = false;
        }

        /// <summary>
        /// Adds all players which stands offside at the moment to the lastPlayersInOffside-list.
        /// </summary>
        private void SaveOffsidePlayers()
        {
            lastPlayersInOffside.Clear();
            if(currentGame.GameBall.LastPlayer != null)
            {
                Team ballPlayerTeam = gameAI.PlayersTeam(currentGame.GameBall.LastPlayer);
                Team enemyTeam = gameAI.GetEnemyTeam(ballPlayerTeam);

                foreach (Player teamPlayer in ballPlayerTeam.Players)
                {
                    if(gameAI.IsOffside(teamPlayer, enemyTeam, 0))
                    {
                        lastPlayersInOffside.Add(teamPlayer);    
                    }
                }
            }
        }

        /// <summary>
        /// Checks whether a player stands at the location at the moment and will stand on it in the next round as well. 
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        private Player StandsPlayerOnLocation(Point location)
        {
            Pathfinding pathfinding = new Pathfinding();
            Player foundPlayer = null;
            gameAI.AllPlayers.ToList().ForEach(x =>
            {
                if (x.Location == location)
                {
                    ActionType type;
                    Point nextRoundLocation = pathfinding.PlayerAtSpecificRound(x, 1, out type);
                    if (nextRoundLocation == location)
                    {
                        foundPlayer = x;
                    }
                }
            });

            return foundPlayer;
        }
    }
}
