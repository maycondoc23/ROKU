using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace WebServerSFC.Classes
{
    class ProgramStart : IDisposable
    {
        public bool StartClientProgram(string StationGroup)
        {
            bool Control = false;
            try
            {
                string path_test = string.Empty;
                string directory_test = string.Empty;

                if (StationGroup == "FT")
                {
                    path_test = "Path_Test_FT";
                    directory_test = "Directory_Test_FT";
                }
                else if (StationGroup == "PT")
                {
                    path_test = "Path_Test_PT";
                    directory_test = "Directory_Test_PT";
                }
                else if (StationGroup == "RC")
                {
                    path_test = "Path_Test_RC";
                    directory_test = "Directory_Test_RC";
                }
                else if (StationGroup == "LASER")
                {
                    path_test = "Path_Test_LASER";
                    directory_test = "Directory_Test_LASER";
                }
                else if (StationGroup == "AUTO_OBA")
                {
                    path_test = "Path_Test_AUTO_OBA";
                    directory_test = "Directory_Test_AUTO_OBA";
                }
                else if (StationGroup == "CAL")
                {
                    path_test = "Path_Test_CAL";
                    directory_test = "Directory_Test_CAL";
                }


                var myProcess = new Process();
                string filePath = ConfigurationManager.AppSettings[path_test];

                if (File.Exists(filePath))
                {
                    ProcessStartInfo AmbitUI = new ProcessStartInfo();
                    AmbitUI.WorkingDirectory = ConfigurationManager.AppSettings[directory_test];
                    AmbitUI.FileName = filePath;
                    Process.Start(AmbitUI);
                    System.Threading.Thread.Sleep(5500);

                    Control = true;
                }
                else
                {
                    using (var writeLog = new WriteLog())
                    {
                        writeLog.WriteLogFile($@"Was not possible to found {filePath}");
                    }
                    MessageBox.Show($@"Was not possible to found {filePath}", "Alert", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Alert", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            return Control;
        }

        public void Dispose()
        {
            //throw new NotImplementedException();
        }
    }
}
