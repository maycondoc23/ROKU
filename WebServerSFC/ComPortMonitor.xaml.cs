using System;
using System.Configuration;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using System.Linq;
using WebServerSFC.Classes;
using System.Collections.Generic;
using System.Globalization;
using SentinelaRoku.SendClasses_SFCDATA;
using SentinelaRoku.Entity;

namespace WebServerSFC
{
    /// <summary>
    /// Interaction logic for ComPortMonitor.xaml
    /// </summary>
    public partial class ComPortMonitor : Window
    {
        public SerialPort _serialPort;

        public DataTable sheetData;

        public bool ExistExe = false;
        public string ErrorMessage { get; set; }
        public string dataToSend { get; set; }
        public string StationGroup { get; set; }

        private DataTable tableStation;
        private DataTable tableErrorCode;

        private DataTable tableStation_SFCDATA;
        private DataTable tableErrorCode_SFCDATA;

        DispatcherTimer timer = new DispatcherTimer(); //Exibe os 3 últimos resultados de teste

        private static FileSystemWatcher _monitorarSFCDATA_OUT;

        public List<DateTime> lastMessageTime;


        /*************************************************************************************************************************/
        /*--- Inicialização ---*/
        public ComPortMonitor()
        {
            InitializeComponent();


            LoadDataTableHostnamesROKU();

            DataRow[] consultHostName = tableStation.Select($"SFC_HostName LIKE '%{MainWindow.HostName}%'");

            string Hostname = MainWindow.HostName;
            lbStation.Text = Hostname;

            StationGroup = consultHostName[0]["SFC_Station"].ToString();

            LoadDataTableErrorCode(StationGroup);

            if (MainWindow.TestModeControl)
            {
                lbControl.Text = "ONLINE";
                lbControl.Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 100));
                btExit.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            }
            else
            {
                lbControl.Text = "OFFLINE";
                lbControl.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 30));
                btExit.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));

            }

            //string caminhoArquivo = @"C:\caminho\para\seu\arquivo.txt"; // Caminho do arquivo
            //try
            //{
            //    // Lê todo o conteúdo do arquivo
            //    string conteudo = File.ReadAllText(caminhoArquivo);

            //    // Exibe o conteúdo do arquivo
            //    Console.WriteLine(conteudo);
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine("Erro ao ler o arquivo: " + ex.Message);
            //}

            Productname_view.Content = MainWindow.productname;

            SizeChanged += (o, e) =>
            {
                var r = SystemParameters.WorkArea;
                Left = r.Right - ActualWidth;
            };

            this.Title = $"SENTINELA ROKU { Assembly.GetExecutingAssembly().GetName().Version}";

            var desktopWorkingArea = SystemParameters.WorkArea;

            /*--- Exibe os 3 últimos resultados de teste ---*/
            timer.Tick += TimerTick;
            timer.Interval = TimeSpan.FromSeconds(5);
            timer.Start();

            /*-----------------------------------------------------------------------------------------------------------------------*/


            if (MainWindow.TestModeControl)
            {
                this.Background = new SolidColorBrush(Color.FromArgb(80, 0, 255, 30));
            }
            else
            {
                this.Background = new SolidColorBrush(Color.FromArgb(100, 255, 255, 30));
            }


            if (MainWindow.TestModeControl)
            {
                string fileLogMonitor = string.Empty;

                if (Hostname.Contains("PT"))
                {
                    fileLogMonitor = ConfigurationManager.AppSettings["LogFilePT"];
                }
                else if (Hostname.Contains("CAL"))
                {
                    fileLogMonitor = ConfigurationManager.AppSettings["LogFileCAL"];
                }
                else if (Hostname.Contains("FT"))
                {
                    fileLogMonitor = ConfigurationManager.AppSettings["LogFileFT"];
                }
                else if (Hostname.Contains("RC"))
                {
                    fileLogMonitor = ConfigurationManager.AppSettings["LogFileRC"];
                }
                else if (Hostname.Contains("LASER"))
                {
                    fileLogMonitor = ConfigurationManager.AppSettings["LogFileLASER"];
                }
                else if (Hostname.Contains("OBA"))
                {
                    fileLogMonitor = ConfigurationManager.AppSettings["LogFileOBA"];
                }    

                MonitorarArquivos_SFCDATA_OUT(ConfigurationManager.AppSettings["SFCDATA_OUT"], "*.txt", OnFileCreated);

                if (!ExistExe)
                {
                    using (var programStart = new ProgramStart())
                    {
                        ExistExe = programStart.StartClientProgram(StationGroup); //Inicializa a aplicação do teste
                    }
                }
                if (!ExistExe)
                {
                    using (WriteLog writeLog = new WriteLog())
                    {
                        writeLog.WriteLogFile("WebServerSFC Closed");
                    }
                        
                    Process aProcess = Process.GetCurrentProcess();
                    aProcess.Kill();
                }
            }
            else
            {
                if (!ExistExe)
                {
                    using (var programStart = new ProgramStart())
                    {
                        ExistExe = programStart.StartClientProgram(StationGroup); //inicializa o programa de teste
                    }
                }

                if (!ExistExe)
                {
                    using (WriteLog writeLog = new WriteLog())
                    {
                        writeLog.WriteLogFile("Sentinela ROKU Closed");
                    }

                    Process aProcess = Process.GetCurrentProcess();
                    aProcess.Kill();
                }
            }
        }

        /*************************************************************************************************************************/


        /*************************************************************************************************************************/
        /*--- Lê o arquivo HostnamesROKU.xlsx para a DataTable tableStation ---*/
        private void LoadDataTableHostnamesROKU()
        {
            tableStation = new DataTable();
            tableStation_SFCDATA = new DataTable();

            string fileName = $@"{Directory.GetCurrentDirectory()}\TableStationTest\HostnamesROKU.xlsx";

            using (OleDbConnection conn = this.returnConnection(fileName))
            {
                try
                {
                    conn.Open();
                    // retrieve the data using data adapter
                    OleDbDataAdapter sheetAdapter = new OleDbDataAdapter($"select * from [Sheet1$]", conn);
                    sheetAdapter.Fill(tableStation);
                    sheetAdapter.Fill(tableStation_SFCDATA);
                    conn.Close();

                }
                catch (Exception ex)
                {
                    using (var writeLog = new WriteLog())
                    {
                        writeLog.WriteLogFile($"ComPortMonitor.xaml.cs Flag-1: {ex.Message}");
                    }
                    MessageBox.Show($"ComPortMonitor.xaml.cs Flag-1: {ex.Message}", "Alert", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

        }

        /*************************************************************************************************************************/


        /*************************************************************************************************************************/
        /*--- Lê o arquivo ErrorCode.xlsx para a DataTable tableErrorCode ---*/
        private void LoadDataTableErrorCode(string groupName)
        {
            tableErrorCode = new DataTable();
            tableErrorCode_SFCDATA = new DataTable();

            string fileName = $@"{Directory.GetCurrentDirectory()}\TableErrorCodeROKU\ErrorCode.xlsx";

            using (OleDbConnection conn = this.returnConnection(fileName))
            {
                try
                {
                    conn.Open();
                    // retrieve the data using data adapter
                    OleDbDataAdapter sheetAdapter = new OleDbDataAdapter($"select * from [{groupName}$]", conn);
                    sheetAdapter.Fill(tableErrorCode);
                    sheetAdapter.Fill(tableErrorCode_SFCDATA);
                    conn.Close();

                }
                catch (Exception ex)
                {
                    using (var writeLog = new WriteLog())
                    {
                        writeLog.WriteLogFile($"ComPortMonitor.xaml.cs Flag-2: {ex.Message}");
                    }
                    MessageBox.Show($"ComPortMonitor.xaml.cs Flag-2: {ex.Message}", "Alert", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

        }

        /*************************************************************************************************************************/


        /*************************************************************************************************************************/
        /*--- Conexão com a planilha de Error Code ---*/
        private OleDbConnection returnConnection(string fileName)
        {
            return new OleDbConnection($@"Provider = Microsoft.ACE.OLEDB.12.0; Data Source = { fileName }; Extended Properties = Excel 12.0;");
        }

        /*************************************************************************************************************************/


        /*************************************************************************************************************************/
        /*--- Exibe os 3 últimos resultados de teste ---*/
        private void TimerTick(object sender, EventArgs e)
        {
            if (MainWindow.LastSerial.Count > 0)
            {
                if (MainWindow.LastSerial[0].Contains("FAIL"))
                {
                    lbSerial1.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                }
                else
                {
                    lbSerial1.Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 100));
                }


                lbSerial1.Content = MainWindow.LastSerial[0].Split(' ')[0];
                lbSerial1.Visibility = Visibility;


                if (MainWindow.LastSerial.Count > 1)
                {
                    if (MainWindow.LastSerial[1].Contains("FAIL"))
                    {
                        lbSerial2.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                    }
                    else
                    {
                        lbSerial2.Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 100));
                    }
                    lbSerial2.Content = MainWindow.LastSerial[1].Split(' ')[0];
                    lbSerial2.Visibility = Visibility;
                }
                else
                {
                    lbSerial2.Content = "";
                }


                if (MainWindow.LastSerial.Count > 2)
                {
                    if (MainWindow.LastSerial[2].Contains("FAIL"))
                    {
                        lbSerial3.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                    }
                    else
                    {
                        lbSerial3.Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 100));
                    }
                    lbSerial3.Content = MainWindow.LastSerial[2].Split(' ')[0];
                    lbSerial3.Visibility = Visibility;
                }
                else
                {
                    lbSerial3.Content = "";
                }
            }
            else
            {
                lbSerial1.Content = "";
                lbSerial2.Content = "";
                lbSerial3.Content = "";
            }
        }

        /*************************************************************************************************************************/


        /*************************************************************************************************************************/
        /*--- Inicializa o monitoramento dos logs de teste do diretório LogFile ---*/
        public static void MonitorarArquivos_SFCDATA_OUT(string path, string filtro, FileSystemEventHandler OnFileCreated)
        {
            _monitorarSFCDATA_OUT = new FileSystemWatcher(path, filtro)
            {
                IncludeSubdirectories = true
            };

            _monitorarSFCDATA_OUT.Created += new FileSystemEventHandler(OnFileCreated);
            //_monitorarSFCDATA_OUT.Changed += OnFileChanged;
            //_monitorarSFCDATA_OUT.Deleted += OnFileChanged;
            //_monitorarSFCDATA_OUT.Renamed += OnFileRenamed;

            _monitorarSFCDATA_OUT.EnableRaisingEvents = true;
            //Console.WriteLine($"Monitorando arquivos e: {filtro}");
        }

        /*************************************************************************************************************************/


        /*************************************************************************************************************************/
        /*--- Verifica a mudança de um arquivo ---*/
        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            _monitorarSFCDATA_OUT.EnableRaisingEvents = false;

            using (var writeLog = new WriteLog())
            {
                writeLog.WriteLogFile($@"File Out Created: {e.FullPath}.");
            }

            try
            {
                string fileLog = string.Empty;

                try
                {
                    fileLog = System.IO.File.ReadLines(e.FullPath).Last(x => x.Length > 0);
                }
                catch (Exception)
                {
                    try
                    {
                        System.Threading.Thread.Sleep(200);
                        fileLog = System.IO.File.ReadLines(e.FullPath).Last(x => x.Length > 0);
                    }
                    catch (Exception)
                    {
                        try
                        {
                            System.Threading.Thread.Sleep(200);
                            fileLog = System.IO.File.ReadLines(e.FullPath).Last(x => x.Length > 0);
                        }
                        catch (Exception ex)
                        {
                            using (var writeLog = new WriteLog())
                            {
                                writeLog.WriteLogFile($"ComPortMonitor.xaml.cs Flag-5: {ex.Message}");
                            }
                            MessageBox.Show($"ComPortMonitor.xaml.cs Flag-5: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }

                if (fileLog.Contains(">>") && fileLog.Contains("#")) //indica que a mensagem partiu do teste para ser verificada no webservice
                {
                    RegMessageAnalysis regMessageAnalysis = new RegMessageAnalysis();

                    switch (StationGroup)
                    {
                        case "PT":

                            using ( var writelog = new WriteLog())
                            {
                                writelog.WriteLogFile($"HOSTNAME {MainWindow.HostName}");
                            }

                            if (MainWindow.productname.Trim().ToString() == "BAYSIDE")
                            {

                                using (SendPT_SFCATABAYSIDE sendPT_SFCATA = new SendPT_SFCATABAYSIDE(MainWindow.OperatorId, MainWindow.HostName, StationGroup, tableStation_SFCDATA, tableErrorCode_SFCDATA))
                                {
                                    regMessageAnalysis = sendPT_SFCATA.messageAnalysis(fileLog);
                                }
                                break;

                            }

                            else 
                            {

                                using (SendPT_SFCATA sendPT_SFCATA = new SendPT_SFCATA(MainWindow.OperatorId, MainWindow.HostName, StationGroup, tableStation_SFCDATA, tableErrorCode_SFCDATA))
                                {
                                    regMessageAnalysis = sendPT_SFCATA.messageAnalysis(fileLog);
                                }
                                break;

                            }

                                
                        case "CAL":

                            using (SendCAL_SFCDATA sendCAL_SFCATA = new SendCAL_SFCDATA(MainWindow.OperatorId, MainWindow.HostName, StationGroup, tableStation_SFCDATA, tableErrorCode_SFCDATA))
                            {
                                regMessageAnalysis = sendCAL_SFCATA.messageAnalysis(fileLog);
                            }

                            break;

                        case "FT":

                            if (MainWindow.productname.Trim().ToString() == "BAYSIDE")
                            {
                                using (SendFT_SFCDATABAYSIDE sendFT_SFCDATA = new SendFT_SFCDATABAYSIDE(MainWindow.OperatorId, MainWindow.HostName, StationGroup, tableStation_SFCDATA, tableErrorCode_SFCDATA))
                                {
                                    regMessageAnalysis = sendFT_SFCDATA.messageAnalysis(fileLog);
                                }
                            }

                            else
                            {
                                using (SendFT_SFCDATA sendFT_SFCDATA = new SendFT_SFCDATA(MainWindow.OperatorId, MainWindow.HostName, StationGroup, tableStation_SFCDATA, tableErrorCode_SFCDATA))
                                {
                                    regMessageAnalysis = sendFT_SFCDATA.messageAnalysis(fileLog);
                                }

                            }
                            break;

                        case "RC":

                            if (MainWindow.productname.Trim().ToString() == "BAYSIDE")
                            {

                                using (SendRC_SFCDATABAYSIDE sendRC_SFCDATA = new SendRC_SFCDATABAYSIDE(MainWindow.OperatorId, MainWindow.HostName, StationGroup, tableStation_SFCDATA, tableErrorCode_SFCDATA))
                                {
                                    regMessageAnalysis = sendRC_SFCDATA.messageAnalysis(fileLog);
                                }

                            }
                            else
                            {

                                using (SendRC_SFCDATA sendRC_SFCDATA = new SendRC_SFCDATA(MainWindow.OperatorId, MainWindow.HostName, StationGroup, tableStation_SFCDATA, tableErrorCode_SFCDATA))
                                {
                                    regMessageAnalysis = sendRC_SFCDATA.messageAnalysis(fileLog);
                                }

                            }

                            break;

                        case "LASER":

                            using (SendLASER_SFCDATA sendLASE_SFCDATAR = new SendLASER_SFCDATA(MainWindow.OperatorId, MainWindow.HostName, StationGroup, tableStation_SFCDATA, tableErrorCode_SFCDATA))
                            {
                                regMessageAnalysis = sendLASE_SFCDATAR.messageAnalysis(fileLog);
                            }

                            break;

                        case "AUTO_OBA":

                            using (SendAUTO_OBA_SFCDATA sendAUTO_OBA_SFCDATA = new SendAUTO_OBA_SFCDATA(MainWindow.OperatorId, MainWindow.HostName, StationGroup, tableStation_SFCDATA, tableErrorCode_SFCDATA))
                            {
                                regMessageAnalysis = sendAUTO_OBA_SFCDATA.messageAnalysis(fileLog);
                            }

                            break;

                        default:
                            MessageBox.Show($"Unidentified station {StationGroup}.", "Alert", MessageBoxButton.OK, MessageBoxImage.Warning);
                            using (var writeLog = new WriteLog())
                            {
                                writeLog.WriteLogFile($"Unidentified station {StationGroup}.");
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                using (var writeLog = new WriteLog())
                {
                    writeLog.WriteLogFile($"ComPortMonitor.xaml.cs Flag-6: {ex.Message}");
                }

                MessageBox.Show($"ComPortMonitor.xaml.cs Flag-6: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);

            }

            if (File.Exists(e.FullPath)) File.Delete(e.FullPath);

            _monitorarSFCDATA_OUT.EnableRaisingEvents = true;
        }

        /*************************************************************************************************************************/


        /*************************************************************************************************************************/
        /*--- Sair da aplicação ---*/
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Productname_view_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Productname_view_Click_1(object sender, RoutedEventArgs e)
        {

        }

        /*************************************************************************************************************************/

    }

}
