using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Permissions;
using System.Text;

namespace TVServerBrowser
{
    public static class FireStuff
    {
        public const string HostsFilePath = @"C:\windows\system32\drivers\etc\hosts";

        public static void SaveToProfiles(FileInfo exePath)
        {
            List<FileInfo> profiles = new List<FileInfo>();

            foreach (var item in Directory.GetFiles(new DirectoryInfo(exePath.Directory.FullName).Parent.Parent.FullName + @"\Content\System\Profiles", "*.ini"))
            {
                profiles.Add(new FileInfo(item));
            }

            foreach (var profile in profiles)
            {
                FileAttributes attributes = File.GetAttributes(profile.FullName);
                if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    File.SetAttributes(profile.FullName, attributes ^ FileAttributes.ReadOnly);
                }
                string[] strArray2 = File.ReadAllLines(profile.FullName);
                List<string> list = new List<string>();
                foreach (string str4 in strArray2)
                {
                    if (str4.Substring(0, Math.Min(str4.Length, "serverFavorites=".Length)) != "serverFavorites=")
                    {
                        list.Add(str4);
                    }
                }
                TextWriter writer = new StreamWriter(profile.FullName);
                foreach (string str6 in list)
                {
                    writer.WriteLine(str6);
                }
                foreach (TVServer server in MainForm.tvServers)
                {
                    string ip = server.ipAddress;
                    string port = server.port;
                    writer.WriteLine("serverFavorites=(IP=\"{0}\",Port=\"{1}\")", ip, port);
                }
                writer.Close();
            }
        }

        public static bool HostsOK()
        {
            var hostsfileok = false;
            foreach (string str in File.ReadAllLines(HostsFilePath))
            {
                if (str == "127.0.0.1 tribesv.available.gamespy.com")
                    hostsfileok = true;
            }
            return hostsfileok;
        }

        [PrincipalPermission(SecurityAction.Demand, Role = @"BUILTIN\Administrators")]
        public static void AddToHosts()
        {
            if (!HostsOK())
            {
                File.AppendAllText(HostsFilePath, Environment.NewLine + "127.0.0.1 tribesv.available.gamespy.com" + Environment.NewLine);
            }
        }
    }
}
