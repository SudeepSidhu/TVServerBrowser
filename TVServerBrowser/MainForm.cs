using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Net.NetworkInformation;

namespace TVServerBrowser
{
    public partial class MainForm : Form
    {
        UdpClient selfUDPSocket;
        TcpClient selfTCPSocket;

        private readonly object lockObj = new object();

        private Thread serverLoader;

        public static List<Server> servers = new List<Server>();
        public static List<TVServer> tvServers = new List<TVServer>();

        public MainForm()
        {
            InitializeComponent();

            selfUDPSocket = new UdpClient(new IPEndPoint(IPAddress.Any, 0));

            selfUDPSocket.Client.SendTimeout = 1500;
            selfUDPSocket.Client.ReceiveTimeout = 1500;
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            Search();
        }

        public void Search()
        {
            var nServers = getMasterServerList();
            btnJoin.Enabled = false;

            if (nServers != null && nServers.Length > 0)
            {
                serverLoader = new Thread(loadServers);
                servers.Clear();
                servers.AddRange(nServers);
                serverLoader.Start();
            }

            btnSearch.Enabled = true;
        }

        private Server[] getMasterServerList()
        {
            List<Server> list = new List<Server>();
            
            WebClient client = new WebClient();
            String result = client.DownloadString("http://pastebin.com/raw.php?i=2EENrPgG");

            String[] textInfo = result.Split('\n');

            foreach (String str in textInfo){

                String[] addressAndPort = str.Split(':');
                IPAddress address;
                int port;

                if (addressAndPort.Length == 2 && IPAddress.TryParse(addressAndPort[0],out address) && int.TryParse(addressAndPort[1],out port))
                {
                    Server s = new Server();
                    s.address = address;
                    s.port = port;
                    
                    list.Add(s);
                }
            }

            return list.ToArray();
        }

        private void loadServers()
        {
            lock (lockObj)
            {
                try
                {
                    btnSearch.Invoke((MethodInvoker)delegate { btnSearch.Enabled = false; });
                    lstServers.Invoke((MethodInvoker)delegate { lstServers.Items.Clear(); });
                    tvServers.Clear();

                    foreach (Server server in servers)
                    {
                        getServerInfo(selfUDPSocket, server.address, server.port);
                    }

                    btnSearch.Invoke((MethodInvoker)delegate { btnSearch.Enabled = true; });

                    if (lstServers.Items.Count > 0)
                    {
                        lstServers.Invoke((MethodInvoker)delegate
                        {
                            lstServers.HideSelection = false;
                            lstServers.Items[0].Selected = true;
                            lstServers.Select();
                            lstServers.Focus();
                        });

                        btnJoin.Invoke((MethodInvoker)delegate
                        {
                            btnJoin.Enabled = true;
                        });
                    }
                }
                catch { }
            }
        }

        private void getServerInfo(UdpClient socket, IPAddress address, int port)
        {
            String strAddress = String.Copy(address.ToString());

            IPEndPoint endPoint = new IPEndPoint(address, port);
            byte[] send_buffer = new byte[] { 0x5c, 0x73, 0x74, 0x61, 0x74, 0x75, 0x73, 0x5c, 0x00 }; // "\status\."

            try
            {
                socket.Send(send_buffer, 9, endPoint);

                byte[] info = socket.Receive(ref endPoint);


                String longServerInfo = System.Text.Encoding.Default.GetString(info);

                TVServer server = new TVServer(longServerInfo, strAddress);

                if (server.isValid())
                {
                    tvServers.Add(server);

                    Ping pingSender = new Ping();

                    string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
                    byte[] buffer = Encoding.ASCII.GetBytes(data);
                    int timeout = 2000;
                    PingOptions options = new PingOptions(64, false);

                    PingReply reply = pingSender.Send(server.ipAddress, timeout, buffer, options);

                    if (reply.Status == IPStatus.Success)
                    {
                        addToList(server, reply.RoundtripTime.ToString());
                    }
                    else
                    {
                        addToList(server, "N/A");
                    }


                }
            }
            catch { }
        }

        private void addToList (TVServer server, String ping){

            ListViewItem newItem = new ListViewItem(new String[] { server.serverName, ping, server.mapName, server.gameType, server.numPlayers + "/" + server.maxPlayers, server.ipAddress + ":" + server.port, server.password, server.adminEmail });
            newItem.Tag = server;
            lstServers.Invoke((MethodInvoker)delegate { lstServers.Items.Add(newItem); });
        }

        static string get32BitProgramFilesDir()
        {
            if (8 == IntPtr.Size || (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))))
            {
                return Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            }

            return Environment.GetEnvironmentVariable("ProgramFiles");
        }

        private String getGameLocation()
        {
            string storedLocation = Properties.Settings.Default.gamePath;

            if (storedLocation.Trim().Length > 0)
            {
                return storedLocation;
            }
            else
            {
                MessageBoxEx.Show(this, "Looks like you haven't set the location where the game is installed yet. This program needs that location to know what game to launch. When you click \"Ok\" a form will be presented to you to do just that.", "Input required", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                using (OpenFileDialog openFileDialog1 = new OpenFileDialog())
                {

                    openFileDialog1.InitialDirectory = get32BitProgramFilesDir();
                    openFileDialog1.Filter = "|TV_CD_DVD.exe";
                    openFileDialog1.FilterIndex = 2;
                    openFileDialog1.RestoreDirectory = true;

                    if (openFileDialog1.ShowDialog() == DialogResult.OK)
                    {

                        string selectedFile = openFileDialog1.FileName;

                        Properties.Settings.Default.gamePath = selectedFile;
                        Properties.Settings.Default.Save();

                        return selectedFile;
                    }
                }
            }
            return "";
        }

        private void openTribes(String path, String server, String port)
        {
            Process tvExe = new Process();

            tvExe.StartInfo.FileName = path;

            List<String> args = new List<String>();

            args.Add(" " + server + ":" + port);

            if (chkConsole.Checked)
            {
                args.Add("-console");
            }

            if (chkWindowed.Checked)
            {
                args.Add("-windowed");
            }

            String strArgs = String.Join(" ", args.ToArray());
            String workDir = path.Substring(0, path.LastIndexOf('\\') + 1);

            tvExe.StartInfo.WorkingDirectory = workDir;
            tvExe.StartInfo.Arguments = strArgs;

            tvExe.Start();
        }


        private void selectItem(ListViewItem item)
        {

            try
            {
                String gamePath = getGameLocation();

                if (gamePath.Trim().Length <= 0)
                {
                    gamePath = getGameLocation();
                }

                if (gamePath.Trim().Length > 0)
                {
                    TVServer server = (TVServer)item.Tag;
                    openTribes(gamePath, server.ipAddress, server.port);
                }
            }
            catch (Exception e)
            {
                MessageBoxEx.Show(this, "Error while opening game:\r\n" + e.ToString() + "\r\n\r\nYou might have the wrong game path set.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void lstServers_MouseDoubleClick(object sender, MouseEventArgs e)
        {

            if (lstServers.SelectedItems[0] != null)
            {
                selectItem(lstServers.SelectedItems[0]);
            }

        }

        private void chkConsole_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.useConsole = chkConsole.Checked;
            Properties.Settings.Default.Save();
        }

        private void chkWindowed_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.useWindow = chkWindowed.Checked;
            Properties.Settings.Default.Save();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Search();
            chkConsole.Checked = Properties.Settings.Default.useConsole;
            chkWindowed.Checked = Properties.Settings.Default.useWindow;
        }

        private void btnFindGame_Click(object sender, EventArgs e)
        {


            using (OpenFileDialog openFileDialog1 = new OpenFileDialog())
            {

                openFileDialog1.InitialDirectory = get32BitProgramFilesDir();
                openFileDialog1.Filter = "|TV_CD_DVD.exe";
                openFileDialog1.FilterIndex = 2;
                openFileDialog1.RestoreDirectory = true;

                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    String newLocation = openFileDialog1.FileName;
                    if (!File.Exists(newLocation))
                    {
                        MessageBox.Show("File does not exist");
                        return;
                    }

                    if (newLocation.Trim().Length > 0)
                    {
                        Properties.Settings.Default.gamePath = newLocation;
                        Properties.Settings.Default.Save();
                    }
                }
            }


        }

        private void btnJoin_Click(object sender, EventArgs e)
        {
            if (lstServers.SelectedItems[0] != null)
            {
                selectItem(lstServers.SelectedItems[0]);
            }
        }

        private void btnAbout_Click(object sender, EventArgs e)
        {
            MessageBoxEx.Show(this, "TV Server Browser\r\nInspired by the community.\r\nCreated by B7ADE.\r\n\r\nEmail ib7ade@gmail.com for help.", "About", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!GameSet())
            {
                MessageBox.Show("Please set game installation first.");
                return;
            }
            try
            {
                if (!FireStuff.HostsOK())
                {
                    MessageBox.Show("Your hosts file does not seem to contain redirection to localhost.\nSaving to profiles needs this feature. Please run this application as administrator and it will automagically add redirection to localhost.");
                    FireStuff.AddToHosts();
                }
                FireStuff.SaveToProfiles(new FileInfo(Properties.Settings.Default.gamePath));
            }
#if !DEBUG
            catch (Exception ex)
            {
                MessageBox.Show("Error while saving to profiles \n" + ex);
            }
#endif

            finally
            {

            }
        }


        public bool GameSet()
        {
            if (string.IsNullOrWhiteSpace(Properties.Settings.Default.gamePath)) return false;
            return new FileInfo(Properties.Settings.Default.gamePath).Exists;
        }
    }
}
