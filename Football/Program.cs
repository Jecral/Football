using Football.GUI;
using Football.GUI.General;
using Football.Logic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Football
{
    class Program
    {
        /*
         * Globe Image by: http://www.webdesignerdepot.com/ - Free for commercial use
         * Pencil Image by: https://www.iconfinder.com/ymbproperties - Free for commercial use
         */

        [STAThread]
        public static void Main(String[] args)
        {
            LogfilePath = "Logfile " + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ".txt";

            int columns = 51; //standard 51
            int rows = 25; //standard 25
            int midfieldLineXPosition = 25; //standard 25
            int midfieldPointYPosition = 12; //standard 12
            int leftGoalHeight = 5; //standard 5
            int leftGoalYPosition = 10; //standard 10
            int rightGoalHeight = 5; //standard 5
            int rightGoalYPosition = 10; //standard 10
            int secondsPerStep = 1200;

            //standard settings
            GameSettings settings = new GameSettings(columns, rows, midfieldLineXPosition, midfieldPointYPosition, leftGoalHeight, leftGoalYPosition, rightGoalHeight, rightGoalYPosition, secondsPerStep, 100, true, true);
            try
            {
                //start the program with the safegame if the user dropped it to the executable
                if (args.Length > 0 && args[0].EndsWith(".fsg"))
                {
                    Application.Run(new FieldForm(args[0], settings));
                }
                else
                {
                    Application.Run(new FieldForm(settings));
                }
            }
            catch(Exception e)
            {
                MessageBoxButtons button = MessageBoxButtons.OK;
                MessageBoxIcon icon = MessageBoxIcon.Error;
                MessageBox.Show("Something went wrong.\nPlease have a look at the log file (" + Program.LogfilePath + ").", "Error", button, icon);

                File.AppendAllText(Program.LogfilePath, Program.TimeString + " [" + e.GetType() + "]: " + e.Message + " Source: " + e.Source + "\nInnerException: " + e.InnerException + "\nHelpLink: " + e.HelpLink + "\nStackTrace: " + e.StackTrace + "\n\n");
            }
        }

        public static string LogfilePath;

        public static String TimeString
        {
            get
            {
                return DateTime.Now.ToString("[HH:mm:ss]");
            }
        }
    }
}
