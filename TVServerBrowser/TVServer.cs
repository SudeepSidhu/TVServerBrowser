using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TVServerBrowser
{
    public class TVServer
    {
        public String mapName { get; private set; }
        public String gameType { get; private set; }
        public String serverName { get; private set; }
        public String ipAddress { get; private set; }
        public String port { get; private set; }
        public String numPlayers { get; private set; }
        public String maxPlayers { get; private set; }
        public String password { get; private set; }
        public String adminEmail { get; private set; }



        public TVServer(String info, String address)
        {
            ipAddress = String.Copy(address);

           List<String> vals = new List<string>(info.Split('\\'));

            foreach (String val in vals)
            {
                switch (val)
                {
                    case "mapname":
                        mapName = vals[vals.IndexOf("mapname") + 1];
                        break;
                    case "numplayers":
                        numPlayers = vals[vals.IndexOf("numplayers") + 1];
                        break;
                    case "maxplayers":
                        maxPlayers = vals[vals.IndexOf("maxplayers") + 1];
                        break;
                    case "hostname":
                        serverName = vals[vals.IndexOf("hostname") + 1];
                        break;
                    case "hostport":
                        port = vals[vals.IndexOf("hostport") + 1];
                        break;
                    case "gametype":
                        gameType = vals[vals.IndexOf("gametype") + 1];
                        break;
                    case "password":
                        if (vals[vals.IndexOf("password") + 1].Equals("0"))
                        {
                            password = "No";
                        }
                        else
                        {
                            password = "Yes";
                        }
                        break;
                    case "adminemail":
                        adminEmail = vals[vals.IndexOf("adminemail") + 1];
                        break;
                    default:
                        break;
                }
            }
        }

        public bool isValid()
        {
            return mapName.Trim().Length > 0 &&
                gameType.Trim().Length > 0 &&
                serverName.Trim().Length > 0 &&
                ipAddress.Trim().Length > 0 &&
                port.Trim().Length > 0 &&
                numPlayers.Trim().Length > 0 &&
                maxPlayers.Trim().Length > 0 &&
                password.Trim().Length > 0;
        }
    }
}
