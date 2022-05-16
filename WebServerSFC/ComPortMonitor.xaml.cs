using SentinelaRoku.SendClasses;
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

        DispatcherTimer timer = new DispatcherTimer(); //Exibe os 3 últimos resultados de teste

        private static FileSystemWatcher _monitorarLogOUT;

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

                MonitorarArquivos(fileLogMonitor, "*.txt", OnFileChanged);

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

            string fileName = $@"{Directory.GetCurrentDirectory()}\TableStationTest\HostnamesROKU.xlsx";

            using (OleDbConnection conn = this.returnConnection(fileName))
            {
                try
                {
                    conn.Open();
                    // retrieve the data using data adapter
                    OleDbDataAdapter sheetAdapter = new OleDbDataAdapter($"select * from [Sheet1$]", conn);
                    sheetAdapter.Fill(tableStation);
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

            string fileName = $@"{Directory.GetCurrentDirectory()}\TableErrorCodeROKU\ErrorCode.xlsx";

            using (OleDbConnection conn = this.returnConnection(fileName))
            {
                try
                {
                    conn.Open();
                    // retrieve the data using data adapter
                    OleDbDataAdapter sheetAdapter = new OleDbDataAdapter($"select * from [{groupName}$]", conn);
                    sheetAdapter.Fill(tableErrorCode);
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
        /*--- Inicializa o monitoramento dos logs de teste ---*/
        public static void MonitorarArquivos(string path, string filtro, FileSystemEventHandler OnFileChanged)
        {
            _monitorarLogOUT = new FileSystemWatcher(path, filtro)
            {
                IncludeSubdirectories = true
            };

            //_monitorarLogOUT.Created += new FileSystemEventHandler(OnFileChanged);
            _monitorarLogOUT.Changed += OnFileChanged;
            //_monitorarLogOUT.Deleted += OnFileChanged;
            //_monitorarLogOUT.Renamed += OnFileRenamed;

            _monitorarLogOUT.EnableRaisingEvents = true;
            //Console.WriteLine($"Monitorando arquivos e: {filtro}");
        }

        /*************************************************************************************************************************/


        /*************************************************************************************************************************/
        /*--- Verifica a mudança de um arquivo ---*/
        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            _monitorarLogOUT.EnableRaisingEvents = false;

            using (var writeLog = new WriteLog())
            {
                writeLog.WriteLogFile($@"File Out Changed {e.FullPath}.");
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
                                writeLog.WriteLogFile($"ComPortMonitor.xaml.cs Flag-3: {ex.Message}");
                            }
                            MessageBox.Show($"ComPortMonitor.xaml.cs Flag-3: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }

                if (fileLog.Contains("flag=2;UI->SMO:") && fileLog.Contains("#")) //indica que a mensagem partiu do teste para ser verificada no webservice
                {
                    DateTime timeMessage = DateTime.ParseExact(fileLog.Split(' ')[0], "HH:mm:ss:fff", CultureInfo.InvariantCulture);

                    if ((lastMessageTime == null) || (DateTime.Compare(timeMessage, lastMessageTime[lastMessageTime.Count - 1]) == 1) )
                    {
                        string receivedData = fileLog.Split(':')[4];

                        switch (StationGroup)
                        {
                            case "PT":

                                using (SendPT sendPT = new SendPT(MainWindow.OperatorId, MainWindow.HostName, StationGroup, tableStation, tableErrorCode))
                                {
                                    sendPT.messageAnalysis(receivedData);
                                }

                                break;

                            case "FT":

                                using (SendFT sendFT = new SendFT(MainWindow.OperatorId, MainWindow.HostName, StationGroup, tableStation, tableErrorCode))
                                {
                                    sendFT.messageAnalysis(receivedData);
                                }

                                break;

                            case "RC":

                                using (SendRC sendRC = new SendRC(MainWindow.OperatorId, MainWindow.HostName, StationGroup, tableStation, tableErrorCode))
                                {
                                    sendRC.messageAnalysis(receivedData);
                                }

                                break;

                            case "LASER":

                                using (SendLASER sendLASER = new SendLASER(MainWindow.OperatorId, MainWindow.HostName, StationGroup, tableStation, tableErrorCode))
                                {
                                    sendLASER.messageAnalysis(receivedData);
                                }

                                break;

                            case "AUTO_OBA":

                                using (SendAUTO_OBA sendAUTO_OBA = new SendAUTO_OBA(MainWindow.OperatorId, MainWindow.HostName, StationGroup, tableStation, tableErrorCode))
                                {
                                    sendAUTO_OBA.messageAnalysis(receivedData);
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

                        using (var writeLog = new WriteLog())
                        {
                            writeLog.WriteLogFile($"Mensagem enviada para verificação: {receivedData}");
                        }

                    }
                }

                using (var writeLog = new WriteLog())
                {
                    writeLog.WriteLogFile($"Mensagem recebidado do teste: {fileLog}");
                }
            }
            catch (Exception ex)
            {
                using (var writeLog = new WriteLog())
                {
                    writeLog.WriteLogFile($"ComPortMonitor.xaml.cs Flag-4: {ex.Message}");
                }

                MessageBox.Show($"ComPortMonitor.xaml.cs Flag-4: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);

            }

            _monitorarLogOUT.EnableRaisingEvents = true;
        }

        /*************************************************************************************************************************/


        /*************************************************************************************************************************/
        /*--- Sair da aplicação ---*/
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /*************************************************************************************************************************/

    }

}
