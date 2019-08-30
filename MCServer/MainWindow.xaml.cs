using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
using Xceed.Wpf.Toolkit;
using System.Collections.ObjectModel;
using System.Collections;

namespace MCServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
        public Process process;
        private delegate void UpdateTextCallback(string text);

        DateTime refreshCountdown;
        int motdIndex = 0;

        //bool restartsEnabled;
        int pmAddition = 0;
        DateTime upTimeStart;

        bool restartTimeofDay;
        bool restartWarnings;

        bool warn60;
        bool warn30;
        bool warn15;
        bool warn10;
        bool warn5;
        bool warn2;
        bool warn1;

        bool restarting = false;
        bool uptimeWarning = false;

        bool serverRunning = false;
        string jarLocation = Properties.Settings.Default.jarLocation;
        string jarDirectory = Properties.Settings.Default.jarDirectory;

        string workingDir;

        string nogui;


        SolidColorBrush statusOff = new SolidColorBrush(Colors.Red);
        SolidColorBrush statusOn = new SolidColorBrush(Colors.Green);


        ObservableCollection<Xceed.Wpf.Toolkit.ColorItem> ColorList = new ObservableCollection<ColorItem>();
       // ColorList = new ObservableCollection<Xceed.Wpf.Toolkit.ColorItem>();

        // Dictionary<Color, string> colorDict = new Dictionary<Color, string>()
        


        public MainWindow()
        {
          //   Properties.Settings.Default.Reset();

            

            InitializeComponent();

            PopulateColorList();

            //Console.WriteLine("YEET");
            #region Saved Settings
            chkautoStart.IsChecked = Properties.Settings.Default.autoStart;
            ramMin.Text = Properties.Settings.Default.ramMin;
            ramMax.Text = Properties.Settings.Default.ramMax;
            chkNoGui.IsChecked = Properties.Settings.Default.nogui;
            comboMOTD.Text = Properties.Settings.Default.motdFreq;
            txtMotd.Text = Properties.Settings.Default.txtMotd;
            txtHour.Text = Properties.Settings.Default.restartHr;
            txtMinute.Text = Properties.Settings.Default.restartMin;
            //ComboBoxItem ampmCBI = new ComboBoxItem();// = Properties.Settings.Default.restartAMPM
            //ampmCBI.Content = Properties.Settings.Default.restartAMPM;
            //comboAMPM.SelectedItem = ampmCBI;
            comboAMPM.Text = Properties.Settings.Default.restartAMPM;
            txtHoursRestart.Text =Properties.Settings.Default.restartHours;
            txtHour.Text = Properties.Settings.Default.restartHr;
            txtMinute.Text = Properties.Settings.Default.restartMin;
            txtCommandBox.Text = Properties.Settings.Default.command;
            chkWarnings.IsChecked = Properties.Settings.Default.restartWarnings;
            chkRestart.IsChecked = Properties.Settings.Default.enableRestarts;
            chkRestartPC.IsChecked = Properties.Settings.Default.restartPC;
            if (Properties.Settings.Default.restartTimeOfDay) // Radios
            {
                restartTimeofDay = true;
                radTimeOfDay.IsChecked = true;
            }
            else
            {
                restartTimeofDay = false;
                radSpecifiedTime.IsChecked = true;
            }
            //StringCollection motdItems = Properties.Settings.Default.motdItems;
            //if (motdItems !=null)
            //    foreach(var item in motdItems.Cast<string>().ToList())
            //        listMotd.Items.Add(item);

            if (Properties.Settings.Default.motd2 !=null)
            {
                Console.WriteLine(Properties.Settings.Default.motd2.Count);
                foreach (var item in Properties.Settings.Default.motd2)
                {
                    ListBoxItem lbi = new ListBoxItem();
                    string[] split = item.ToString().Split('^');

                    lbi.Content = split[0];
                    if (split.Length > 1)
                        lbi.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(split[1]));
                    listMotd.Items.Add(lbi);
                }
            }
            #endregion

            process = new Process();
            process.StartInfo.FileName = "cmd.exe";

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.WorkingDirectory = $@"{Properties.Settings.Default.workingDrive}:\";
            process.StartInfo.LoadUserProfile = true;

            process.StartInfo.CreateNoWindow = true;

            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;

            process.OutputDataReceived += ProcessOutputDataHandler;
            process.ErrorDataReceived += ProcessErrorDataHandler;

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            stopServer.IsEnabled = false;

            if (string.IsNullOrEmpty(jarLocation))
                startServer.IsEnabled = false;
            else
            {
                txtServerJar.Text = System.IO.Path.Combine(jarDirectory,jarLocation);
                SendCommand("cd " + jarDirectory);
                if(chkautoStart.IsChecked==true)
                {
                    StartServer();
                }
            }
            
            Log(jarLocation);

            var dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
            refreshCountdown = DateTime.Now.AddSeconds(60*int.Parse(comboMOTD.Text));
            //process.StandardInput.WriteLine("dir");

        }


        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            bool notRestarting = true;
            DateTime restartTime;
            if (!serverRunning) return;
            if (Properties.Settings.Default.enableRestarts && !restarting)
            {

                if (restartTimeofDay)
                {

                    restartTime = DateTime.Today;
                    int intHourFromTxt;
                    intHourFromTxt = int.Parse(txtHour.Text);
                    if (intHourFromTxt == 12)
                        intHourFromTxt = 0;
                        
                    int hr = (pmAddition + intHourFromTxt);
                    restartTime = restartTime.AddHours(hr);
                    int min = int.Parse(txtMinute.Text);
                    restartTime = restartTime.AddMinutes(min);

                  //  Log("" + hr+":"+min);
                    // if (DateTime.Now.Hour == (pmAddition + int.Parse(txtHour.Text)) && DateTime.Now.Minute == int.Parse(txtMinute.Text))
                    if (DateTime.Now.Hour == restartTime.Hour && DateTime.Now.Minute == restartTime.Minute)
                    {
                        if (DateTime.Now > upTimeStart.AddSeconds(60))// Safeguarding against quick boots and Auto restart.
                        {
                            Restart();
                            notRestarting = false;
                        }
                        else
                        {
                            if (!uptimeWarning)
                            {
                                Log("Server will not reboot, uptime too low");
                                uptimeWarning = true;
                            }

                        }
                    }
                }
                else
                {
                    //int hrs = int.Parse(txtHoursRestart.Text);
                    // restartTime = upTimeStart.AddDays(Math.Abs(hrs/24));
                    // restartTime = upTimeStart.AddHours(hrs%24);
                    restartTime = upTimeStart.AddHours(double.Parse(txtHoursRestart.Text));
                    // if (DateTime.Now > upTimeStart.AddHours(double.Parse(txtHoursRestart.Text)))
                    if (DateTime.Now > restartTime)
                        {
                        Restart();
                        notRestarting = false;
                    }
                }

                if (notRestarting) { TimeTillRestart(restartTime); }
            }


            if (listMotd.Items.Count == 0) return;

            TimeSpan timeTillRefresh = refreshCountdown - DateTime.Now;

            if (timeTillRefresh.TotalSeconds <= 0 && serverRunning)
            {
                if (motdIndex >= listMotd.Items.Count)
                    motdIndex = 0;
                var lbi = listMotd.Items[motdIndex] as ListBoxItem;

                //SendCommand("say "+lbi.Content.ToString() + ColorList.FirstOrDefault(x => x.Color ==(lbi.Foreground as SolidColorBrush).Color).Name);
                string col = ColorList.FirstOrDefault(x => x.Color == (lbi.Foreground as SolidColorBrush).Color).Name;
                SendCommand("/tellraw @a [\"\",{\"text\":\"" + lbi.Content.ToString()+"\", \"color\":\""+ col+"\"}]");
                Log(col + "--" + lbi.Content.ToString());
                //  ["",{"text":"Welco"},{"text":"me to Minecraft Tools","color":"dark_aqua"}]
                

                motdIndex++;
                refreshCountdown = DateTime.Now.AddSeconds(60 * int.Parse(comboMOTD.Text));
            }

           
        }

            public void ProcessOutputDataHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            //Console.WriteLine(outLine.Data);
            WriteTextSafe(outLine.Data);
        }

        public void ProcessErrorDataHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            WriteTextSafe(outLine.Data);
        }

        private void StartServer_Click(object sender, RoutedEventArgs e)
        {
           StartServer();
        }

        private void StartServer()
        {
            SendCommand("java -Xms" + ramMin.Text + " -Xmx" + ramMax.Text + " -jar " + jarLocation + nogui);
            upTimeStart = DateTime.Now;
            startServer.IsEnabled = false;
            stopServer.IsEnabled = true;
            serverRunning = true;
            uptimeWarning = false;
            labStatus.Background = statusOn;
            labStatus.Content = "ON";

            warn60 = false;
            warn30 = false; 
            warn15 = false;
            warn10 = false;
            warn5 = false;
            warn2 = false;
            warn1 = false;
        }

        private void SendCommand(string command)
        {
            if (command == "stop")
            {
                StopServer();
                return;
            }
            Console.WriteLine("Sending Command:" + command);
            process.StandardInput.WriteLine(command);
            
        }

        private async void Restart()
        {

            restarting = true;

            Log("Stopping Server..");
            StopServer();

            SaveSettings();

            Log("Waiting 10 Seconds..");
            await Task.Delay(10000); // wait 10 Seconds
            if (!Properties.Settings.Default.restartPC)
            {
                Log("Starting Server...");
                StartServer();
            }
            else
            {
                System.Diagnostics.Process.Start("shutdown.exe", "-f -r -t 0");
            }
            restarting = false;
        }

        private void SaveSettings()
        {
            Properties.Settings.Default.txtMotd = txtMotd.Text;
            Properties.Settings.Default.restartWarnings = restartWarnings;
            Properties.Settings.Default.restartTimeOfDay = restartTimeofDay;

            Properties.Settings.Default.ramMax = ramMax.Text;
            Properties.Settings.Default.ramMin = ramMin.Text;


            Properties.Settings.Default.restartHr = txtHour.Text;
            Properties.Settings.Default.restartMin = txtMinute.Text;
            Properties.Settings.Default.restartAMPM = comboAMPM.Text;
            Properties.Settings.Default.restartHours = txtHoursRestart.Text;
            Properties.Settings.Default.command = txtCommandBox.Text;

            //StringCollection collection = new StringCollection();
            //foreach (ListBoxItem item in listMotd.Items)
            //    collection.Add(item.Content.ToString());
            //Properties.Settings.Default.motdItems = collection;


            //if (listMotd.Items.Count > 0)
            //{
            //    var newList = new ArrayList();
            //    foreach (object item in listMotd.Items)
            //    {
            //        newList.Add(item.ToString() ;
            //    }

            //    Properties.Settings.Default.motd2 = newList;
            //    Console.WriteLine(Properties.Settings.Default.motd2.Count);
            //}

            if (listMotd.Items.Count > 0)
            {
                var newList = new ArrayList();
                foreach (ListBoxItem item in listMotd.Items)
                {
                    string color = item.Foreground.ToString();

                    newList.Add(item.Content.ToString()+"^"+color);
                }

                Properties.Settings.Default.motd2 = newList;
               // Console.WriteLine(Properties.Settings.Default.motd2.Count);
            }

            Properties.Settings.Default.motdFreq = comboMOTD.Text;
            Properties.Settings.Default.Save();
        }

        private void TimeTillRestart(DateTime scheduleTime, bool debug = false)
        {
            TimeSpan timeleft;
            if (restartTimeofDay)
            {
                if (scheduleTime.Hour < DateTime.Now.Hour || (scheduleTime.Hour == DateTime.Now.Hour && scheduleTime.Minute < DateTime.Now.Minute))
                {
                   scheduleTime = scheduleTime.AddDays(1);
                }
                timeleft = (DateTime.Now - scheduleTime);
               if (debug) Log(timeleft.Hours + ":" + timeleft.Minutes+":"+timeleft.Seconds);
            }
            else
            {
                timeleft = (DateTime.Now - scheduleTime);
              if (debug)  Log((timeleft.Days*24+ timeleft.Hours) + ":" + timeleft.Minutes);
            }

            if (restartWarnings && Properties.Settings.Default.enableRestarts && !restarting)
            {
                if (Math.Abs(timeleft.Hours) == 0)
                {
                    switch (Math.Abs(timeleft.Minutes)){
                        case 59:
                            if (!warn60)
                            {
                                warn60 = true; // Set so it doesnt repeat in the same minute -- diff seconds.
                                SendCommand("say 1 Hour till server restart");
                            }
                            break;
                        case 29:
                            if (!warn30)
                            {
                                warn30 = true; // Set so it doesnt repeat in the same minute -- diff seconds.
                                SendCommand("say 30 minutes till server restart");
                            }
                            break;
                        case 14:
                            if (!warn15)
                            {
                                warn15 = true; // Set so it doesnt repeat in the same minute -- diff seconds.
                                SendCommand("say 15 minutes till server restart");
                            }
                            break;
                        case 9:
                            if (!warn10)
                            {
                                warn10 = true; // Set so it doesnt repeat in the same minute -- diff seconds.
                                SendCommand("say 10 minutes till server restart");
                            }
                            break;
                        case 4:
                            if (!warn5)
                            {
                                warn5 = true; // Set so it doesnt repeat in the same minute -- diff seconds.
                                SendCommand("say 5 minutes till server restart");
                            }
                            break;
                        case 1:
                            if (!warn2)
                            {
                                warn2 = true; // Set so it doesnt repeat in the same minute -- diff seconds.
                                SendCommand("say 2 minutes till server restart");
                            }
                            break;
                        case 0:
                            if (!warn1)
                            {
                                warn1 = true; // Set so it doesnt repeat in the same minute -- diff seconds.
                                SendCommand("say 1 minutes till server restart");
                                SendCommand("save-all");
                            }

                            switch (Math.Abs(timeleft.Seconds))
                            {
                                case 30:

                                    SendCommand("say 30 seconds till server restart");
                                    break;
                                case 10:

                                    SendCommand("say 10 seconds till server restart");
                                    break;
                                case 5:

                                    SendCommand("say 5 seconds till server restart");
                                    break;
                                case 4:

                                    SendCommand("say 4 seconds till server restart");
                                    break;
                                case 3:

                                    SendCommand("say 3 seconds till server restart");
                                    break;
                                case 2:

                                    SendCommand("say 2 seconds till server restart");
                                    break;
                                case 1:

                                    SendCommand("say 1 seconds till server restart");
                                    break;
                            }
                            break;
                    }
                }
            }
        }


        public void Log(string message)
        {
            //LogBox.AppendText("\n[" + DateTime.Now.ToString("HH:mm") + "]" + message);
            LogBox.AppendText("\n"+ message);
            LogBox.ScrollToEnd();

            //WriteTextSafe(message);
        }

        private void WriteTextSafe(string text)
        {
            
                var d = new UpdateTextCallback(Log);
                LogBox.Dispatcher.Invoke(d, new object[] { text });
           
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            OpenFileDialog openFileDialog = new OpenFileDialog();
         //   openFileDialog.Multiselect = true;
            openFileDialog.Filter = "Server Jar (*.jar)|*.jar";
            if (string.IsNullOrEmpty(Properties.Settings.Default.jarDirectory))
                openFileDialog.InitialDirectory = Environment.CurrentDirectory;
            else
                openFileDialog.InitialDirectory = Properties.Settings.Default.jarDirectory;
            if (openFileDialog.ShowDialog() == true)
            {
                jarLocation = openFileDialog.SafeFileName;
                Properties.Settings.Default.jarLocation = jarLocation;
              //  jarLocation = openFileDialog.FileName;
                txtServerJar.Text = openFileDialog.FileName;
                workingDir = System.IO.Path.GetDirectoryName(openFileDialog.FileName);
                if (!workingDir.StartsWith(Properties.Settings.Default.workingDrive))
                    {

                    Properties.Settings.Default.workingDrive = workingDir.Substring(0, 1);
                    SendCommand(Properties.Settings.Default.workingDrive+":");
                }

                Properties.Settings.Default.jarDirectory = workingDir;
                SendCommand("cd " + workingDir);
                if (!startServer.IsEnabled)
                    startServer.IsEnabled =true;
                serverRunning = true;
            }


        }

        private void SendCommand_Click(object sender, RoutedEventArgs e)
        {
            SendCommand(txtCommandBox.Text);
        }

        private void ChkNoGui_Checked(object sender, RoutedEventArgs e)
        {
            nogui = " nogui";
            Properties.Settings.Default.nogui = true;
        }

        private void ChkNoGui_Unchecked(object sender, RoutedEventArgs e)
        {
            nogui = "";
            Properties.Settings.Default.nogui = false;
        }

        private void StopServer_Click(object sender, RoutedEventArgs e)
        {
            StopServer();
        }

        private void StopServer()
        {
            process.StandardInput.WriteLine("stop");
            startServer.IsEnabled = true;
            stopServer.IsEnabled = false;
            serverRunning = false;
            labStatus.Background = statusOff;

            labStatus.Content = "OFF";
        }

        private void BtnAddMotd_Click(object sender, RoutedEventArgs e)
        {
            ListBoxItem lbi = new ListBoxItem();
            lbi.Content = txtMotd.Text.Replace("^",string.Empty);
            lbi.Foreground = new SolidColorBrush((Color)clrPicker.SelectedColor);
            //listMotd.Items.Add(txtMotd.Text);
            listMotd.Items.Add(lbi);
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {

               // var textBox = sender as TextBox;
                e.Handled = Regex.IsMatch(e.Text, "[^0-9]+");
            
        }


        private void ChkRestart_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.enableRestarts = true;
        }

        private void ChkRestart_Unchecked(object sender, RoutedEventArgs e)
        {
           Properties.Settings.Default.enableRestarts = false;
        }

        private void ComboAMPM_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string text = (comboAMPM.SelectedItem as ComboBoxItem).Content as string;
            Log(text);
            if (text == "AM")
                pmAddition = 0;
            else
                pmAddition = 12;
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            restartTimeofDay = true;
            //restart at X time of day
        }

        private void RadioButton_Checked_1(object sender, RoutedEventArgs e)
        {
            restartTimeofDay = false;
            //restart after X time instead.
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (serverRunning)
            {
                StopServer();

              //  await Task.Delay(4000); // Dont need delay - as its a separate window, it does close safely once it receives stop input.
            }
            SaveSettings();
           
            // await Task.Delay(2000);
           // process.Close();
            
        }

        private void ChkautoStart_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.autoStart = true;
        }

        private void ChkautoStart_Unchecked(object sender, RoutedEventArgs e)
        {
            
            Properties.Settings.Default.autoStart = false;
        }

        private void BtnRemoveMOTD_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in listMotd.SelectedItems.Cast<object>().ToArray())
            {
               listMotd.Items.Remove(item);
            }
        }

        private void ChkWarnings_Unchecked(object sender, RoutedEventArgs e)
        {
            restartWarnings = false;
        }

        private void ChkWarnings_Checked(object sender, RoutedEventArgs e)
        {
            restartWarnings = true;
        }

        private void TxtHoursRestart_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtHoursRestart.Text) || int.Parse(txtHoursRestart.Text) == 0)
            {
                txtHoursRestart.Text = "1";
            }
            else
            {
                if (int.Parse(txtHoursRestart.Text) > 999)
                    txtHoursRestart.Text = "24";
            }
        }

        private void TxtHour_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtHour.Text) || int.Parse(txtHour.Text) == 0)
            {
                txtHour.Text = "1";
            }
            else
            {
                if (int.Parse(txtHour.Text) > 12)
                    txtHour.Text = "12";

            }

            
        }

        private void TxtMinute_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtMinute.Text))
            {
                txtMinute.Text = "00";
            }
            else
            {
                if (int.Parse(txtMinute.Text) > 59)
                    txtMinute.Text = "59";
            }
        }

        private void ChkRestartPC_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.restartPC = false;
        }

        private void ChkRestartPC_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.restartPC = true;
        }
        private void PopulateColorList()
        {
            //ColorList = new ObservableCollection<Xceed.Wpf.Toolkit.ColorItem>();
            //ColorList.Add(new ColorItem(Colors.Beige, "Beige"));
            //ColorList.Add(new ColorItem(Colors.Black, "Black"));
            //ColorList.Add(new ColorItem(Colors.Blue, "Blue"));
            //ColorList.Add(new ColorItem(Colors.Pink, "Pink"));
            //ColorList.Add(new ColorItem(Colors.Red, "Red"));
            //ColorList.Add(new ColorItem(Colors.White, "White"));
            //ColorList.Add(new ColorItem(Colors.Yellow, "Yellow"));

            ColorList = new ObservableCollection<Xceed.Wpf.Toolkit.ColorItem>()
        {
            {new ColorItem( Colors.DarkRed, "dark_red") },
            {new ColorItem( Colors.Red, "red" )},
            {new ColorItem(Colors.Gold, "gold") } ,
            {new ColorItem(Colors.Yellow, "yellow" )},
            {new ColorItem(Colors.DarkGreen, "dark_green") },
            {new ColorItem(Colors.Green, "green") },
            {new ColorItem(Colors.Aqua, "aqua" )},
            {new ColorItem(Colors.DarkBlue,"dark_blue") },
            {new ColorItem(Colors.Blue, "blue") },
            {new ColorItem(Colors.LightCyan, "light_purple") },
            {new ColorItem(Colors.DarkCyan, "dark_purple" )},
            {new ColorItem(Colors.White, "white" )},
            {new ColorItem(Colors.Gray, "gray") },
            {new ColorItem(Colors.DarkGray, "dark_gray" )},
            {new ColorItem(Colors.Black, "black" )},
        };
            clrPicker.StandardColors = ColorList;
        }

        private void TxtMotd_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (e.Text == '^'.ToString())
                e.Handled = true;
        }
    }

    

}


