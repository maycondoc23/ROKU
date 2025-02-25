using SentinelaRoku;
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

        public static string productname = "";

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

            if(System.IO.File.Exists($@"{Directory.GetCurrentDirectory()}\LogWebService\LogWebService.txt"))
            {
                System.IO.File.Delete($@"{Directory.GetCurrentDirectory()}\LogWebService\LogWebService.txt");
            }
               
            try
            {
                KillProcess();
            }
            catch (Exception ex)
            {
                using (var writeLog = new WriteLog())
                {
                    writeLog.WriteLogFile($"MainWindow.xaml.cs Flag-1: {ex.Message}");
                }

                MessageBox.Show($"MainWindow.xaml.cs Flag-1: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            ClearLogFiles();

            DispatcherTimer timer = new DispatcherTimer();
            timer.Tick += TimerTick;
            timer.Interval = TimeSpan.FromSeconds(5);
            timer.Start();

            this.Title = $"SENTINELA ROKU 8.0.15 - Check Status";
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

            if (System.IO.File.Exists($@"{Directory.GetCurrentDirectory()}\LogWebService\LogWebService.txt"))
            {
                try
                {
                    string messageWS = System.IO.File.ReadAllText($@"{Directory.GetCurrentDirectory()}\LogWebService\LogWebService.txt");

                    AlertMessage alertMessage = new AlertMessage();
                    alertMessage.InsertMessage(messageWS);
                    alertMessage.ShowDialog();

                    if (System.IO.File.Exists($@"{Directory.GetCurrentDirectory()}\LogWebService\LogWebService.txt"))
                        System.IO.File.Delete($@"{Directory.GetCurrentDirectory()}\LogWebService\LogWebService.txt");
                }
                catch (Exception ex)
                {
                    using (var writeLog = new WriteLog())
                    {
                        writeLog.WriteLogFile($"MainWindow.xaml.cs Flag-2: {ex.Message}");
                    }
                    MessageBox.Show($"MainWindow.xaml.cs Flag-2: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
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
        public bool CheckHostNameIsValid()
        {
            if (txtJiga.Text.Contains("M_FT") || txtJiga.Text.Contains("M_PT") || txtJiga.Text.Contains("M_CAL") || txtJiga.Text.Contains("M_RC") || txtJiga.Text.Contains("M_LASER") || txtJiga.Text.Contains("AUTO_OBA"))
            {
                if (txtJiga.Text.Contains("M_FT")) { StationGroup = "FT"; }
                else if (txtJiga.Text.Contains("M_PT")) { StationGroup = "PT"; }
                else if (txtJiga.Text.Contains("M_CAL")) { StationGroup = "CAL"; }
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
                    var res = webServices.SFIS_LOGIN(txtUser.Text, txtPassWord.Password, AppConfiguration.ProductLine, StationGroup, txtJiga.Text.Trim());
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
                    using (var writeLog = new WriteLog())
                    {
                        writeLog.WriteLogFile($"MainWindow.xaml.cs Flag-3: {ex.Message}");
                    }
                    MessageBox.Show($"MainWindow.xaml.cs Flag-3: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
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
            HostName = txtJiga.Text.Trim();
            OperatorId = txtUser.Text.Trim();
            OperatorPassWord = txtPassWord.Password.ToString().Trim();
            productname = Produto.SelectionBoxItem.ToString().Trim();
            string station = string.Empty;





            if (txtJiga.Text.Contains("M_FT")) { station = "Path_Test_FT"; }
            else if (txtJiga.Text.Contains("M_CAL")) { station = "Path_Test_CAL"; }
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
                using (var writeLog = new WriteLog())
                {
                    writeLog.WriteLogFile($"MainWindow.xaml.cs Flag-4: {ex.Message}");
                }
                MessageBox.Show($"MainWindow.xaml.cs Flag-4: {ex.Message}", "Alert", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /*************************************************************************************************************************/


        /*************************************************************************************************************************/
        /*--- Limpa os pastas SFCDATA_OUT e SFCDATA_IN ---*/
        public void ClearLogFiles()
        {
            try
            {
                string[] filesOut = Directory.GetFiles($@"{ConfigurationManager.AppSettings["SFCDATA_OUT"]}", "*txt");

                foreach (string fileOut in filesOut)
                {
                    if (File.Exists(fileOut)) File.Delete(fileOut);
                }
            }
            catch (Exception ex)
            {
                using (var writeLog = new WriteLog())
                {
                    writeLog.WriteLogFile($"MainWindow.xaml.cs Flag-5: {ex.Message}");
                }

                MessageBox.Show($"MainWindow.xaml.cs Flag-5: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }


            try
            {
                string[] filesOut = Directory.GetFiles($@"{ConfigurationManager.AppSettings["SFCDATA_IN"]}", "*txt");

                foreach (string fileOut in filesOut)
                {
                    if (File.Exists(fileOut)) File.Delete(fileOut);
                }
            }
            catch (Exception ex)
            {
                using (var writeLog = new WriteLog())
                {
                    writeLog.WriteLogFile($"MainWindow.xaml.cs Flag-6: {ex.Message}");
                }

                MessageBox.Show($"MainWindow.xaml.cs Flag-6: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        private void txtJiga_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

        }

        private void txtJiga_TextChanged_1(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

        }

        private void PRODUCTNAME_SelectionChanged_1(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

        }

        /*************************************************************************************************************************/
    }
}
