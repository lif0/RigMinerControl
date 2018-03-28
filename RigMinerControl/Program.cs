using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenHardwareMonitor;
using OpenHardwareMonitor.Hardware;
using System.Threading;
using System.Net;
using SimpleJSON;
using System.Collections.Specialized;
using System.Net.Http;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace RigMinerControl
{
    static class GPUSensors
    {
        public static float? GetTemperature(this IHardware GPUAdapter)
        {
            if (GPUAdapter.HardwareType == HardwareType.GpuAti || GPUAdapter.HardwareType == HardwareType.GpuNvidia)
            {
                GPUAdapter.Update();
                return GPUAdapter.Sensors[0].Value;
            }

            return null;
        }

        public static float? GetFunSpeed(this IHardware GPUAdapter)
        {
            if (GPUAdapter.HardwareType == HardwareType.GpuAti || GPUAdapter.HardwareType == HardwareType.GpuNvidia)
            {
                GPUAdapter.Update();
                return GPUAdapter.Sensors[8].Value;
            }

            return null;
        }
    }
    class GPU
    {
        private string typeGPU;
        private string name;
        private int adapterIndex;
        private IHardware gpuAdapter;

        public string TypeGPU { get => typeGPU;}
        public string Name { get => name;}
        public int AdapterIndex { get => adapterIndex;}
        public IHardware GpuAdapter { get => gpuAdapter;}
        public GPU(ref IHardware GPUAdapter,int AdapterIndex)
        {
            gpuAdapter = GPUAdapter;
            typeGPU = GpuAdapter.HardwareType.ToString();
            name = GpuAdapter.Name;
            adapterIndex = AdapterIndex;
        }
    }
    class Telegram
    {
        private static string TOKEN = string.Empty;
        private static string chatID = string.Empty;
        private static string RigName = string.Empty;
        private int LastUpdateID = 0;
        //
        private int UpdateSpeedCommand = 0;
        private int UpdateFailWait = 0;
        private static int CountRigs = 0;
        private int thisRig = 0;
        private int WaitingAllRig = 0;

        //private static int 
        public Telegram()
        {
            RigName = Program.settingsINI.IniReadValue("RigMinerSetting", "RigName");
            LastUpdateID = int.Parse(Program.settingsINI.IniReadValue("Telegram", "LastUpdateID"));

            if(Program.settingsINI.IniReadValue("Telegram", "Token") == "")
                TOKEN = Properties.Resources.A1;
            else
                TOKEN = Program.settingsINI.IniReadValue("Telegram", "Token");
            //
            UpdateSpeedCommand = int.Parse(Program.settingsINI.IniReadValue("RigMinerSetting", "UpdateSpeedCommand"));
            UpdateFailWait = int.Parse(Program.settingsINI.IniReadValue("RigMinerSetting", "UpdateFailWait"));
            CountRigs = int.Parse(Program.settingsINI.IniReadValue("RigMinerSetting", "CountRig"));
            thisRig = int.Parse(Program.settingsINI.IniReadValue("RigMinerSetting", "thisRig"));
            WaitingAllRig = int.Parse(Program.settingsINI.IniReadValue("RigMinerSetting", "WaitingAllRig"));

            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    try
                    {
                        GetUpdates();
                        Thread.Sleep(UpdateSpeedCommand);
                    }
                    catch (Exception)
                    {
                        Thread.Sleep(UpdateFailWait);
                    }
                }
            });
        }
        void GetUpdates()
        {
            using (var webClient = new WebClient())
            {
                //https://api.telegram.org/bot407673499:AAGNMCa74b4y0aS8AO_88unQ0lnPdSlw9hc/getUpdates
                string response = webClient.DownloadString("https://api.telegram.org/bot" + TOKEN + "/getUpdates");
                if (response.Length > 23)
                {
                    JSONNode N = JSON.Parse(response);
                    int UnixTimeFirstMessage = N[1][0]["message"]["date"].AsInt;
                    foreach (JSONNode r in N["result"].AsArray)
                    {
                        if (r["message"]["text"].Value.Remove(0, 2).ToLower().Replace(" ", "") == "mytelegramid")
                        {
                            chatID = r["message"]["from"]["id"];
                            SendMessage(Program.LangINI.IniReadValue("Language", "YourID").Replace("%s", r["message"]["from"]["id"]));
                            LastUpdateID = r["update_id"].AsInt;
                            webClient.DownloadString("https://api.telegram.org/bot" + TOKEN + "/getUpdates" + "?offset=" + (LastUpdateID+1));
                            Program.settingsINI.IniWriteValue("Telegram", "LastUpdateID", LastUpdateID.ToString());
                        }
                        else if (Find(Program.ChatsID, r["message"]["from"]["id"]) && (r["update_id"].AsInt > LastUpdateID))
                        {
                            int differenceTime = r["message"]["date"].AsInt - UnixTimeFirstMessage;


                            if (r["update_id"].AsInt == (++LastUpdateID) || differenceTime > 300)//300 it 5 minure on UnixTime format
                            {
                                if (thisRig < CountRigs)
                                    Thread.Sleep(WaitingAllRig);
                                response = webClient.DownloadString("https://api.telegram.org/bot" + TOKEN + "/getUpdates" + "?offset=" + (LastUpdateID + 1));
                            }

                            chatID = r["message"]["from"]["id"];
                            LastUpdateID = r["update_id"].AsInt;
                            //UnixTimeFirstMessage = N["message"]["date"].AsInt;
                            Program.settingsINI.IniWriteValue("Telegram", "LastUpdateID", LastUpdateID.ToString());
                            PerfCommand.Run(r["message"]["text"]);
                        }
                        else
                        {
                        }
                        //else
                            //UnixTimeFirstMessage = N["message"]["date"].AsInt;
                    }
                }
            }
        }
        bool Find(List<string> _list, string value)
        {
            for (int i = 0; i < _list.Count; i++)
                if (_list[i] == value)
                    return true;

            return false;
        }

        #region Method Telegram
        public static void SendMessage(string message)
        {
            SendChatAction(ChatAction.typing);
            using (var webClient = new WebClient())
            {
                if (message != "")
                {
                    NameValueCollection pars = new NameValueCollection();
                    if(int.Parse(Program.settingsINI.IniReadValue("RigMinerSetting", "CountRig")) > 1)
                    {
                        string str = Program.LangINI.IniReadValue("Language", "RigName").Replace("%s", RigName);
                        pars.Add("text", String.Format("__{0}__\r\n{1}", str,message));
                    }
                    else
                        pars.Add("text", message);
                    pars.Add("chat_id", chatID);

                    webClient.UploadValues("https://api.telegram.org/bot" + TOKEN + "/sendMessage", pars);
                }
            }
        }
        public static void SendMessage(string message,string ChatID)
        {
            Telegram.SendChatAction(Telegram.ChatAction.typing);
            using (var webClient = new WebClient())
            {
                if (message != "")
                {
                    var pars = new NameValueCollection();
                    pars.Add("text", message);
                    pars.Add("chat_id", ChatID);
                    webClient.UploadValues("https://api.telegram.org/bot" + TOKEN + "/sendMessage", pars);
                }
            }
        }
        public async static Task SendLocalDocument(string filePath)
        {
            Telegram.SendChatAction(Telegram.ChatAction.upload_photo);
            string fileName = filePath.Split('\\').Last();

            using (var form = new MultipartFormDataContent())
            {
                string caption = string.Empty;
                if (CountRigs > 1)
                    caption = Program.LangINI.IniReadValue("Language", "RigName").Replace("%s", RigName);
                form.Add(new StringContent(chatID, Encoding.UTF8), "chat_id");
                form.Add(new StringContent(caption, Encoding.UTF8), "caption");

                using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    form.Add(new StreamContent(fileStream), "document", fileName);

                    using (var client = new HttpClient())
                    {
                        await client.PostAsync(string.Format("https://api.telegram.org/bot{0}/sendDocument", TOKEN), form);
                    }
                }
            }
        }
        //private static void SendPhoto(string caption, string file_ID, int chatID,string Token)
        //{
        //    Telegram.SendChatAction(Telegram.ChatAction.upload_photo);
        //    using (var webClient = new WebClient())
        //    {
        //        var pars = new NameValueCollection();
        //        pars.Add("photo", file_ID);
        //        pars.Add("chat_id", chatID.ToString());
        //        pars.Add("caption", caption);
        //        webClient.UploadValues("https://api.telegram.org/bot" + Token + "/sendPhoto", pars);
        //    }
        //}
        private static void SendChatAction(ChatAction action)
        {
            using (WebClient webClient = new WebClient())
            {
                NameValueCollection pars = new NameValueCollection();
                pars.Add("chat_id", chatID.ToString());
                pars.Add("action", action.ToString());
                webClient.UploadValues("https://api.telegram.org/bot" + TOKEN + "/sendChatAction", pars);
            }
        }
        private enum ChatAction
        {
            typing,
            upload_photo,
            record_video,
            upload_video,
            record_audio,
            upload_audio,
            upload_document,
            find_location
        }
        #endregion

        #region keyboard
        public static void SendKeyboard()
        {
            using (var webClient = new WebClient())
            {
                for (int i = 0; i < Program.ChatsID.Count; i++)
                {
                    NameValueCollection pars = new NameValueCollection();
                    pars.Add("chat_id", Program.ChatsID[i]);
                    pars.Add("text", Program.LangINI.IniReadValue("Language", "IamStarted"));

                    string JSON = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "keyboard.telegram");
                    //pars.Add("reply_markup", "{\"remove_keyboard\":true}");
                    pars.Add("reply_markup", JSON);

                    webClient.UploadValues("https://api.telegram.org/bot" + TOKEN + "/sendMessage", pars);
                }
            }
        }
        #endregion
    }
    static class PerfCommand
    {
        public static void Run(string Command)
        {
            switch (Command.Remove(0, 2).ToLower().Replace(" rig","").Replace(" ",""))
            {
                case "balance":
                    GetBalanceMiner();
                    break;

                case "screenshot":
                    GetScreenshot();
                    break;

                case "temperature":
                     GetTemperature();
                    break;

                case "restart":
                    ShellConsoleCommand("shutdown -r");
                    break;

                case "shutdown":
                    ShellConsoleCommand("shutdown -s");
                    break;
                case "/start":
                    Telegram.SendKeyboard();
                    break;
                case "/setPosition":

                    break;

                default:
                    Telegram.SendMessage("*" + Program.LangINI.IniReadValue("Language", "CommandNotFound") + "*");
                    break;
            }
        }
        #region GetScreen
        private static void GetScreenshot()
        {
            System.Drawing.Bitmap BM = new System.Drawing.Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            System.Drawing.Graphics GH = System.Drawing.Graphics.FromImage(BM as System.Drawing.Image);
            GH.CopyFromScreen(0, 0, 0, 0, BM.Size);
            GH.DrawString("*", new System.Drawing.Font("Arial", 30), System.Drawing.Brushes.Red, Cursor.Position.X, Cursor.Position.Y);
            using (SaveFileDialog SFD = new SaveFileDialog())
            {
                SFD.FileName = Path.GetTempPath() + DateTime.Now.Day + DateTime.Now.Month + DateTime.Now.Year + ".png";
                BM.Save(SFD.FileName);
                Telegram.SendLocalDocument(SFD.FileName).Wait();
            }
        }
        #endregion
        #region GetTemperature
        private static void GetTemperature()
        {
            string _temp = String.Empty;
            for (int i = 0; i < Program.GPUAdapters.Count; i++)
            {
                float? temperature = Program.GPUAdapters[i].GpuAdapter.GetTemperature();

                if (temperature != null)
                    _temp += String.Format("[{0}]| {1}: {2}\r\n", i, Program.GPUAdapters[i].Name, temperature + "°C");
            }
            Telegram.SendMessage(_temp);
        }
        #endregion
        #region GetBalance
        public static void GetBalanceMiner()
        {
            using (WebClient web = new WebClient())
            {
                string miner = Program.settingsINI.IniReadValue("RigMinerSetting", "Miner");
                string pool = Program.settingsINI.IniReadValue("RigMinerSetting", "EtherPool") + "miners/";
                if (miner == "")
                {
                    Telegram.SendMessage(Program.LangINI.IniReadValue("Language", "SetMiner"));
                    return;
                }

                string html = web.DownloadString(pool + miner);
                if (html.IndexOf("Error: Invalid search string provided!", 0) >= 0)
                    Telegram.SendMessage(Program.LangINI.IniReadValue("Language", "MinerNotFound").Replace("%s", miner));
                html = html.Remove(0, html.IndexOf("Hashrates"));
                html = html.Remove(html.IndexOf("Hashrate, Shares & Workers"));
                //html = html.Remove(0, html.IndexOf("<hr>", 0));
                //html = html.Remove(html.IndexOf("row hidden-md hidden-lg", 0));

                string hashrates = string.Empty;
                getHashrates(ref html, ref hashrates);
                if(hashrates == "")
                {
                    Telegram.SendMessage(Program.LangINI.IniReadValue("Language", "MinerIsNotActive"));
                    return;
                }
                string unpaidBalance = string.Empty;
                getUnpaidBalance(ref html, ref unpaidBalance);
                string activeWorkers = string.Empty;
                getActiveWorkers(ref html, ref activeWorkers);
                string shares = string.Empty;
                getShares(ref html, ref shares);
                //Telegram.SendMessage(String.Format("Hashrates:\r\n{0}\r\nUnpaid Balance:\r\n{1}\r\nActive Workers:\r\n{2}\r\nShares (Last 1h):\r\n{3}", Hashrates, UnpaidBalanc, ActiveWorkers, Shares));
                Telegram.SendMessage(String.Format("Hashrates:\r\n{0}\r\n", hashrates));
                Telegram.SendMessage(String.Format("Unpaid Balance:\r\n{0}\r\n", unpaidBalance));
                Telegram.SendMessage(String.Format("Active Workers:\r\n{0}\r\n", activeWorkers));
                Telegram.SendMessage(String.Format("Shares (Last 1h):\r\n{0}\r\n", shares));


            }
        }

        static void getHashrates(ref string html, ref string Hashrates)
        {
            string _temp = html.Remove(html.IndexOf("Unpaid Balanc"));
            Regex r = new Regex(@"\d+\.\d+.\w+.\w+", RegexOptions.Multiline);
            MatchCollection matches = r.Matches(_temp);
            for (int i = 0; i < matches.Count; i++)
                Hashrates += matches[i] + " ";
        }
        static void getUnpaidBalance(ref string html, ref string UnpaidBalanc)
        {
            string _temp = html.Remove(0,html.IndexOf("Unpaid Balanc"));
            _temp = _temp.Remove(_temp.IndexOf("Active Workers"));
            Regex r = new Regex(@"\d+\.\d+.\w+", RegexOptions.Multiline);
            MatchCollection matches = r.Matches(_temp);
            if (matches.Count == 0)
            {
                UnpaidBalanc = "0";
                return;
            }
            UnpaidBalanc = matches[0].ToString();    
        }
        static void getActiveWorkers(ref string html, ref string ActiveWorkers)
        {
            string _temp = html.Remove(0, html.IndexOf("Active Workers"));
            _temp = _temp.Remove(_temp.IndexOf("Shares"));
            Regex r = new Regex(@">\d+<", RegexOptions.Multiline);
            MatchCollection matches = r.Matches(_temp);
            if (matches.Count == 0)
            {
                ActiveWorkers = "0";
                return;
            }
            ActiveWorkers = matches[0].ToString();

        }
        static void getShares(ref string html, ref string Shares)
        {
            string _temp = html.Remove(0, html.IndexOf("Shares (Last 1h)"));
            _temp = _temp.Remove(_temp.IndexOf("Hashrates"));
            Regex r = new Regex(@"\d+ \(\d+\%\)", RegexOptions.Multiline);
            MatchCollection matches = r.Matches(_temp);
            for (int i = 0; i < matches.Count; i++)
                Shares += matches[i] + " / ";

        }
        #endregion
        #region CMD
        public static void ShellConsoleCommand(string command)
        {
            Process MyProcess = new Process();
            ProcessStartInfo MyStIn = new ProcessStartInfo();
            MyStIn.FileName = "cmd.exe";
            MyStIn.RedirectStandardError = false;
            MyStIn.RedirectStandardOutput = false;
            MyStIn.RedirectStandardInput = false;
            MyStIn.UseShellExecute = false;
            MyStIn.CreateNoWindow = true;
            MyStIn.Arguments = "/D /c" + command;
            MyProcess.StartInfo = MyStIn;
            MyProcess.Start();
            MyProcess.Dispose();
        }
        #endregion
    }
    class Program
    {
        public static INI settingsINI = new INI(AppDomain.CurrentDomain.BaseDirectory + "settings.ini");
        public static INI LangINI = new INI(AppDomain.CurrentDomain.BaseDirectory + "lang\\"+settingsINI.IniReadValue("RigMinerSetting", "language"));
        public static List<GPU> GPUAdapters = new List<GPU>();
        public static List<string> ChatsID = new List<string>();
        static Computer thisComputer;
        static bool Notif = false;
        static int offset = 0;
        static void Main(string[] args)
        {
            //PerfCommand.GetBalanceMiner();
            //Console.ReadKey();
            //
            Telegram t = new Telegram();
            getChatsID();

            Telegram.SendKeyboard();

            Console.Write("Load.........................");
            thisComputer = new Computer() {
                GPUEnabled = true,
                FanControllerEnabled = false,
                CPUEnabled = false,
                HDDEnabled=false,
                MainboardEnabled=false,
                RAMEnabled=false};

            thisComputer.Open();
            Console.Clear();

            for (int i = 0; i < thisComputer.Hardware.Length; i++)
                GPUAdapters.Add(new GPU(ref thisComputer.Hardware[i], i));

            new Thread(new ThreadStart(ConsoleUI)).Start();
            Console.ReadLine();
        }

        static int SpeedUpdateSensonrs;
        static void ConsoleUI()
        {
            SpeedUpdateSensonrs = int.Parse(settingsINI.IniReadValue("RigMinerSetting", "SpeedUpdateTemperature"));
            float? temperature;
            //float? funSpeed;
            for (;;)
            {
                for (int i = 0; i < thisComputer.Hardware.Length; i++)
                {
                    Console.SetCursorPosition(0, i+ offset);
                    Console.Write(GPUAdapters[i].Name + ": ");

                    temperature = GPUAdapters[i].GpuAdapter.GetTemperature();
                    //funSpeed = GPUAdapters[i].GpuAdapter.GetFunSpeed();


                    if (temperature != null)
                    {
                        if (temperature >= float.Parse(settingsINI.IniReadValue("GPUAdapter." + GPUAdapters[i].AdapterIndex, "TemperatureLimit_Shutdown")))
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            foreach (String chatID in ChatsID)
                            {
                                Telegram.SendMessage(LangINI.IniReadValue("Language", "Temperaturelimitexceeded") + "\r\n" +
                                    LangINI.IniReadValue("Language", "Turnoff"), chatID);
                            }
                            PerfCommand.ShellConsoleCommand("shutdown -f -s -t ");
                        }
                        else if (temperature >= float.Parse(settingsINI.IniReadValue("GPUAdapter." + GPUAdapters[i].AdapterIndex, "TemperatureLimit_NotifyMe")))
                        {
                            if (!Notif)
                            {
                                Notif = !Notif;
                                foreach (String chatID in ChatsID)
                                    Telegram.SendMessage(LangINI.IniReadValue("Language", "Temperaturelimitexceeded"),chatID);
                            }
                            Console.ForegroundColor = ConsoleColor.Red;
                        }
                        else { 
                            Console.ForegroundColor = ConsoleColor.DarkGreen;
                            Notif = !Notif;
                        }
                    }
                    Console.Write(temperature+ "°C");

                    //Console.ResetColor();
                    //Console.WriteLine(" | Fun:"+funSpeed+"%");
                }
                outputToTheConsole("\r\nBTC: 1E7zHkPucMER2WxWmQouY2rjUdQqGhDxak", ConsoleColor.DarkYellow);
                outputToTheConsole("ETH: 0x431d452bbeC3ee01825396BC242f716B19BBcdE4", ConsoleColor.DarkYellow);
                outputToTheConsole("DCR: Dsiofn49UKp4PNXLKyzifPJVCkGBkKuNkVf\r\n", ConsoleColor.DarkYellow);
                Thread.Sleep(SpeedUpdateSensonrs);
                
            }
        }
        static void outputToTheConsole(string txt, ConsoleColor cl)
        {
            Console.ForegroundColor = cl;
            Console.WriteLine(txt);
            Console.ResetColor();
        }
        static void getChatsID()
        {
            ChatsID.AddRange(settingsINI.IniReadValue("Telegram", "ChatsID").Split(','));
        }

    }
}
