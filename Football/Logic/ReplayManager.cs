using Football.Logic.GameFiles;
using Football.Logic.GameFiles.Images;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Football.Logic
{
    class ReplayManager
    {
        public ReplayManager()
        {

        }

        public ReplayManager(GameSettings settings)
        {
            Image = new GameImage();
            Image.Settings = settings;

            FileName = "Football Savefile " + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ".fsg";
            serializer = new XmlSerializer(typeof(GameImage));
        }

        public ReplayManager(string path)
        {
            serializer = new XmlSerializer(typeof(GameImage));
            FileName = path;
        }

        public GameImage Image { get; set; }
        private string FileName { get; set; }
        private XmlSerializer serializer;

        /// <summary>
        /// Saves the current GameImage serialized in a xml file.
        /// </summary>
        public void Save()
        {
            Save(FileName);
        }

        /// <summary>
        /// Saves the current GameImage serialized in a xml file.
        /// </summary>
        /// <param name="path"></param>
        public void Save(string path)
        {
            using (StringWriter sr = new StringWriter())
            {
                serializer.Serialize(sr, Image);

                string xmlString = sr.ToString();
                string encrytedXML = Encrypt(xmlString);
                File.WriteAllText(path, encrytedXML);
            }
        }

        /// <summary>
        /// Loads a GameImage from a xml file at the given path.
        /// </summary>
        /// <param name="path"></param>
        public void Load()
        {
            try
            {
                string encryptedFile = File.ReadAllText(@FileName);
                string ivString = encryptedFile.Substring(encryptedFile.Length - 24, 24);
                byte[] iv = Convert.FromBase64String(ivString);
                string encryptedXml = encryptedFile.Substring(0, encryptedFile.Length - 24);

                string decryptedXml = Decrypt(encryptedXml, iv);

                using (XmlReader reader = XmlReader.Create(new StringReader(decryptedXml)))
                {
                    Image = (GameImage)serializer.Deserialize(reader);
                    Image.RoundImages.OrderBy(x => x.Number);
                }
            }
            catch
            {
                MessageBoxIcon icon = MessageBoxIcon.Error;
                MessageBox.Show("Corrupt savegame.", "Error!", MessageBoxButtons.OK, icon);
            }
        }

        #region Encryption
        /// <summary>
        /// Encrypts the plain text in the parameter with the Rijndael-algorithm.
        /// </summary>
        /// <param name="plain"></param>
        /// <returns></returns>
        public string Encrypt(string plain)
        {
            byte[] encrypted;
            string iv;
            using (RijndaelManaged rijndael = new RijndaelManaged())
            {
                byte[] plainBytes = Encoding.UTF8.GetBytes(plain);
                rijndael.IV = Convert.FromBase64String(GenerateIV());
                iv = Convert.ToBase64String(rijndael.IV);
                rijndael.Key = Convert.FromBase64String("/I3gyvuYTOCtEVcwqpyGKyo0LSuWT/l8");

                MemoryStream memoryStream = new MemoryStream();
                ICryptoTransform encryptor = rijndael.CreateEncryptor();

                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(plainBytes, 0, plainBytes.Length);
                }

                encrypted = memoryStream.ToArray();
                encryptor.Dispose();
                memoryStream.Dispose();
            }

            return Convert.ToBase64String(encrypted) + iv;
        }

        /// <summary>
        /// Decrypts the encrypted text with the Rijndael-algorithm with the initialization vector in the parameter.
        /// </summary>
        /// <param name="encrypted"></param>
        /// <param name="iv"></param>
        /// <returns></returns>
        public string Decrypt(string encrypted, byte[] iv)
        {
            string plain = string.Empty;

            using (RijndaelManaged rijndael = new RijndaelManaged())
            {
                byte[] encryptedBytes = Convert.FromBase64String(encrypted);
                rijndael.IV = iv;
                rijndael.Key = Convert.FromBase64String("/I3gyvuYTOCtEVcwqpyGKyo0LSuWT/l8");

                MemoryStream memoryStream = new MemoryStream();
                ICryptoTransform decryptor = rijndael.CreateDecryptor();

                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(encryptedBytes, 0, encryptedBytes.Length);
                }

                plain = Encoding.UTF8.GetString(memoryStream.ToArray());
                decryptor.Dispose();
                memoryStream.Dispose();
            }

            return plain;
        }

        /// <summary>
        /// Returns an unique initialization vector.
        /// </summary>
        /// <returns></returns>
        public string GenerateIV()
        {
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            byte[] byteArray = new byte[16];
            rng.GetNonZeroBytes(byteArray);

            return Convert.ToBase64String(byteArray);
        }
        #endregion

        #region Game
        /// <summary>
        /// Returns the halftime at specific round.
        /// </summary>
        /// <param name="round"></param>
        /// <returns></returns>
        public int SearchHalfTime(int round)
        {
            int halftime = (from change in Image.ChangeoverImages
                            where change.Round <= round
                            select change).Count();

            return halftime + 1;
        }

        /// <summary>
        /// Returns the amount of goals a specific team has at at specific round.
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="round"></param>
        /// <returns></returns>
        public int SearchTeamGoalsCount(int teamId, int round)
        {
            int amount = (from goal in Image.GoalImages
                          where goal.Round <= round && goal.TeamId == teamId
                          select goal).Count();

            //int test = Image.GoalImages.Count(x => x.TeamId == teamId); //lambda expression

            return amount;
        }

        /// <summary>
        /// Returns the team id of the team which controls the left goal at a specific round.
        /// </summary>
        /// <param name="round"></param>
        /// <returns></returns>
        public int LeftGoalOwner(int round)
        {
            var changeovers = from changeover in Image.ChangeoverImages
                          where changeover.Round <= round orderby changeover.Round descending
                          select changeover;

            int leftId = 0;
            if (changeovers.Count() > 0)
            {
                leftId = changeovers.ElementAt(0).LeftGoalTeamId;
            }

            return leftId;
        }

        /// <summary>
        /// Returns a RoundImage with a specific round number.
        /// Returns null if there is no RoundImage with this number.
        /// </summary>
        /// <param name="round"></param>
        /// <returns></returns>
        public RoundImage SearchImageAtRound(int round)
        {
            IEnumerable<RoundImage> images = from im in Image.RoundImages
                                      where im.Number == round
                                      select im;

            if (images.Count() > 0)
            {
                return images.ElementAt(0);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Searches the highest round number.
        /// </summary>
        /// <returns></returns>
        public int SearchLastRoundNumber()
        {
            int number = Image.RoundImages.Max(x => x.Number);
            return number;
        }
        #endregion
    }
}
