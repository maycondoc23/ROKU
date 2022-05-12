using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using WebServerSFC.Classes;

namespace WebServerSFC
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static string HostName = "";

        public static string OperatorId = "";

        public static string OperatorPassWord = "";
        public string StationGroup { get; set; }

        public static bool TestModeControl = true;

        public static List<string> LastSerial = new List<string>();


        /*************************************************************************************************************************/
        /*--- Inicialização ---*/
        public MainWindow()
        {
            Process aProcess = Process.GetCurrentProcess();
            string aProcName = aProcess.ProcessName;

            if (Process.GetProcessesByName(aProcName).Length > 1)
            {
                aProcess.Kill();
            }

            using (var writeLog = new WriteLog())
            {
                writeLog.WriteLogFile("WebServerSFC Starting");
            }

            InitializeComponent();

            try
            {
                KillProcess();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Alert", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            DispatcherTimer timer = new DispatcherTimer();
            timer.Tick += TimerTick;
            timer.Interval = TimeSpan.FromSeconds(5);
            timer.Start();

            this.Title = $"SENTINELA ROKU 2.0.0";
            txtJiga.Focus();
        }

        /*************************************************************************************************************************/


        /*************************************************************************************************************************/
        /*--- Descrição ---*/
        private void TimerTick(object sender, EventArgs e)
        {
            if (File.Exists($@"{Directory.GetCurrentDirectory()}\log\SerialList.log"))
            {
                var data = File.ReadAllLines($@"{Directory.GetCurrentDirectory()}\log\SerialList.log");

                LastSerial.Clear();
                if (data.Length > 0)
                {
                    LastSerial.Add(data[data.Length - 1]);
                    if (data.Length > 1)
                    {
                        LastSerial.Add(data[data.Length - 2]);
                        if (data.Length > 2)
                        {
                            LastSerial.Add(data[data.Length - 3]);
                        }
                    }
                }
            }
        }

        /*************************************************************************************************************************/


        /*************************************************************************************************************************/
        /*--- Descrição ---*/
        private void chkOffline_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)chkOnline.IsChecked)
            {
                chkOnline.IsChecked = false;
                chkOffline.IsChecked = true;
                TestModeControl = false;
            }
        }

        /*************************************************************************************************************************/


        /*************************************************************************************************************************/
        /*--- Descrição ---*/
        private void chkOnline_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)chkOffline.IsChecked)
            {
                chkOnline.IsChecked = true;
                chkOffline.IsChecked = false;
                TestModeControl = true;
            }
        }

        /*************************************************************************************************************************/


        /*************************************************************************************************************************/
        /*--- Descrição ---*/
        private void txtJiga_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            //if (txtJiga.Text.Length >= 6 )
            //{
            //    if (txtJiga.Text.Contains("M_FT") || txtJiga.Text.Contains("M_PT") || txtJiga.Text.Contains("M_RC") || txtJiga.Text.Contains("M_LASER") || txtJiga.Text.Contains("AUTO_OBA"))
            //    {
            //        if (txtJiga.Text.Contains("M_FT")) { StationGroup = "FT"; }
            //        else if (txtJiga.Text.Contains("M_PT")) { StationGroup = "PT"; }
            //        else if (txtJiga.Text.Contains("M_RC")) { StationGroup = "RC"; }
            //        else if (txtJiga.Text.Contains("M_LASER")) { StationGroup = "LASER"; }
            //        else if (txtJiga.Text.Contains("AUTO_OBA")) { StationGroup = "AUTO_OBA"; }

            //        using (var writeLog = new WriteLog())
            //        {
            //            writeLog.WriteLogFile($@"Validated hostname");
            //        }
            //        txtJiga.IsEnabled = false;
            //        txtUser.Visibility = Visibility;
            //        txtUser.Focus();
            //    }
            //    else
            //    {
            //        using (var writeLog = new WriteLog())
            //        {
            //            writeLog.WriteLogFile($@"Invalid hostname");
            //        }
            //        MessageBox.Show("HostName invalido!", "Alert", MessageBoxButton.OK, MessageBoxImage.Warning);
            //        txtJiga.Clear();
            //        txtJiga.Focus();
            //        return;
            //    }
            //}

        }

        /*************************************************************************************************************************/


        /*************************************************************************************************************************/
        /*--- Descrição ---*/
        public bool CheckHostNameIsValid()
        {
            if (txtJiga.Text.Contains("M_FT") || txtJiga.Text.Contains("M_PT") || txtJiga.Text.Contains("M_RC") || txtJiga.Text.Contains("M_LASER"))
            {
                if (txtJiga.Text.Contains("M_FT")) { StationGroup = "FT"; }
                else if (txtJiga.Text.Contains("M_PT")) { StationGroup = "PT"; }
                else if (txtJiga.Text.Contains("M_RC")) { StationGroup = "RC"; }
                else if (txtJiga.Text.Contains("M_LASER")) { StationGroup = "LASER"; }
                else if (txtJiga.Text.Contains("AUTO_OBA")) { StationGroup = "AUTO_OBA"; }

                using (var writeLog = new WriteLog())
                {
                    writeLog.WriteLogFile($@"Validated hostname");
                }
                txtUser.Visibility = Visibility;
                txtUser.Focus();
            }
            else
            {
                using (var writeLog = new WriteLog())
                {
                    writeLog.WriteLogFile($@"Invalid hostname");
                }
                MessageBox.Show("HostName invalido!", "Alert", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtJiga.Clear();
                txtJiga.Focus();
            }
            return txtUser.IsVisible;
        }

        /*************************************************************************************************************************/


        /*************************************************************************************************************************/
        /*--- Descrição ---*/
        private void txtUser_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                txtPassWord.Visibility = Visibility.Visible;
                txtPassWord.Focus();
            }
            else if (e.Key == Key.Enter && (txtJiga.Text.Length >= 13 || txtJiga.Text.Length < 12))
            {
                MessageBox.Show("HostName invalido", "Alert", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /*************************************************************************************************************************/


        /*************************************************************************************************************************/
        /*--- Descrição ---*/
        private void txtPassWord_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CheckUserIsValid();
            }
        }

        /*************************************************************************************************************************/


        /*************************************************************************************************************************/
        /*--- Descrição ---*/
        public bool CheckUserIsValid()
        {
            txtUser.IsEnabled = false;
            Thread.Sleep(100);
            using (var webServices = new WebServiceMethods())
            {
                try
                {
                    var res = webServices.SFIS_LOGIN(txtUser.Text, txtPassWord.Password, AppConfiguration.ProductLine, StationGroup, txtJiga.Text);
                    if (res.StatusCode == "1")
                    {
                        using (var writeLog = new WriteLog())
                        {
                            writeLog.WriteLogFile($@"Unauthorized user");
                        }

                        if (res.ErrorMessage == "Hostname not registed in SFIS!")
                        {
                            MessageBox.Show($@"{ res.ErrorMessage } ", "Alert", MessageBoxButton.OK, MessageBoxImage.Warning);
                            txtPassWord.Clear();
                            txtPassWord.Visibility = Visibility.Hidden;
                            txtUser.IsEnabled = true;
                            txtUser.Clear();
                            txtUser.Visibility = Visibility.Hidden;
                            txtJiga.Clear();
                            txtJiga.IsEnabled = true;
                            txtJiga.Focus();
                        }
                        else
                        {
                            MessageBox.Show($@"{ res.ErrorMessage } ", "Alert", MessageBoxButton.OK, MessageBoxImage.Warning);
                            txtPassWord.Clear();
                            txtPassWord.Visibility = Visibility.Hidden;
                            txtUser.IsEnabled = true;
                            txtUser.Clear();
                            txtUser.Focus();
                        }

                    }
                    else
                    {
                        using (var writeLog = new WriteLog())
                        {
                            writeLog.WriteLogFile($@"Authorized User");
                        }

                        txtUser.IsEnabled = false;
                        chkOnline.Visibility = Visibility;
                        chkOffline.Visibility = Visibility;
                        btLogin.Visibility = Visibility;
                    }

                }
                catch (Exception ex)
                {

                    MessageBox.Show(ex.Message, "Alert", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            return txtUser.IsEnabled;
        }

        /*************************************************************************************************************************/


        /*************************************************************************************************************************/
        /*--- Descrição ---*/
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            KillProcess();

            using (var writeLog = new WriteLog())
            {
                writeLog.WriteLogFile("WebServerSFC Closed");
            }
        }

        /*************************************************************************************************************************/


        /*************************************************************************************************************************/
        /*--- Descrição ---*/
        private void chkOnline_Click(object sender, RoutedEventArgs e)
        {
            if (!(bool)chkOffline.IsChecked)
            {
                chkOnline.IsChecked = true;
            }
        }

        /*************************************************************************************************************************/


        /*************************************************************************************************************************/
        /*--- Descrição ---*/
        private void chkOffline_Click(object sender, RoutedEventArgs e)
        {
            if (!(bool)chkOffline.IsChecked)
            {
                chkOffline.IsChecked = true;

            }
        }

        /*************************************************************************************************************************/


        /*************************************************************************************************************************/
        /*--- Descrição ---*/
        private void txtJiga_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CheckHostNameIsValid();
            }
            else if (e.Key == Key.Enter && (txtJiga.Text.Length >= 13 || txtJiga.Text.Length < 12))
            {
                MessageBox.Show("HostName invalido", "Alert", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /*************************************************************************************************************************/


        /*************************************************************************************************************************/
        /*--- Inicia a operação de interfaceamento com o WebService ---*/
        private void btLogin_Click(object sender, RoutedEventArgs e)
        {
            HostName = txtJiga.Text;
            OperatorId = txtUser.Text;
            OperatorPassWord = txtPassWord.Password.ToString();

            string station = string.Empty;

            if (txtJiga.Text.Contains("M_FT")) { station = "Path_Test_FT"; }
            else if (txtJiga.Text.Contains("M_PT")) { station = "Path_Test_PT"; }
            else if (txtJiga.Text.Contains("M_RC")) { station = "Path_Test_RC"; }
            else if (txtJiga.Text.Contains("M_LASER")) { station = "Path_Test_LASER"; }
            else if (txtJiga.Text.Contains("AUTO_OBA")) { station = "Path_Test_AUTO_OBA"; }


            using (var writeLog = new WriteLog())
            {
                if ((bool)chkOnline.IsChecked)
                {
                    writeLog.WriteLogFile($@"Starting {ConfigurationManager.AppSettings[station]} online mode");
                }
                else
                {
                    writeLog.WriteLogFile($@"Starting {ConfigurationManager.AppSettings[station]} offline mode");
                }
            }

            using (var setMode = new SetStationinfoOnOff())
            {
                setMode.SetOnOff((bool)chkOnline.IsChecked);
            }

            var Monitor = new ComPortMonitor();
            this.Hide();
            Monitor.ShowDialog();
            this.Close();
        }

        /*************************************************************************************************************************/


        /*************************************************************************************************************************/
        /*--- Encerra o aplicativo de teste ---*/
        public void KillProcess()
        {
            try
            {
                foreach (Process proc in Process.GetProcessesByName("javaw"))
                {
                    proc.Kill();
                }

                foreach (Process proc in Process.GetProcessesByName("cmd"))
                {
                    proc.Kill();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"MainWindow.xaml.cs Flag-395: {ex.Message}", "Alert", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }



        /*************************************************************************************************************************/
    }
}
