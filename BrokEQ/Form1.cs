using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using WindowsInput;
using WindowsInput.Native;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Discord;
using Discord.API;
using Discord.WebSocket;
using Discord.Audio;
using Discord.Commands;
using static BrokEQ.ScanCodes;
using static BrokEQ.KeySetup;

namespace BrokEQ
{
    public partial class BrokEQ : Form
    {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);





        private string sCharmedMobName = string.Empty;
        public static IntPtr eqHwnd;
        public static StringBuilder sbOut = new StringBuilder();
        InputSimulator s = new InputSimulator();
        public static long _currentSize = 0;
        public static long _previousSize = 0;
        
        // Tests .+?(?=has been charmed)
        public static string sVals = "Your Charm spell has worn off|.+?(?=has been charmed)";
        public Regex rTest = new Regex(sVals);


        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern IntPtr GetMessageExtraInfo();

        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(System.Windows.Forms.Keys vKey);

        /// <summary>
        /// PreDefined Values for Keys
        /// </summary>
        /// 
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;
        public const int KEYEVENTF_EXTENDEDKEY = 0x0001; //Key down flag
        public const int KEYEVENTF_KEYUP = 0x0002; //Key up flag
        //public const int QKEY = 0x10; // Key Q 
        //public const int WKEY = 0x11; // Key W 
        //public const int EKEY = 0x12; // Key E 
        //public const int SKEY = 0x1f; // Key S 
        //public const int SPACEBAR = 39; // space
        //public const int One = 0x02; // Key 1 
        //public const int Two = 0x03; // Key 2 
        //public const int Three = 0x04; // Key 3 
        //public const int Four = 0x05; // Key 4 
        //public const int Five = 0x06; // Key 5 
        //public const int Six = 0x07; // Key 6 
        //public const int Seven = 0x08; // Key 7 
        //public const int Eight = 0x09; // Key 8 
        //public const int Nine = 0x0a; // Key 9 
        //public const int F1 = 0x3b; // Key f1
        //public const int F2 = 0x3c; // Key f2
        //public const int F3 = 0x3d; // Key f3
        //public const int F4 = 0x3e; // Key f4
        //public const int F5 = 0x3f; // Key f5
        //public const int F6 = 0x40; // Key f6
        //public const int F7 = 0x41; // Key f7

        public DiscordSocketClient Client = new DiscordSocketClient();
        public IAudioClient audioClient;
        public IAudioChannel channel;
        private const string sIniLoc = @"Bot.ini";
        private const string sIniPixel = @"BotPixels.ini";
        private const string sIniUsers = @"BotUsers.ini";
        public string sAuthUsers { get; set; }
        public string sTriggerGroup { get; set; }
        public string sRebuffTarget { get; set; }
        private string sLogFile = string.Empty;
        public int PixelCount { get; set; }
        public int iTextTriggerCount { get; set; }

        public string sBotToken { get; set; }

        public BotConfig bcConfig { get; set; }

        public bool MonitorLogs { get; set; }


        private BackgroundWorker backgroundWorker1;
        private BackgroundWorker backgroundLogMonitor;


        Bitmap bmp = new Bitmap(1, 1);
        System.Drawing.Color GetColorAt(int x, int y)
        {
            Rectangle bounds = new Rectangle(x, y, 1, 1);
            using (Graphics g = Graphics.FromImage(bmp))
                g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
            return bmp.GetPixel(0, 0);
        }

        public BrokEQ()
        {
            // Background Process to do the Pixel Monitor
            backgroundWorker1 = new BackgroundWorker();
            backgroundWorker1.DoWork += new DoWorkEventHandler(CheckPixels);
            backgroundWorker1.WorkerSupportsCancellation = true;


            // Background Process to watch the Log File
            backgroundLogMonitor = new BackgroundWorker();
            backgroundLogMonitor.DoWork += new DoWorkEventHandler(MonitorLog);
            backgroundLogMonitor.WorkerSupportsCancellation = true;

            // Trigger to watch Discord Messages
            Client.MessageReceived += ClientOnMessageReceived;

            Client.LoginAsync(TokenType.Bot, "ODM2NzI3NTU3MDA0NzIyMTc2.YIiNQw.z6LPth0ZpexRzvzgGL1POkGL7_g");
            Task t1 =  Client.StartAsync();

            // The command's Run Mode MUST be set to RunMode.Async, otherwise, being connected to a voice channel will block the gateway thread.

            this.Show();

            try
            {
                wait(1000);
                channel = Client.GetChannel(836729415932575765) as IAudioChannel;
                
                
            }
            catch(Exception ex)
            {

            }

            InitializeComponent();

            ReloadSettings();
            
            this.sAuthUsers = this.tbPrioList.Text;

            //Task tt = JoinChannel();

            

        }

        private void ReloadSettings()
        {
            // Load Inis
            try
            {
                this.loadDataAsync(tbEQGameFolder.Text + @"\" + sIniLoc);
                this.loadPixelData(tbEQGameFolder.Text + @"\" + sIniPixel);
                this.LoadConfig(tbEQGameFolder.Text + @"\" + sIniUsers);

                this.sAuthUsers = tbPrioList.Text;
                iTextTriggerCount = getTextTriggerCount();
            }
            catch (Exception ex)
            {
                log.Info(ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        [Command("join", RunMode = RunMode.Async)]
        public async Task JoinChannel()
        {
            wait(1000);
            var channel = Client.GetChannel(836729415932575765) as IAudioChannel;
            
        }

        private async Task Brain(string sTrigger)
        {

            string[] saAction = sTrigger.Split(' ');
            List<Input> lInputs = new List<Input>();
            Input[] inputKeyboard = new Input[2];

            switch (saAction[0])
            {
                // CharmBreak TTS
                case "tts":
                    var iMC = Client.GetChannel(837075311036334082) as IMessageChannel;
                    StringBuilder sb = new StringBuilder();
                    for (int x1 = 1; x1 < saAction.Length; x1++)
                        sb.Append(saAction[x1] + " ");
                    await iMC.SendMessageAsync(sb.ToString(), true);
                    break;
                
                // POC for Sending Audio to Discord
                case "rafiki":
                    try
                    {
                        audioClient = await channel.ConnectAsync();
                        Task tt = SendAsync(audioClient, @"c:\Rafiki_you_fkn_suck.wav");
                    }
                    catch (Exception ex)
                    {
                        log.Info("Error: " + ex.Message + Environment.NewLine + ex.StackTrace);
                    }
                    break;
                // Rebuffing Target
                // rebuff <spellgem>
                // Target gotten from the Trigger line, buffme.spell <target> saved into Variable, sRebuffTarget
                case "rebuff":
                    try
                    {
                        focusEQ();
                        wait(250);
                        // Rebuff, What we rebuffin (saAction[1] holds the spell)
                        if (true)
                        {
                            if (sRebuffTarget.ToLower().Equals("you"))
                            {
                                // Target Outselves, send F1
                                Input[] iRebuffKeys = ParseAction("f1");
                                SendInput((uint)iRebuffKeys.Length, iRebuffKeys, Marshal.SizeOf(typeof(Input)));
                                wait(100);
                            }
                            else
                            {
                                // Allowed User
                                lInputs.Add(GenerateDownInput("slash"));
                                lInputs.Add(GenerateUpInput("slash"));

                                inputKeyboard = lInputs.ToArray();
                                SendInput((uint)inputKeyboard.Length, inputKeyboard, Marshal.SizeOf(typeof(Input)));
                                lInputs.Clear();

                                lInputs.Add(GenerateDownInput("t"));
                                lInputs.Add(GenerateUpInput("t"));

                                inputKeyboard = lInputs.ToArray();
                                SendInput((uint)inputKeyboard.Length, inputKeyboard, Marshal.SizeOf(typeof(Input)));
                                lInputs.Clear();

                                lInputs.Add(GenerateDownInput("a"));
                                lInputs.Add(GenerateUpInput("a"));

                                inputKeyboard = lInputs.ToArray();
                                SendInput((uint)inputKeyboard.Length, inputKeyboard, Marshal.SizeOf(typeof(Input)));
                                lInputs.Clear();
                                wait(100);

                                lInputs.Add(GenerateDownInput("r"));
                                lInputs.Add(GenerateUpInput("r"));

                                inputKeyboard = lInputs.ToArray();
                                SendInput((uint)inputKeyboard.Length, inputKeyboard, Marshal.SizeOf(typeof(Input)));
                                lInputs.Clear();

                                lInputs.Add(GenerateDownInput("space"));
                                lInputs.Add(GenerateUpInput("space"));

                                inputKeyboard = lInputs.ToArray();
                                SendInput((uint)inputKeyboard.Length, inputKeyboard, Marshal.SizeOf(typeof(Input)));
                                lInputs.Clear();
                                wait(100);

                                char[] charRebuffTarget = sRebuffTarget.ToCharArray();
                                foreach (char c in charRebuffTarget)
                                {
                                    if (c == '_')
                                    {
                                        lInputs.Add(GenerateDownInput("shiftright"));
                                        lInputs.Add(GenerateDownInput("minus"));

                                        inputKeyboard = lInputs.ToArray();
                                        SendInput((uint)inputKeyboard.Length, inputKeyboard, Marshal.SizeOf(typeof(Input)));
                                        lInputs.Clear();
                                        wait(100);

                                        lInputs.Add(GenerateUpInput("minus"));
                                        lInputs.Add(GenerateUpInput("shiftright"));

                                        inputKeyboard = lInputs.ToArray();
                                        SendInput((uint)inputKeyboard.Length, inputKeyboard, Marshal.SizeOf(typeof(Input)));
                                        lInputs.Clear();
                                        wait(50);

                                    }
                                    else
                                    {
                                        lInputs.Add(GenerateDownInput(c.ToString()));
                                        lInputs.Add(GenerateUpInput(c.ToString()));

                                        inputKeyboard = lInputs.ToArray();
                                        SendInput((uint)inputKeyboard.Length, inputKeyboard, Marshal.SizeOf(typeof(Input)));
                                        lInputs.Clear();
                                        wait(75);
                                    }

                                }

                                lInputs.Add(GenerateDownInput("enter"));
                                lInputs.Add(GenerateUpInput("enter"));

                                inputKeyboard = lInputs.ToArray();
                                SendInput((uint)inputKeyboard.Length, inputKeyboard, Marshal.SizeOf(typeof(Input)));
                                lInputs.Clear();
                            }
                            wait(750);

                            //Input[] inputKeyboard = lInputs.ToArray();
                            Input[] iKeyPresses = ParseAction("Alt+D" + saAction[1]);

                            SendInput((uint)iKeyPresses.Length, iKeyPresses, Marshal.SizeOf(typeof(Input)));
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Info("Error: " + ex.Message + Environment.NewLine + ex.StackTrace);
                    }
                    break;
                // Custom Recharm Function, on charm  break, if the action is recharm, the bot recharms the saved target (must be used in conjuction with the charmed trigger/action)
                case "recharm":
                    try
                    {
                        if (!focusEQ())
                            break;
                        Input[] iKeyPresses = ParseAction("Alt+D5");
                        //SendInput((uint)iKeyPresses.Length, iKeyPresses, Marshal.SizeOf(typeof(Input)));
                        //wait(500);

                        char[] mobChar = sCharmedMobName.ToCharArray();


                        // We should have a pet name saved
                        if (sCharmedMobName is not null && sCharmedMobName != string.Empty)
                        {
                            // We got a charmed mob saved. lets target it and recharm

                            // /tar sCharmedMobName
                            lInputs.Add(GenerateDownInput("slash"));
                            lInputs.Add(GenerateUpInput("slash"));

                            inputKeyboard = lInputs.ToArray();
                            SendInput((uint)inputKeyboard.Length, inputKeyboard, Marshal.SizeOf(typeof(Input)));
                            lInputs.Clear();

                            lInputs.Add(GenerateDownInput("t"));
                            lInputs.Add(GenerateUpInput("t"));

                            inputKeyboard = lInputs.ToArray();
                            SendInput((uint)inputKeyboard.Length, inputKeyboard, Marshal.SizeOf(typeof(Input)));
                            lInputs.Clear();

                            lInputs.Add(GenerateDownInput("a"));
                            lInputs.Add(GenerateUpInput("a"));

                            inputKeyboard = lInputs.ToArray();
                            SendInput((uint)inputKeyboard.Length, inputKeyboard, Marshal.SizeOf(typeof(Input)));
                            lInputs.Clear();
                            wait(100);

                            lInputs.Add(GenerateDownInput("r"));
                            lInputs.Add(GenerateUpInput("r"));

                            inputKeyboard = lInputs.ToArray();
                            SendInput((uint)inputKeyboard.Length, inputKeyboard, Marshal.SizeOf(typeof(Input)));
                            lInputs.Clear();

                            lInputs.Add(GenerateDownInput("space"));
                            lInputs.Add(GenerateUpInput("space"));

                            inputKeyboard = lInputs.ToArray();
                            SendInput((uint)inputKeyboard.Length, inputKeyboard, Marshal.SizeOf(typeof(Input)));
                            lInputs.Clear();
                            foreach (char c in mobChar)
                            {
                                if (c == '_')
                                {
                                    lInputs.Add(GenerateDownInput("shiftright"));
                                    lInputs.Add(GenerateDownInput("minus"));

                                    inputKeyboard = lInputs.ToArray();
                                    SendInput((uint)inputKeyboard.Length, inputKeyboard, Marshal.SizeOf(typeof(Input)));
                                    lInputs.Clear();
                                    wait(100);

                                    lInputs.Add(GenerateUpInput("minus"));
                                    lInputs.Add(GenerateUpInput("shiftright"));

                                    inputKeyboard = lInputs.ToArray();
                                    SendInput((uint)inputKeyboard.Length, inputKeyboard, Marshal.SizeOf(typeof(Input)));
                                    lInputs.Clear();
                                    wait(50);

                                }
                                else
                                {
                                    lInputs.Add(GenerateDownInput(c.ToString()));
                                    lInputs.Add(GenerateUpInput(c.ToString()));

                                    inputKeyboard = lInputs.ToArray();
                                    SendInput((uint)inputKeyboard.Length, inputKeyboard, Marshal.SizeOf(typeof(Input)));
                                    lInputs.Clear();
                                    wait(50);
                                }

                            }

                            lInputs.Add(GenerateDownInput("enter"));
                            lInputs.Add(GenerateUpInput("enter"));

                            inputKeyboard = lInputs.ToArray();
                            SendInput((uint)inputKeyboard.Length, inputKeyboard, Marshal.SizeOf(typeof(Input)));
                            lInputs.Clear();
                            wait(1000);

                            //Input[] inputKeyboard = lInputs.ToArray();

                            iKeyPresses = ParseAction("Alt+D9");

                            SendInput((uint)iKeyPresses.Length, iKeyPresses, Marshal.SizeOf(typeof(Input)));


                        }
                    }
                    catch(Exception ex)
                    {
                        log.Info("Error: " + ex.Message + Environment.NewLine + ex.StackTrace);
                    }
                    break;
                // Ideas for more Actions:
                // Keystrokes, just sending a keystroke/string of keys
                // Trigger would be: keystroke: <X>
                // Other possibilities
                // Spellgem:  Just send a spellgem
                case "keystroke":
                    try
                    {
                        // First word in the Action 
                        if(saAction[1].Contains(","))
                        {
                            Input[] inKey = new Input[1];
                            // We have multiple keystrokes, lets send em
                            foreach(string s in saAction[1].Split(','))
                            {    
                                inKey = ParseAction(s);
                                SendInput((uint)inKey.Length, inKey, Marshal.SizeOf(typeof(Input)));
                            }
                        }
                        else
                        {
                            // We have a single Keystroke, lets send it
                            Input[] iRebuffKeys = ParseAction(saAction[1]);
                            SendInput((uint)iRebuffKeys.Length, iRebuffKeys, Marshal.SizeOf(typeof(Input)));
                        }
                        
                    }
                    catch(Exception ex)
                    {
                        log.Info("Error: " + ex.Message + Environment.NewLine + ex.StackTrace);
                    }
                    break;
                default:
                    break;
            }
        }


        private async Task ClientOnMessageReceived(SocketMessage socketMessage)
        {
            await Task.Run(() =>
            {

                if (!this.tbDiscordChannel.Text.Contains(socketMessage.Channel.Id.ToString()))
                    return;

                if (!(socketMessage is SocketUserMessage msg))
                    return;

                //var UNick = (socketMessage.Author as SocketGuildUser).Nickname;

                // Ensure its from an Author we trust
                if (!sAuthUsers.ToLower().Contains(socketMessage.Author.ToString().ToLower()))
                    return;

                for (int iTextTriggerCnt = 1; iTextTriggerCnt <= iTextTriggerCount; iTextTriggerCnt++)
                {

                    TextBox tbTmpT = this.Controls.Find("Trigger" + iTextTriggerCnt, true).First() as TextBox;
                    TextBox tbTmpA = this.Controls.Find("Action" + iTextTriggerCnt, true).First() as TextBox;

                    Regex rTmpReg = new(tbTmpT.Text);
                    Match mReg = rTmpReg.Match(socketMessage.Content);

                    if (mReg.Success)
                    {
                        if (tbTmpA.Text.ToLower().Contains("charmed"))
                        {
                            // Save Charmed Mob
                            this.sCharmedMobName = mReg.Value.Remove(0, mReg.Value.LastIndexOf(']') + 1).Trim().Replace(" ", "_");

                        }
                        else if (tbTmpA.Text.ToLower().Contains("rebuff"))
                        {
                            // Use Regex to Get the Requestor
                            // [Mon May 03 10:16:27 2021] You tell your party, 'buffme.clarity'
                            // \w+(?=\s+(tell|tells) your party)
                            // Regex to match the target:  (?<=\b(buffme.clarity|buffme.haste)\s)(\w+)  buffme.clarity|buffme.haste can be looped through all the action triggers as to not hardcode
                            Regex regParseTarget = new Regex(@"(?<=\b(" + this.sTriggerGroup + @")\s)(\w+)");
                            Match mTarget = regParseTarget.Match(socketMessage.Content);
                            string sasdasd = mTarget.Value;

                            this.sRebuffTarget = mTarget.Value;



                        }
                        Task tBrain = Brain(tbTmpA.Text.ToLower());
                    }
                }
                    
            });
        }

        public void loadDataAsync(string sLoc)
        {
            string sActions, sTriggers;
            StringBuilder sb = new StringBuilder();

            FileInfo fi = new FileInfo(sLoc);
            using (TextReader txtWriter = new StreamReader(fi.Open(FileMode.Open)))

            {
                // Actions First Line
                sActions = txtWriter.ReadLine();

                // Triggers second
                sTriggers = txtWriter.ReadLine();
            }

            if (sActions is null || sTriggers is null)
                return;

            string[] saActions = sActions.Split('|');
            string[] saTriggers = sTriggers.Split('|');

            for (int x = 1; x < saActions.Length; x++)
            {
                TextBox tbTmpPC = this.Controls.Find("Action" + x.ToString(), true).First() as TextBox;
                TextBox tbTmpPL = this.Controls.Find("Trigger" + x.ToString(), true).First() as TextBox;

                tbTmpPC.Text = saActions[x-1];
                tbTmpPL.Text = saTriggers[x-1];

                sb.Append(tbTmpPL.Text + "|");
            }

            this.sTriggerGroup = sb.ToString();
        }

        public void LoadConfig(string sIni)
        {
            FileInfo fi = new FileInfo(sIni);
            using (TextReader txtWriter = new StreamReader(fi.Open(FileMode.Open)))
            {
                // First line is  Auth Users
                string s = txtWriter.ReadLine();
                this.tbPrioList.Text = s.Replace("|", Environment.NewLine);

                this.tbDiscordChannel.Text = txtWriter.ReadLine();
            }
        }

        public async void loadPixelData(string sIni)
        {

            string sPixelData = string.Empty;

            FileInfo fi = new FileInfo(sIni);
            using (TextReader txtWriter = new StreamReader(fi.Open(FileMode.Open)))
            {
                while (txtWriter.Peek() > 1)
                {
                    // Reads First Pixel Line
                    sPixelData = txtWriter.ReadLine();


                    if (sPixelData is null)
                        return;


                    // Data PixelNumber|PixelColor|PixelLocation|PixelAction
                    string[] saTriggerData = sPixelData.Split('|');

                    TextBox tbTmpPC = this.Controls.Find("PixelColor" + saTriggerData[0].ToString(), true).First() as TextBox;
                    TextBox tbTmpPL = this.Controls.Find("PixelLocation" + saTriggerData[0].ToString(), true).First() as TextBox;
                    TextBox tbTmpPA = this.Controls.Find("PixelAction" + saTriggerData[0].ToString(), true).First() as TextBox;

                    tbTmpPC.Text = saTriggerData[1];
                    tbTmpPL.Text = saTriggerData[2];
                    tbTmpPA.Text = saTriggerData[3];

                }
            }
        }

        private void MonitorLog(object sender, EventArgs e)
        {            
            // Note the FileShare.ReadWrite, allowing others to modify the file
            using (FileStream fileStream = File.Open(sLogFile, FileMode.Open,
                FileAccess.Read, FileShare.ReadWrite))
            {
                fileStream.Seek(0, SeekOrigin.End);
                using (StreamReader streamReader = new StreamReader(fileStream))
                {
                    for (; ; )
                    {
                        if (!MonitorLogs)
                            break;
                        // Every half a second, re-read the log file
                        Thread.Sleep(TimeSpan.FromSeconds(.5));

                        // If you want newlines, search the return value of "ReadToEnd"
                        // for Environment.NewLine.

                        // Come up with a better way, we still get empty lines read, work around is checking if its null/empty and skipping, possible waste of cpu time
                        string[] sLogLines = streamReader.ReadToEnd().Trim().Split(Environment.NewLine.ToCharArray());
                        foreach (string str in sLogLines)
                        {
                            if (str is null || str == string.Empty)
                                continue;

                            for(int iTextTriggerCnt = 1; iTextTriggerCnt <= iTextTriggerCount; iTextTriggerCnt++)
                            {
                                
                                TextBox tbTmpT = this.Controls.Find("Trigger" + iTextTriggerCnt, true).First() as TextBox;
                                TextBox tbTmpA = this.Controls.Find("Action" + iTextTriggerCnt, true).First() as TextBox;

                                Regex rTmpReg = new(tbTmpT.Text);
                                Match mReg = rTmpReg.Match(str);

                                if (mReg.Success)
                                {
                                    if (tbTmpA.Text.ToLower().Contains("charmed"))
                                    {
                                        // Save Charmed Mob
                                        this.sCharmedMobName = mReg.Value.Remove(0, mReg.Value.LastIndexOf(']') + 1).Trim().Replace(" ", "_");

                                    }
                                    // Rebuff handled via Discord
                                    else if (tbTmpA.Text.ToLower().Contains("rebuff"))
                                    {
                                        // Use Regex to Get the Requestor
                                        // [Mon May 03 10:16:27 2021] You tell your party, 'buffme.clarity'
                                        // \w+(?=\s+(tell|tells) your party)
                                        Regex rTargetReg = new(@"\w+(?=\s+(tell|tells) (your party|you))");
                                        Match mTarget = rTargetReg.Match(str);
                                        this.sRebuffTarget = mTarget.Value;


                                    }
                                    Task tBrain = Brain(tbTmpA.Text.ToLower());
                                }
                            }
                            
                        }



                    }
                }
            }
        }

        public void DoMouseDown(int X, int Y)
        {
            //Call the imported function with the cursor's current position
            mouse_event(MOUSEEVENTF_LEFTDOWN, X, Y, 0, 0);
        }


        public void DoMouseUp(int X, int Y)
        {
            //Call the imported function with the cursor's current position
            mouse_event(MOUSEEVENTF_LEFTUP, X, Y, 0, 0);
        }

        public void wait(int milliseconds) // It will wait number of miliseconds. 
        {
            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds <= milliseconds)
            {
                Application.DoEvents();
            }
        }

        private bool focusEQ() // Focus world of warcraft windows
        {
            var prc = Process.GetProcessesByName("eqgame");
            if (prc.Length > 0)
            {
                SetForegroundWindow(prc[0].MainWindowHandle);
                return true;
            }
            else
                return false;

        }

        // envia Key para o wow 
        void SendKeyPress(byte key, int time = 50) // it will send coded key, and keep it pressed number of miliseconds in argument 2. 50miliseconds will be default. 
        {
            focusEQ(); // this is the focust window method above
            if (time != 2) keybd_event(key, 0, KEYEVENTF_EXTENDEDKEY, 0);
            if (time != 2) wait(time); // this is wait method above 
            if (time > 0) keybd_event(key, 0, KEYEVENTF_KEYUP, 0); // solta a Key
        }

        static IEnumerable<string> TailFrom(string file)
        {
            using (var reader = File.OpenText(file))
            {
                while (true)
                {
                    string line = reader.ReadLine();
                    if (reader.BaseStream.Length < reader.BaseStream.Position)
                        reader.BaseStream.Seek(0, SeekOrigin.Begin);

                    if (line != null) yield return line;
                    else Thread.Sleep(500);
                }
            }
        }


        private void CheckCommands(string sLogItem)
        {
            if(sLogItem.ToLower().Contains("chme"))
            {

                s.Keyboard.KeyDown(VirtualKeyCode.MENU);
                Thread.Sleep(50);
                s.Keyboard.KeyPress(VirtualKeyCode.VK_1);
                Thread.Sleep(50);
                s.Keyboard.KeyUp(VirtualKeyCode.MENU);
                Thread.Sleep(50);
            }

        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            openFileDialog1.ShowDialog();

            sLogFile = openFileDialog1.FileName;
            this.tbEQGameFolder.Text = System.IO.Path.GetDirectoryName(openFileDialog1.FileName);

            ReloadSettings();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            MonitorLogs = true;
            if (!backgroundLogMonitor.IsBusy)
                backgroundLogMonitor.RunWorkerAsync();            
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            StringBuilder sbActions = new StringBuilder();
            sbActions.Append(this.Action1.Text + "|" +
                this.Action2.Text + "|" +
                this.Action3.Text + "|" +
                this.Action4.Text + "|" +
                this.Action5.Text + "|" +
                this.Action6.Text + "|" +
                this.Action7.Text + "|" +
                this.Action8.Text + "|" +
                this.Action9.Text + "|" +
                this.Action10.Text);

            StringBuilder sbTriggers = new StringBuilder();
            sbTriggers.Append(this.Trigger1.Text + "|" +
                this.Trigger2.Text + "|" +
                this.Trigger3.Text + "|" +
                this.Trigger4.Text + "|" +
                this.Trigger5.Text + "|" +
                this.Trigger6.Text + "|" +
                this.Trigger7.Text + "|" +
                this.Trigger8.Text + "|" +
                this.Trigger9.Text + "|" +
                this.Trigger10.Text);

            await SaveData(sbActions.ToString(), sbTriggers.ToString());

        }

        private async Task SaveData(string sActions, string sTriggers)
        {

            FileInfo fi = new FileInfo(tbEQGameFolder.Text + @"\" + sIniLoc);
            using (TextWriter txtWriter = new StreamWriter(fi.Open(FileMode.Truncate)))

            {

                await txtWriter.WriteAsync(sActions + Environment.NewLine);

                await txtWriter.WriteAsync(sTriggers + Environment.NewLine);
            }
            
        }

        private Process CreateStream(string path)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });
        }

        private async Task SendAsync(IAudioClient client, string path)
        {
            // Create FFmpeg using the previous example
            using (var ffmpeg = CreateStream(path))
            using (var output = ffmpeg.StandardOutput.BaseStream)
            using (var discord = client.CreatePCMStream(AudioApplication.Mixed))
            {
                try { await output.CopyToAsync(discord); }
                finally { await discord.FlushAsync(); }
            }
        }

        /// <summary>
        /// Function to set the Pixels
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click(object sender, EventArgs e)
        {
            //Cursor.Position.X = 1058;
            //Cursor.Position.Y = 745;

            this.Cursor = new Cursor(Cursor.Current.Handle);
            Cursor.Position = new Point(335, 947);
            int iCountPixels = 1;

            try
            {
                for (; ; )
                {
                    if (GetAsyncKeyState(Keys.D0) != 0)
                    {
                        System.Drawing.Color x = GetColorAt((int)(Cursor.Position.X * 1.5), (int)(Cursor.Position.Y * 1.5));
                        string colorLogger = string.Format("{0}|{1}|{2}", x.Name.Remove(0, 2), Cursor.Position.X, Cursor.Position.Y);
                        log.Info(colorLogger);
                        wait(100);

                        FileInfo fi = new FileInfo(sIniPixel);
                        using (TextWriter txtWriter = new StreamWriter(fi.Open(FileMode.Append, FileAccess.Write, FileShare.Read)))
                        {

                            Task tWrite = txtWriter.WriteAsync("Pixel" + iCountPixels.ToString() + "|" + colorLogger + Environment.NewLine);


                        }
                        TextBox tbTmpPC = this.Controls.Find("PixelColor" + iCountPixels, true).First() as TextBox;
                        TextBox tbTmpPL = this.Controls.Find("PixelLocation" + iCountPixels, true).First() as TextBox;

                        tbTmpPC.Text = x.Name.Remove(0, 2);
                        tbTmpPL.Text = Cursor.Position.X.ToString() + ", " + Cursor.Position.Y.ToString();

                        iCountPixels++;
                        wait(100);

                    }
                    if (GetAsyncKeyState(Keys.C) != 0)
                    {
                        this.TestPixels();
                    }

                }
            }
            catch(Exception ex)
            {
                return;
            }

        }

        private Input GenerateDownInput(string sKeyCommand)
        {
            
            Scancode scKey = (Scancode)System.Enum.Parse(typeof(Scancode), sKeyCommand.ToLower());

            Input iReturnInput = new Input
            {
                type = (int)InputType.Keyboard,
                u = new InputUnion
                {
                    ki = new KeyboardInput
                    {
                        wVk = 0,
                        wScan = (ushort)scKey, // 1
                        dwFlags = (uint)(KeyEventF.KeyDown | KeyEventF.Scancode),
                        dwExtraInfo = GetMessageExtraInfo()
                    }
                }
            };

            return iReturnInput;
        }

        private Input GenerateUpInput(string sKeyCommand)
        {

            Scancode scKey = (Scancode)System.Enum.Parse(typeof(Scancode), sKeyCommand.ToLower());

            Input iReturnInput = new Input
            {
                type = (int)InputType.Keyboard,
                u = new InputUnion
                {
                    ki = new KeyboardInput
                    {
                        wVk = 0,
                        wScan = (ushort)scKey, // 1
                        dwFlags = (uint)(KeyEventF.KeyUp | KeyEventF.Scancode),
                        dwExtraInfo = GetMessageExtraInfo()
                    }
                }
            };

            return iReturnInput;
        }


        //Input[] iKeyPresses = new Input[]
        //{
        //    new Input
        //    {
        //        type = (int)InputType.Keyboard,
        //        u = new InputUnion
        //        {
        //            ki = new KeyboardInput
        //            {
        //                wVk = 0,
        //                wScan = (ushort)Scancode.One, // 1
        //                dwFlags = (uint)(KeyEventF.KeyDown | KeyEventF.Scancode),
        //                dwExtraInfo = GetMessageExtraInfo()
        //            }
        //        }
        //    },
        //    new Input
        //    {
        //        type = (int)InputType.Keyboard,
        //        u = new InputUnion
        //        {
        //            ki = new KeyboardInput
        //            {
        //                wVk = 0,
        //                wScan = (ushort)Scancode.One, // 1
        //                dwFlags = (uint)(KeyEventF.KeyUp | KeyEventF.Scancode),
        //                dwExtraInfo = GetMessageExtraInfo()
        //            }
        //        }
        //    }
        //};
        private Input[] ParseAction(string sActionText)
        {
            string sModifier = string.Empty;
            Input modDown = new Input();
            Input modUp = new Input();

            /// Key Combo
            if(sActionText.Contains("+"))
            {
                // Key Combo, lets get the first key (Modifier, Shift, Alt, Control)
                switch(sActionText.Substring(0, sActionText.IndexOf("+")).ToLower())
                {
                    case "shift":
                        modDown = new Input
                        {
                            type = (int)InputType.Keyboard,
                            u = new InputUnion
                            {
                                ki = new KeyboardInput
                                {
                                    wVk = 0,
                                    wScan = (ushort)Scancode.shiftLeft,
                                    dwFlags = (uint)(KeyEventF.KeyDown | KeyEventF.Scancode),
                                    dwExtraInfo = GetMessageExtraInfo()
                                }
                            }
                        };
                        
                        modUp = new Input
                        {
                            type = (int)InputType.Keyboard,
                            u = new InputUnion
                            {
                                ki = new KeyboardInput
                                {
                                    wVk = 0,
                                    wScan = (ushort)Scancode.shiftLeft,
                                    dwFlags = (uint)(KeyEventF.KeyUp | KeyEventF.Scancode),
                                    dwExtraInfo = GetMessageExtraInfo()
                                }
                            }
                        };

                        sModifier = "shift";
                        break;
                    case "alt":
                        modDown = new Input
                        {
                            type = (int)InputType.Keyboard,
                            u = new InputUnion
                            {
                                ki = new KeyboardInput
                                {
                                    wVk = 0,
                                    wScan = (ushort)Scancode.altLeft,
                                    dwFlags = (uint)(KeyEventF.KeyDown | KeyEventF.Scancode),
                                    dwExtraInfo = GetMessageExtraInfo()
                                }
                            }
                        };

                        modUp = new Input
                        {
                            type = (int)InputType.Keyboard,
                            u = new InputUnion
                            {
                                ki = new KeyboardInput
                                {
                                    wVk = 0,
                                    wScan = (ushort)Scancode.altLeft,
                                    dwFlags = (uint)(KeyEventF.KeyUp | KeyEventF.Scancode),
                                    dwExtraInfo = GetMessageExtraInfo()
                                }
                            }
                        };
                        sModifier = "alt";
                        break;
                    case "ctrl":
                    case "control":
                        modDown = new Input
                        {
                            type = (int)InputType.Keyboard,
                            u = new InputUnion
                            {
                                ki = new KeyboardInput
                                {
                                    wVk = 0,
                                    wScan = (ushort)Scancode.controlLeft,
                                    dwFlags = (uint)(KeyEventF.KeyDown | KeyEventF.Scancode),
                                    dwExtraInfo = GetMessageExtraInfo()
                                }
                            }
                        };

                        modUp = new Input
                        {
                            type = (int)InputType.Keyboard,
                            u = new InputUnion
                            {
                                ki = new KeyboardInput
                                {
                                    wVk = 0,
                                    wScan = (ushort)Scancode.controlLeft,
                                    dwFlags = (uint)(KeyEventF.KeyUp | KeyEventF.Scancode),
                                    dwExtraInfo = GetMessageExtraInfo()
                                }
                            }
                        };
                        sModifier = "ctrl";
                        break;
                    default:
                        break;
                }
            }

            Input[] iKeyPresses = new Input[1];

            // Time to add the Key
            string sKey = sActionText.Substring(sActionText.IndexOf("+")+1 );

            Scancode scKey = (Scancode)System.Enum.Parse(typeof(Scancode), sKey.ToLower());

            // Lets add the Modifier and the Action Key to the Input Array

            if(sModifier != string.Empty)
            {
                iKeyPresses = new Input[]
                {
                    modDown,
                    new Input
                    {
                        type = (int)InputType.Keyboard,
                        u = new InputUnion
                        {
                            ki = new KeyboardInput
                            {
                                wVk = 0,
                                wScan = (ushort)scKey,
                                dwFlags = (uint)(KeyEventF.KeyDown | KeyEventF.Scancode),
                                dwExtraInfo = GetMessageExtraInfo()
                            }
                        }
                    },
                    new Input
                    {
                        type = (int)InputType.Keyboard,
                        u = new InputUnion
                        {
                            ki = new KeyboardInput
                            {
                                wVk = 0,
                                wScan = (ushort)scKey, 
                                dwFlags = (uint)(KeyEventF.KeyUp | KeyEventF.Scancode),
                                dwExtraInfo = GetMessageExtraInfo()
                            }
                        }
                    },
                    modUp
                };

            }
            else
            {
                // No Modifier, lets just send the keystroke
                iKeyPresses = new Input[]
                {
                    new Input
                    {
                        type = (int)InputType.Keyboard,
                        u = new InputUnion
                        {
                            ki = new KeyboardInput
                            {
                                wVk = 0,
                                wScan = (ushort)scKey,
                                dwFlags = (uint)(KeyEventF.KeyDown | KeyEventF.Scancode),
                                dwExtraInfo = GetMessageExtraInfo()
                            }
                        }
                    },
                    new Input
                    {
                        type = (int)InputType.Keyboard,
                        u = new InputUnion
                        {
                            ki = new KeyboardInput
                            {
                                wVk = 0,
                                wScan = (ushort)scKey,
                                dwFlags = (uint)(KeyEventF.KeyUp | KeyEventF.Scancode),
                                dwExtraInfo = GetMessageExtraInfo()
                            }
                        }
                    }
                };
            }

            //new Input
            //{
            //    type = (int)InputType.Mouse,
            //    u = new InputUnion
            //    {
            //        mi = new MouseInput
            //        {
                        
            //        }
            //    }
            //}

            return iKeyPresses;

        }

        public Input[] DoMouseClick(int button = 1) // argument is button, 1 or 2, 1 is default
        {
            return
                new Input[]
                {
                    new Input
                    {
                        type = (int) InputType.Mouse,
                        u = new InputUnion
                        {
                            mi = new MouseInput
                            {
                                dwFlags = (uint)MouseEventF.LeftDown,
                                dwExtraInfo = GetMessageExtraInfo()
                            }
                        }
                    },
                    new Input
                    {
                        type = (int) InputType.Mouse,
                        u = new InputUnion
                        {
                            mi = new MouseInput
                            {
                                dwFlags = (uint)MouseEventF.LeftUp,
                                dwExtraInfo = GetMessageExtraInfo()
                            }
                        }
                    }
                };
        }

        private void TestPixels()
        {

            // Loop through the Pixels
            for (int x = 1; x <= PixelCount; x++)
            {
                TextBox tbTmpPC = this.Controls.Find("PixelColor" + x.ToString(), true).First() as TextBox;
                TextBox tbTmpPL = this.Controls.Find("PixelLocation" + x.ToString(), true).First() as TextBox;
                TextBox tbTmpPA = this.Controls.Find("PixelAction" + x.ToString(), true).First() as TextBox;

                // If no Action, skip checking it
                if (tbTmpPA.Text == string.Empty)
                    break;

                string[] sPixel = tbTmpPL.Text.Split(',');

                System.Drawing.Color cPixelColor = GetColorAt((int)(int.Parse(sPixel[0]) * 1.5), (int)(int.Parse(sPixel[1]) * 1.5));

                    Cursor.Position = new Point((int)(int.Parse(sPixel[0])), (int)(int.Parse(sPixel[1])));
                    wait(500);
                    Input[] iMouseClick = DoMouseClick(1);
                    wait(500);
                }

            }


            private void CheckPixels(object sender, EventArgs e)
        {
            focusEQ();
            wait(300);

            for (; ; )
            {
                // Loop through the Pixels
                for (int x = 1; x <= PixelCount; x++)
                {
                    TextBox tbTmpPC = this.Controls.Find("PixelColor" + x.ToString(), true).First() as TextBox;
                    TextBox tbTmpPL = this.Controls.Find("PixelLocation" + x.ToString(), true).First() as TextBox;
                    TextBox tbTmpPA = this.Controls.Find("PixelAction" + x.ToString(), true).First() as TextBox;
                    
                    // If no Action, skip checking it
                    if (tbTmpPA.Text == string.Empty)
                        break;

                    string[] sPixel = tbTmpPL.Text.Split(',');

                    System.Drawing.Color cPixelColor = GetColorAt((int)(int.Parse(sPixel[0]) * 1.5), (int)(int.Parse(sPixel[1]) * 1.5));

                    if (!cPixelColor.Name.Remove(0, 2).Equals(tbTmpPC.Text))
                    {
                        // Pixel is Different, Do the Action


                        Input[] iKeyPresses = ParseAction(tbTmpPA.Text);

                        focusEQ();
                        wait(200);

                        // Put the Cursor on the pixel, and click
                        log.Info(string.Format("Cursor Moved to: {0}, {1}.", sPixel[0], sPixel[1]));
                        Cursor.Position = new Point((int)(int.Parse(sPixel[0])), (int)(int.Parse(sPixel[1])));
                        wait(500);
                        Input[] iMouseClick = DoMouseClick(1);
                        //SendInput((uint)iMouseClick.Length, iMouseClick, Marshal.SizeOf(typeof(Input)));
                        //SendInput((uint)iMouseClick.Length, iMouseClick, Marshal.SizeOf(typeof(Input)));
                        DoMouseDown((int)(int.Parse(sPixel[0])), (int)(int.Parse(sPixel[1])));
                        wait(100);
                        DoMouseUp((int)(int.Parse(sPixel[0])), (int)(int.Parse(sPixel[1])));
                        wait(750);


                        SendInput((uint)iKeyPresses.Length, iKeyPresses, Marshal.SizeOf(typeof(Input)));
                        wait(200);
                    }

                }
                Thread.Sleep(2000);
            }
        }

        private int getTextTriggerCount()
        {
            int iControCount = 0;
            try
            {
                for (int iLooper = 1; iLooper < 20; iLooper++)
                {
                    // Get the Textbox
                    TextBox tbTmpPC = this.Controls.Find("Trigger" + iLooper.ToString(), true).First() as TextBox;

                    // check if its null and has text
                    if (tbTmpPC is not null)
                        if (tbTmpPC.Text != string.Empty)
                            iControCount++;

                }
            }
            catch (Exception ex)
            {
                // Have an Exception, means we reached the end
                return iControCount;
            }
            return iControCount;
        }

        private int getPixelTriggerCount()
        {
            int iControCount = 0;
            try
            {
                for(int iLooper = 1; iLooper < 20; iLooper++)
                {
                    // Get the Textbox
                    TextBox tbTmpPC = this.Controls.Find("PixelColor" + iLooper.ToString(), true).First() as TextBox;

                    // check if its null and has text
                    if (tbTmpPC is not null)
                        if (tbTmpPC.Text != string.Empty)
                            iControCount++;

                }
            }
            catch(Exception ex)
            {
                // Have an Exception, means we reached the end
                return iControCount;
            }
            return iControCount;
        }

        private async Task SavePixelData()
        {
            string sPixelStuff = "";

            FileInfo fi = new FileInfo(sIniPixel);
            using (TextWriter txtWriter = new StreamWriter(fi.Open(FileMode.Truncate)))

            {

                for (int x = 1; x <= PixelCount; x++)
                {
                    // For all the pixels lets save the Stuff

                    // Get out Pixel Stuff
                    TextBox tbTmpPC = this.Controls.Find("PixelColor" + x.ToString(), true).First() as TextBox;
                    TextBox tbTmpPL = this.Controls.Find("PixelLocation" + x.ToString(), true).First() as TextBox;
                    TextBox tbTmpPA = this.Controls.Find("PixelAction" + x.ToString(), true).First() as TextBox;

                    sPixelStuff = x.ToString() + "|" + tbTmpPC.Text + "|" + tbTmpPL.Text + "|" + tbTmpPA.Text;

                    await txtWriter.WriteAsync(sPixelStuff + Environment.NewLine);

                }
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            // Save the Pixels to File, first lets see how many Pixels are set
            this.PixelCount = getPixelTriggerCount();

            // Now lets build the ini file
            Task tSaveData = SavePixelData();


            backgroundWorker1.RunWorkerAsync();
        }

       

        private void PixelAction1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Shift || e.KeyCode != Keys.Control || e.KeyCode != Keys.Alt)
                this.PixelAction1.Text = (e.Modifiers.ToString() == "None" ? "" : (e.Modifiers.ToString() + "+") + e.KeyCode.ToString());
        }

        private void PixelAction2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Shift || e.KeyCode != Keys.Control || e.KeyCode != Keys.Alt)
                this.PixelAction2.Text = (e.Modifiers.ToString() == "None" ? "" : (e.Modifiers.ToString() + "+") + e.KeyCode.ToString());
        }
        private void PixelAction3_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Shift || e.KeyCode != Keys.Control || e.KeyCode != Keys.Alt)
                this.PixelAction3.Text = (e.Modifiers.ToString() == "None" ? "" : (e.Modifiers.ToString() + "+") + e.KeyCode.ToString());
        }
        private void PixelAction4_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Shift || e.KeyCode != Keys.Control || e.KeyCode != Keys.Alt)
                this.PixelAction4.Text = (e.Modifiers.ToString() == "None" ? "" : (e.Modifiers.ToString() + "+") + e.KeyCode.ToString());
        }
        private void PixelAction5_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Shift || e.KeyCode != Keys.Control || e.KeyCode != Keys.Alt)
                this.PixelAction5.Text = (e.Modifiers.ToString() == "None" ? "" : (e.Modifiers.ToString() + "+") + e.KeyCode.ToString());
        }
        private void PixelAction6_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Shift || e.KeyCode != Keys.Control || e.KeyCode != Keys.Alt)
                this.PixelAction6.Text = (e.Modifiers.ToString() == "None" ? "" : (e.Modifiers.ToString() + "+") + e.KeyCode.ToString());
        }
        private void PixelAction7_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Shift || e.KeyCode != Keys.Control || e.KeyCode != Keys.Alt)
                this.PixelAction7.Text = (e.Modifiers.ToString() == "None" ? "" : (e.Modifiers.ToString() + "+") + e.KeyCode.ToString());
        }
        private void PixelAction8_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Shift || e.KeyCode != Keys.Control || e.KeyCode != Keys.Alt)
                this.PixelAction8.Text = (e.Modifiers.ToString() == "None" ? "" : (e.Modifiers.ToString() + "+") + e.KeyCode.ToString());
        }
        private void PixelAction9_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Shift || e.KeyCode != Keys.Control || e.KeyCode != Keys.Alt)
                this.PixelAction9.Text = (e.Modifiers.ToString() == "None" ? "" : (e.Modifiers.ToString() + "+") + e.KeyCode.ToString());
        }

        private void btnSaveConfig_Click(object sender, EventArgs e)
        {
            
            FileInfo fi = new FileInfo(tbEQGameFolder.Text + @"\" + sIniUsers);
            using (TextWriter txtWriter = new StreamWriter(fi.Open(FileMode.Truncate)))
            {
                // Write To the Log

                txtWriter.WriteLine(this.tbPrioList.Text.Replace(Environment.NewLine, "|"));
                txtWriter.WriteLine(this.tbDiscordChannel.Text);
                txtWriter.WriteLine(this.tbEQGameFolder.Text);
            }

            /*
             * Trying to use a Single Config File
             *  
            this.bcConfig.AuthorizedUsers = this.tbPrioList.Text.Replace(Environment.NewLine, "|");
            this.bcConfig.DiscordListenerChannel = this.tbDiscordChannel.Text;
            this.bcConfig.DiscordAudioChannel = this.tbDiscordAudioChannel.Text;
            this.bcConfig.EQLogDirectory = this.tbEQGameFolder.Text;
            this.bcConfig.Triggers = sTriggerGroup;
            */
        }

        private void button6_Click(object sender, EventArgs e)
        {
            MonitorLogs = false;
            if (backgroundLogMonitor.IsBusy)
                this.backgroundLogMonitor.CancelAsync();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            if(backgroundWorker1.IsBusy)
                this.backgroundWorker1.CancelAsync();
        }
    }
}