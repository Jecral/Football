using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Football.Multiplayer
{
    class ConnectionDatabaseManager
    {
        public ConnectionDatabaseManager()
        {
            if (DoesFileExist() && IsFileValid())
            {
                database = XDocument.Load("database.xml");
            }
            else
            {
                CreateDatabase();
                database = XDocument.Load("database.xml");
            }
        }

        private XDocument database;

        /// <summary>
        /// Checks whether the database file exists
        /// </summary>
        /// <returns></returns>
        private bool DoesFileExist()
        {
            return File.Exists("database.xml");
        }

        /// <summary>
        /// Validates the database xml file.
        /// </summary>
        /// <returns></returns>
        private bool IsFileValid()
        {
            XDocument doc;
            try
            {
                doc = XDocument.Load("database.xml");
            }
            catch
            {
                return false;
            }

            XmlSchemaSet xmlSchema = new XmlSchemaSet();
            xmlSchema.Add("", XmlReader.Create(new StringReader(Properties.Resources.ConnectionsXSD)));

            bool valid = true;
            doc.Validate(xmlSchema, (o, e) => //ValidationEventHandler in a lambda expression
            {
                Console.WriteLine("{0}", e.Message);
                valid = false;
            });

            return valid;
        }

        /// <summary>
        /// Creates a new xml file with a declaration, "Database" as a root element and "Connections" as its descendant
        /// </summary>
        private void CreateDatabase()
        {
            XDocument database = new XDocument(
                new XDeclaration("1.0", "UTF-16", null),
                new XElement("Database",
                    new XElement("Connections")
                ));

            database.Save("database.xml");
        }

        /// <summary>
        /// Inserts a new connection into the database
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="name"></param>
        public void InsertConnection(IPAddress ip, int port, string name, DateTime lastLogin)
        {
            lock (database)
            {
                XElement root = database.Descendants("Connections").ElementAt(0);
                XElement connection = new XElement("Connection",
                                        new XElement("IP-Address", ip),
                                        new XElement("Port", port),
                                        new XElement("Username", name),
                                        new XElement("LastLogin", lastLogin.ToString("yyyy-MM-dd HH:mm:ss"))
                                        );

                root.Add(connection);
                database.Save("database.xml");
            }
        }

        /// <summary>
        /// Reads all used connections and returns them in a two dimensional string array.
        /// </summary>
        /// <returns></returns>
        public string[,] ReadConnections()
        {
            IEnumerable<XElement> root = database.Descendants("Connections");
            var test = from nm in root.Elements()
                       orderby nm.Element("LastLogin").Value descending
                       select nm;

            string[,] connectionsList = new string[test.Count(), 3];
            for (int i = 0; i < test.Count(); i++)
            {
                XElement connection = test.ElementAt(i);
                connectionsList[i, 0] = connection.Element("IP-Address").Value;
                connectionsList[i, 1] = connection.Element("Port").Value;
                connectionsList[i, 2] = connection.Element("Username").Value;
            }

            return connectionsList;
        }

        /// <summary>
        /// Checks whether a connection with this IP-address does already exist.
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool DoesConnectionExist(IPAddress ip)
        {
            IEnumerable<XElement> root = database.Descendants("Connections");
            var test = from nm in root.Elements()
                       where nm.Element("IP-Address").Value == ip.ToString()
                       select nm.Element("IP-Address").Value;

            return test.Count() > 0;
        }

        /// <summary>
        /// Changes the saved port for this ip-address to the new port.
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        public void UpdatePort(IPAddress ip, int port)
        {
            lock (database)
            {
                IEnumerable<XElement> root = database.Descendants("Connections");
                var test = (from nm in root.Elements()
                            where nm.Element("IP-Address").Value == ip.ToString()
                            select nm).First();

                test.SetElementValue("Port", port);
                database.Save("database.xml");
            }
        }

        /// <summary>
        /// Returns the saved username for this ip address
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public string GetUsername(IPAddress ip)
        {
            IEnumerable<XElement> root = database.Descendants("Connections");
            var test = (from nm in root.Elements()
                        where nm.Element("IP-Address").Value == ip.ToString()
                        select nm.Element("Username").Value).First();

            return test;
        }

        /// <summary>
        /// Updates the username for this ip address to a new name
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="name"></param>
        public void UpdateUsername(IPAddress ip, string name)
        {
            lock (database)
            {
                IEnumerable<XElement> root = database.Descendants("Connections");
                var test = (from nm in root.Elements()
                            where nm.Element("IP-Address").Value == ip.ToString()
                            select nm).First();

                test.SetElementValue("Username", name);
                database.Save("database.xml");
            }
        }

        /// <summary>
        /// Updates the LastLogin datetime for this ip address to a new datetime
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="time"></param>
        public void UpdateLastLoginDate(IPAddress ip, DateTime time)
        {
            lock (database)
            {
                IEnumerable<XElement> root = database.Descendants("Connections");
                var test = (from nm in root.Elements()
                            where nm.Element("IP-Address").Value == ip.ToString()
                            select nm).First();

                test.SetElementValue("LastLogin", time.ToString("yyyy-MM-dd HH:mm:ss"));
                database.Save("database.xml");
            }
        }
    }
}
