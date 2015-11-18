using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Football.Logic.GameFiles.Images
{
    public class ChangeoverImage
    {
        /// <summary>
        /// Saves which team owns which goal.
        /// </summary>
        /// <param name="leftId"></param>
        /// <param name="rightId"></param>
        public ChangeoverImage(int round, int leftId, int rightId)
        {
            LeftGoalTeamId = leftId;
            RightGoalTeamId = rightId;
            Round = round;
        }

        public ChangeoverImage()
        {

        }

        public int LeftGoalTeamId { get; set; }
        public int RightGoalTeamId { get; set; }
        public int Round { get; set; }
    }
}
