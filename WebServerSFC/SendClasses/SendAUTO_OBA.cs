using SentinelaRoku.ServiceReferenceTEST;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.IO.Ports;
using System.Windows;
using WebServerSFC;
using WebServerSFC.Classes;

namespace SentinelaRoku.SendClasses
{
    class SendAUTO_OBA : IDisposable
    {
        private DataTable tableErrorCode;
        private DataTable tableStation;
        private string operatorID;
        private string productLine;
        private string hostName;
        private string groupName;

        /*************************************************************************************************************************/
        /*--- Construtor ---*/
        public SendAUTO_OBA(string id, string host, string group)
        {
            operatorID = id;
            productLine = ConfigurationManager.AppSettings["PRODUCT_LINE"];
            hostName = host;
            groupName = group;

            LoadDataTableHostnamesROKU();
            LoadDataTableErrorCode();
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

                    MessageBox.Show(ex.Message, "Alert", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

        }

        /*************************************************************************************************************************/


        /*************************************************************************************************************************/
        /*--- Lê o arquivo ErrorCode.xlsx para a DataTable tableErrorCode ---*/
        private void LoadDataTableErrorCode()
        {
            tableErrorCode = new DataTable();

            string fileName = $@"{Directory.GetCurrentDirectory()}\TableErrorCodeROKU\ErrorCode.xlsx";

            using (OleDbConnection conn = this.returnConnection(fileName))
            {
                try
                {
                    conn.Open();
                    // retrieve the data using data adapter
                    OleDbDataAdapter sheetAdapter = new OleDbDataAdapter($"select * from [AUTO_OBA$]", conn);
                    sheetAdapter.Fill(tableErrorCode);
                    conn.Close();

                }
                catch (Exception ex)
                {

                    MessageBox.Show(ex.Message, "Alert", MessageBoxButton.OK, MessageBoxImage.Warning);
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
        /*--- Analisa a mensagem recebida do teste e executa a ação correspondente ---*/
        public List<object> messageAnalysis(string Receive)
        {
            List<object> resultList = new List<object>();

            bool resultTest = false;
            string testAnswer = string.Empty;


            /*--- Análise ---*/
            try
            {
                if (Receive.Contains("1>>")) //Consulta no WebService
                {
                    string message = Receive.Substring(3);

                    string[] componetMessage = message.Split(',');

                    string SN = componetMessage[0];
                    string HostNameTest = componetMessage[1];
                    string GroupNameTest = componetMessage[2];

                    DataRow[] consultHostName = tableStation.Select($"Test_Station LIKE '%{HostNameTest}%'");

                    string Hostname = consultHostName[0]["SFC_HostName"].ToString();
                    string GroupName = consultHostName[0]["SFC_Station"].ToString();

                    if (Hostname == hostName)
                    {
                        /*--- Consulta no WebService ---*/
                        var webservice = new WebServiceMethods();

                        var resultCheckStatus = webservice.SFIS_CHECK_STATUS(SN, GroupName);
                        var resultGetData = webservice.SFIS_GET_DATA(SN);

                        string CheckStatusErrorMessage = resultCheckStatus.ErrorMessage;
                        string GetDataErrorMessage = resultGetData.ErrorMessage;

                        string PN = resultGetData.Configuration.Sku;
                        DeviceDetail[] details = resultGetData.Configuration.DeviceDetails;

                        string CSN = string.Empty;
                        string CESN = string.Empty;

                        foreach (DeviceDetail detail in details)
                        {
                            if (detail.Key == "CSN")
                            {
                                CSN = detail.Value;
                            }

                            if (detail.Key == "CESN")
                            {
                                CESN = detail.Value;
                            }
                        }

                        /*-----------------------------------------------------------------------------------------------------------------------*/


                        /*--- Responde para o teste ---*/
                        if (resultCheckStatus.StatusCode == "0") //check OK
                        {
                            resultTest = true;

                            testAnswer = $"{DateTime.Now.ToString("HH:mm:ss:fff")} flag=2;SMO->UI:1>>SERIALNO={SN},CSN={CSN},CESN={CESN},PNNAME={PN}#OK,UNIT STATUS IS VALID";
                        }
                        else if (resultCheckStatus.StatusCode == "1")   //check not OK
                        {
                            resultTest = false;

                            testAnswer = $"{DateTime.Now.ToString("HH:mm:ss:fff")} flag=2;SMO->UI:1>>SERIALNO={SN},CSN={CSN},CESN={CESN},PNNAME={PN}#{CheckStatusErrorMessage}";
                        }

                        SendMessageToTest(testAnswer);

                        /*-----------------------------------------------------------------------------------------------------------------------*/
                    }
                    else
                    {
                        SendMessageToTest($"{DateTime.Now.ToString("HH:mm:ss:fff")} flag=2;SMO->UI:1>>SERIALNO={SN},PNNAME=#Wrong hostname!");
                        MessageBox.Show("Wrong hostname received!" + Environment.NewLine + $"Selected hostname: {hostName}" + Environment.NewLine + $"Received hostname: {Hostname}", "Alert", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else if (Receive.Contains("2>>")) //Logout no Webservice
                {
                    string message = Receive.Substring(3);

                    message = message.Replace(';', ',');
                    string[] componetMessage = message.Split(',');

                    string SN = componetMessage[0];
                    string CSN = componetMessage[1].Split('=')[1];
                    string CESN = componetMessage[2].Split('=')[1];
                    string ResultTest = componetMessage[3].Split('#')[1];

                    if (ResultTest == "PASS")
                    {
                        /*--- Resposta para o teste ---*/
                        testAnswer = $"{DateTime.Now.ToString("HH:mm:ss:fff")} flag=2;SMO->UI:2>>SERIALNO={SN}#OK,UNIT PASS!";

                        SendMessageToTest(testAnswer); //devemos devolver a resposta para o teste no step 2 o mais rapido possível, pode haver problema de timeout

                        /*-----------------------------------------------------------------------------------------------------------------------*/

                        var webservice = new WebServiceMethods();


                        /*--- Logout do SN ---*/
                        var resultLogout = webservice.SFIS_LOGOUT(SN, operatorID, productLine, groupName, hostName, "0");

                        if (resultLogout.StatusCode == "1")
                        {
                            MessageBox.Show(resultLogout.ErrorMessage, "Alert", MessageBoxButton.OK, MessageBoxImage.Warning);

                            using (var writeLog = new WriteLog())
                            {
                                writeLog.WriteLogFile(resultLogout.ErrorMessage);
                            }
                        }

                        /*-----------------------------------------------------------------------------------------------------------------------*/
                    }
                    else // Test result Fail
                    {
                        /*--- Resposta para o teste ---*/
                        testAnswer = $"{DateTime.Now.ToString("HH:mm:ss:fff")} flag=2;SMO->UI:2>>SERIALNO={SN}#{ResultTest}";

                        SendMessageToTest(testAnswer); //devemos devolver a resposta para o teste no step 2 o mais rapido possível, pode haver problema de timeout

                        /*-----------------------------------------------------------------------------------------------------------------------*/

                        var webservice = new WebServiceMethods();

                        /*--- Logout do SN ---*/
                        DataRow[] consultErrorCode = tableErrorCode.Select($"Description LIKE '%{ResultTest}%'");

                        if (consultErrorCode.Length <= 0)
                        {
                            consultErrorCode = tableErrorCode.Select($"CODE LIKE '%{ResultTest}%'");
                        }

                        string ErrorCode = consultErrorCode[0]["CODE"].ToString();

                        var resultLogout = webservice.SFIS_LOGOUT(SN, operatorID, productLine, groupName, hostName, ErrorCode);

                        if (resultLogout.StatusCode == "1")
                        {
                            MessageBox.Show(resultLogout.ErrorMessage, "Alert", MessageBoxButton.OK, MessageBoxImage.Warning);

                            using (var writeLog = new WriteLog())
                            {
                                writeLog.WriteLogFile(resultLogout.ErrorMessage);
                            }
                        }

                        /*-----------------------------------------------------------------------------------------------------------------------*/
                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Alert", MessageBoxButton.OK, MessageBoxImage.Warning);
                throw;
            }
            /*-----------------------------------------------------------------------------------------------------------------------*/


            resultList.Add(resultTest);
            resultList.Add(testAnswer);

            return resultList;
        }

        /*************************************************************************************************************************/


        /*************************************************************************************************************************/
        /*--- Escreve o arquivo de resposta para o teste na pasta C:\SFCDATA_IN ---*/
        public List<object> SendMessageToTest(string sendMessage)
        {
            List<object> resultList = new List<object>();

            bool resultTest = false;
            string resultMessage = string.Empty;


            /*--- Constroi o arquivo de resposta e salva na pasta do teste ---*/
            try
            {
                //escreve uma string num arquivo, cria o arquivo se não existir
                string[] messageForTest = { Environment.NewLine, sendMessage };

                File.AppendAllLines($@"{ConfigurationManager.AppSettings["LogFileOBA"]}\{DateTime.Now.ToString("yyyyMMdd")}-Log.txt", messageForTest);

                using (var writeLog = new WriteLog())
                {
                    writeLog.WriteLogFile($"File sent for testing: {$@"{ConfigurationManager.AppSettings["LogFileOBA"]}\{DateTime.Now.ToString("yyyyMMdd")}-Log.txt"}");
                }
            }
            catch (Exception ex)
            {
                using (var writeLog = new WriteLog())
                {
                    writeLog.WriteLogFile($"SendAUTO_OBA.cs Flag-OnFileChanged: {ex.Message}");
                }

                MessageBox.Show($"SendAUTO_OBA.cs Flag-: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }

            /*-----------------------------------------------------------------------------------------------------------------------*/


            resultList.Add(resultTest);
            resultList.Add(resultMessage);

            return resultList;
        }

        /*************************************************************************************************************************/


        /*************************************************************************************************************************/
        /*--- Destructor ---*/
        ~SendAUTO_OBA()
        {
            this.Dispose();
        }
        /*************************************************************************************************************************/


        /*************************************************************************************************************************/
        /*--- Dispose ---*/
        public void Dispose()
        {
            try
            {
                // release scarce resource here
            }
            finally
            {
                GC.SuppressFinalize(this);
            }
        }
        /*************************************************************************************************************************/
    }
}
