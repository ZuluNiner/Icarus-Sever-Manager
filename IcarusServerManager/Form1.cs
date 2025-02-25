using IcarusServerManager.Classes;
using System.Diagnostics;
using System.Windows.Forms;

namespace IcarusServerManager
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            consoleTextbox.Text = "Initializing...";
            this.FormClosed += KillProcess;
        }

        public Process serverProc;
        private bool serverStarted = false;
        private DateTime startTime;
        private FolderBrowserDialog folderBrowser;
        private void Form1_Load(object sender, EventArgs e)
        {

            serverLocationBox.Text = Properties.Settings.Default.serverLocation;
            if (Properties.Settings.Default.IsInit)
            {
                serverStatusBox.Text = "Waiting for install";
            }
            else
            {
                startServerButton.Enabled = true;
                serverStatusBox.Text = "Idle";
            }

            serverNameBox.Text = Properties.Settings.Default.serverName;
            queryPortBox.Text = Properties.Settings.Default.queryPort.ToString();
            gamePortBox.Text = Properties.Settings.Default.gamePort.ToString();
            int restartIndex = (Properties.Settings.Default.restartInterval / 60) - 1;
            restartTimeBox.SelectedIndex = restartIndex;
            Debug.WriteLine(restartIndex);
            WriteToConsole("Server manager initialized");
        }

        public void WriteToConsole(string text)
        {
            if (consoleTextbox.InvokeRequired)
            {
                consoleTextbox.Invoke(new InvokeConsoleWrite(WriteToConsole), new object[] { text });
            }
            else
            {
                consoleTextbox.Text = consoleTextbox.Text + Environment.NewLine + text;
                // set the current caret position to the end
                consoleTextbox.SelectionStart = consoleTextbox.Text.Length;
                // scroll it automatically
                consoleTextbox.ScrollToCaret();
            }
        }


        private void selectLocationButton_Click(object sender, EventArgs e)
        {
            folderBrowser = new FolderBrowserDialog();
            folderBrowser.ShowDialog();

            string folder = folderBrowser.SelectedPath;

            Properties.Settings.Default.serverLocation = folder;
            Properties.Settings.Default.Save();
            serverLocationBox.Text = folder;
            if (!File.Exists(folder + "\\Icarus\\Binaries\\Win64\\IcarusServer-Win64-Shipping.exe"))
            {
                WriteToConsole("An Icarus installation was not found at the specified location, please click the install button to proceed");
                startServerButton.Enabled = false;
                serverStatusBox.Text = "Waiting for install";
            }
            else
            {
                WriteToConsole("An Icarus installation was found at the specified location, your server is ready to start.\n\nNote you can press the install button at any time to initiate an update and file verification");
                startServerButton.Enabled = true;
            }
        }

        private void installServerButton_Click(object sender, EventArgs e)
        {
            serverStatusBox.Text = "Installing...";
            ProcessStartInfo procStartInfo = new ProcessStartInfo(Environment.CurrentDirectory + "\\steamcmd\\steamcmd.exe", "/b /w /high +login anonymous +force_install_dir \"" + Properties.Settings.Default.serverLocation + "\" +app_update 2089300 validate +quit");
            Process installer = new Process();
            installer.StartInfo = procStartInfo;
            installer.Start();
            installer.WaitForExit();

            installServerButton.Enabled = false;
            startServerButton.Enabled = true;
            serverStatusBox.Text = "Idle";

            Properties.Settings.Default.IsInit = false;
            Properties.Settings.Default.Save();
        }

        private void startServerButton_Click(object sender, EventArgs e)
        {

            if (serverStarted)
            {
                StopProcess();
            }
            else
            {
                StartProcess();
            }
        }

        private void StartProcess()
        {
            WriteToConsole("Starting game server...");
            new Task(() =>
            {
                ProcessStartInfo procStartInfo = new ProcessStartInfo(Properties.Settings.Default.serverLocation + "\\Icarus\\Binaries\\Win64\\IcarusServer-Win64-Shipping.exe", "-nosteamclient -Port=17777 -QueryPort=27023 -SteamServerName=M.o.M.");
                serverProc = new Process();
                serverProc.StartInfo = procStartInfo;

                serverProc.Disposed += ProcessEnded;

                serverProc.Start();
                startTime = DateTime.Now;
                UpdateStatus("Running");
                startServerButton.BackColor = Color.Green;
                ChangeStartButton("Stop Server");
                serverStarted = true;
                serverProc.OutputDataReceived += HandleExeOutput;
                WriteToConsole("Game server started");
                serverProc.WaitForExit();
                serverStarted = false;
            }).Start();

            new Task(async () =>
            {
                WriteToConsole("Starting restart timer...");
                WriteToConsole("Restart interval is " + Properties.Settings.Default.restartInterval + " minutes");
                await Task.Delay(5000);
                WriteToConsole("Restart timer started");
                while (serverStarted)
                {
                    if (startTime.AddMinutes(Properties.Settings.Default.restartInterval) <= DateTime.Now)
                    {
                        WriteToConsole("Initiating restart");
                        StopProcess();
                        await Task.Delay(5000);
                        StartProcess();
                    }
                    else
                    {
                        TimeSpan nextRestart = (startTime.AddMinutes(Properties.Settings.Default.restartInterval) - DateTime.Now);
                        WriteToConsole("Restart check performed, next restart in: " + double.Floor(nextRestart.TotalMinutes).ToString() + " minutes");
                        Debug.WriteLine(nextRestart.TotalMinutes);
                    }
                    await Task.Delay(60000);
                    GC.Collect();
                }
                WriteToConsole("Restart timer ended");
            }).Start();
        }

        private void StopProcess()
        {
            if (serverStarted)
            {
                WriteToConsole("Stopping server...");
                serverProc.Kill();
                serverProc.Close();
                serverStarted = false;
                UpdateStatus("Idle...");
                startServerButton.BackColor = Color.Maroon;
                ChangeStartButton("Start Server");
                WriteToConsole("Game server stopped");
            }
        }

        private void KillProcess(object o, EventArgs e)
        {
            if (serverProc != null && serverStarted)
            {
                serverProc.Kill();
                Environment.Exit(0);
            }
        }

        private void HandleExeOutput(object sender, DataReceivedEventArgs e)
        {
            string output = e.Data;
            // do something here with the string output
            WriteToConsole(output);
        }

        public void ProcessEnded(object o, EventArgs e)
        {
            serverStarted = false;
        }

        private void UpdateStatus(string text)
        {
            if (serverStatusBox.InvokeRequired)
            {
                serverStatusBox.Invoke(new InvokeConsoleWrite(UpdateStatus), new object[] { text });
            }
            else
            {
                serverStatusBox.Text = text;
            }
        }

        private void ChangeStartButton(string text)
        {
            if (startServerButton.InvokeRequired)
            {
                startServerButton.Invoke(new InvokeConsoleWrite(ChangeStartButton), new object[] { text });
            }
            else
            {
                startServerButton.Text = text;
            }
        }

        private void serverNameBox_TextChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.serverName = serverNameBox.Text;
            Properties.Settings.Default.Save();
        }

        private void queryPortBox_TextChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.queryPort = int.Parse(queryPortBox.Text);
            Properties.Settings.Default.Save();
        }

        private void gamePortBox_TextChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.gamePort = int.Parse(gamePortBox.Text);
            Properties.Settings.Default.Save();
        }

        private void restartTimeBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            int time = int.Parse(restartTimeBox.Text.Split(' ')[0]);
            time = time * 60;

            Properties.Settings.Default.restartInterval = time;
            Properties.Settings.Default.Save();
        }

        public delegate void InvokeConsoleWrite(string text);
    }
}
