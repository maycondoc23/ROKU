using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WebServerSFC.Classes;

namespace SentinelaRoku.SendClasses_SFCDATA
{
    class SendFT_SFCDATA: IDisposable
    {
        private DataTable tableErrorCode;
        private DataTable tableStation;
        private string operatorID;
        private string productLine;
        private string hostName;
        private string groupName;

        /*************************************************************************************************************************/
        /*--- Construtor ---*/
        public SendFT_SFCDATA(string id, string host, string group, DataTable stations, DataTable errorCodes)
        {
            operatorID = id;
            productLine = ConfigurationManager.AppSettings["PRODUCT_LINE"];
            hostName = host;
            groupName = group;
            tableStation = stations;
            tableErrorCode = errorCodes;
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
                        //var webservice = new WebServiceMethods();
                        //var resultGetData = webservice.SFIS_GET_DATA(SN);

                        //string GetDataErrorMessage = resultGetData.ErrorMessage;

                        //string PN = resultGetData.Configuration.Sku;
                        //DeviceDetail[] details = resultGetData.Configuration.DeviceDetails;

                        //string CSN = string.Empty;
                        //string CESN = string.Empty;

                        //foreach (DeviceDetail detail in details)
                        //{
                        //    if (detail.Key == "CSN")
                        //    {
                        //        CSN = detail.Value;
                        //    }

                        //    if (detail.Key == "CESN")
                        //    {
                        //        CESN = detail.Value;
                        //    }
                        //}

                        string PN = "RU9026000643";
                        string CSN = string.Empty;
                        string CESN = string.Empty;

                        /*-----------------------------------------------------------------------------------------------------------------------*/


                        /*--- Responde para o teste ---*/
                        //if (resultGetData.StatusCode == "0") //check OK
                        //{
                        //    resultTest = true;

                        //    testAnswer = $"1>>SERIALNO={SN},CSN={CSN},CESN={CESN},PNNAME={PN}#OK,UNIT STATUS IS VALID";
                        //}
                        //else if (resultGetData.StatusCode == "1")   //check not OK
                        //{
                        //    resultTest = false;

                        //    testAnswer = $"1>>SERIALNO={SN},CSN={CSN},CESN={CESN},PNNAME={PN}#{GetDataErrorMessage}";
                        //}

                        testAnswer = $"1>>SERIALNO={SN},CSN={CSN},CESN={CESN},PNNAME={PN}#OK,UNIT STATUS IS VALID";

                        SendMessageToTest(testAnswer, "start");

                        /*-----------------------------------------------------------------------------------------------------------------------*/
                    }
                    else
                    {
                        SendMessageToTest($"1>>SERIALNO={SN},PNNAME=#Wrong hostname!", "start");
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
                        testAnswer = $"2>>SERIALNO={SN}#OK,UNIT PASS!";

                        SendMessageToTest(testAnswer, "end"); //devemos devolver a resposta para o teste no step 2 o mais rapido possível, pode haver problema de timeout

                        /*-----------------------------------------------------------------------------------------------------------------------*/

                        //var webservice = new WebServiceMethods();


                        /*--- Logout do SN ---*/
                        //var resultLogout = webservice.SFIS_LOGOUT(SN, operatorID, productLine, groupName, hostName, "0");

                        //if (resultLogout.StatusCode == "1")
                        //{
                        //    File.WriteAllText($@"{Directory.GetCurrentDirectory()}\LogWebService\LogWebService.txt", resultLogout.ErrorMessage);

                        //    using (var writeLog = new WriteLog())
                        //    {
                        //        writeLog.WriteLogFile(resultLogout.ErrorMessage);
                        //    }
                        //}

                        /*-----------------------------------------------------------------------------------------------------------------------*/
                    }
                    else // Test result Fail
                    {
                        /*--- Resposta para o teste ---*/
                        testAnswer = $"2>>SERIALNO={SN}#{ResultTest}";

                        SendMessageToTest(testAnswer, "end"); //devemos devolver a resposta para o teste no step 2 o mais rapido possível, pode haver problema de timeout

                        /*-----------------------------------------------------------------------------------------------------------------------*/

                        //var webservice = new WebServiceMethods();

                        /*--- Logout do SN ---*/
                        //DataRow[] consultErrorCode = tableErrorCode.Select($"Description LIKE '%{ResultTest}%'");

                        //if (consultErrorCode.Length <= 0)
                        //{
                        //    consultErrorCode = tableErrorCode.Select($"CODE LIKE '%{ResultTest}%'");
                        //}

                        //string ErrorCode = consultErrorCode[0]["CODE"].ToString();

                        //var resultLogout = webservice.SFIS_LOGOUT(SN, operatorID, productLine, groupName, hostName, ErrorCode);

                        //if (resultLogout.StatusCode == "1")
                        //{
                        //    File.WriteAllText($@"{Directory.GetCurrentDirectory()}\LogWebService\LogWebService.txt", resultLogout.ErrorMessage);

                        //    using (var writeLog = new WriteLog())
                        //    {
                        //        writeLog.WriteLogFile(resultLogout.ErrorMessage);
                        //    }
                        //}

                        /*-----------------------------------------------------------------------------------------------------------------------*/
                    }

                }
            }
            catch (Exception ex)
            {
                using (var writeLog = new WriteLog())
                {
                    writeLog.WriteLogFile($"SendFT_SFCDATA.cs Flag-1: {ex.Message}");
                }
                MessageBox.Show($"SendFT_SFCDATA.cs Flag-1: {ex.Message}", "Alert", MessageBoxButton.OK, MessageBoxImage.Warning);
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
        public List<object> SendMessageToTest(string sendMessage, string testStage)
        {
            List<object> resultList = new List<object>();

            bool resultTest = false;
            string resultMessage = string.Empty;


            /*--- Constroi o arquivo de resposta e salva na pasta do teste ---*/
            try
            {
                //escreve uma string num arquivo, cria o arquivo se não existir

                System.IO.File.WriteAllText($@"{ConfigurationManager.AppSettings["SFCDATA_IN"]}\{DateTime.Now.ToString("yyyyMMddHHmmssfffff")}_{testStage}.txt", sendMessage);

                using (var writeLog = new WriteLog())
                {
                    writeLog.WriteLogFile($@"File sent for testing: {ConfigurationManager.AppSettings["SFCDATA_IN"]}\{DateTime.Now.ToString("yyyyMMddHHmmssfffff")}_{testStage}.txt");
                }
            }
            catch (Exception ex)
            {
                using (var writeLog = new WriteLog())
                {
                    writeLog.WriteLogFile($"SendFT_SFCDATA.cs Flag-2: {ex.Message}");
                }

                MessageBox.Show($"SendFT_SFCDATA.cs Flag-2: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
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
        ~SendFT_SFCDATA()
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
