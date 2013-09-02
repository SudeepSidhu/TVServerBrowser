using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace TVServerBrowser
{
    public static class FireStuff
    {
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
                foreach (var server in MainForm.Servers.Distinct())
                {
                    IPAddress addr;
                    if (!IPAddress.TryParse(server, out addr)) continue;
                    var splat = addr.ToString().Split(':');
                    string ip = splat[0];
                    string port = splat.Length == 2 ? splat[1] : 7777.ToString();
                    writer.WriteLine("serverFavorites=(IP=\"{0}\",Port=\"{1}\")", ip, port);
                }
                writer.Close();
            }
        }
    }
}
