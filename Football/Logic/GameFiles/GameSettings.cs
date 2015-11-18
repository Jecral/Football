using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Football.Logic
{
    public class GameSettings
    {
        public GameSettings(int columns, int rows, int midfieldLineXPosition, int midfieldPointYPosition, int leftGoalHeight, int leftGoalYPosition, int rightGoalHeight, int rightGoalYPosition, int secondsPerRound, int roundsPerGame, bool firstTeamUsesKI, bool secondTeamUsesKI)
        {
            Columns = columns;
            Rows = rows;
            MidLineXPosition = midfieldLineXPosition;
            MidPointYPosition = midfieldPointYPosition;
            LeftGoalHeight = leftGoalHeight;
            LeftGoalYPosition = leftGoalYPosition;
            RightGoalHeight = rightGoalHeight;
            RightGoalYPosition = rightGoalYPosition;
            SecondsPerRound = secondsPerRound;
            RoundsPerHalf = roundsPerGame;
            FirstTeamUsesKI = firstTeamUsesKI;
            SecondTeamUsesKI = secondTeamUsesKI;
        }

        public GameSettings()
        {

        }

        [XmlAttribute]
        public int MidLineXPosition { get; set; }
        [XmlAttribute]
        public int MidPointYPosition { get; set; }
        [XmlAttribute]
        public int LeftGoalHeight { get; set; }
        [XmlAttribute]
        public int LeftGoalYPosition { get; set; }
        [XmlAttribute]
        public int RightGoalHeight { get; set; }
        [XmlAttribute]
        public int RightGoalYPosition { get; set; }
        [XmlAttribute]
        public int Columns { get; set; }
        [XmlAttribute]
        public int Rows { get; set; }
        [XmlAttribute]
        public int SecondsPerRound { get; set; }
        [XmlAttribute]
        public int RoundsPerHalf { get; set; }
        [XmlAttribute]
        public bool FirstTeamUsesKI { get; set; }
        [XmlAttribute]
        public bool SecondTeamUsesKI { get; set; }
    }
}
