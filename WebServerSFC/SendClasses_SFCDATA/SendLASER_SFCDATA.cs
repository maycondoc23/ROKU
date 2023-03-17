using SentinelaRoku.Entity;
using SentinelaRoku.ServiceReferenceTEST;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WebServerSFC;
using WebServerSFC.Classes;

namespace SentinelaRoku.SendClasses_SFCDATA
{
    class SendLASER_SFCDATA: IDisposable
    {
        private DataTable tableErrorCode;
        private DataTable tableStation;
        private string operatorID;
        private string productLine;
        private string hostName;
        private string groupName;

        /*************************************************************************************************************************/
        /*--- Construtor ---*/
        public SendLASER_SFCDATA(string id, string host, string group, DataTable stations, DataTable errorCodes)
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
        public RegMessageAnalysis messageAnalysis(string Receive)
        {
            RegMessageAnalysis regMessageAnalysis = new RegMessageAnalysis();

            using (var writeLog = new WriteLog())
            {
                writeLog.WriteLogFile($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} --> Mensagem recebidado do teste: {Receive}");
            }

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

                    if (HostNameTest.Contains("--"))
                    {
                        string hostNameOld = HostNameTest;

                        HostNameTest = HostNameTest.Replace("--", string.Empty);

                        string hostNameNew = HostNameTest;

                        using (var writeLog = new WriteLog())
                        {
                            writeLog.WriteLogFile($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} --> Replace HostName: {hostNameOld}(Old) to {hostNameNew}(New)");
                        }
                    }

                    DataRow[] consultHostName = tableStation.Select($"Test_Station LIKE '%{HostNameTest}%'");

                    if (consultHostName.Length > 0)
                    {
                        string Hostname = consultHostName[0]["SFC_HostName"].ToString();
                        string GroupName = consultHostName[0]["SFC_Station"].ToString();

                        if (Hostname == hostName)
                        {
                            string PN = string.Empty;
                            string CSN = string.Empty;
                            string CESN = string.Empty;

                            /*--- Consulta no WebService ---*/
                            var webservice = new WebServiceMethods();  

                            var resultCheckStatus = webservice.SFIS_CHECK_STATUS(SN, GroupName);

                            string statusMessage = resultCheckStatus.ErrorMessage;

                            /*-----------------------------------------------------------------------------------------------------------------------*/


                            /*--- Responde para o teste ---*/
                            if (resultCheckStatus.StatusCode == "0")
                            {
                                var resultGetData = webservice.SFIS_GET_DATA(SN);

                                using (var writeLog = new WriteLog())
                                {
                                    writeLog.WriteLogFile($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} --> Passo 1 - Enviado Para WebService SFIS_GET_DATA: SN: {SN}");
                                    writeLog.WriteLogFile($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} --> Passo 1 - Enviado Para WebService SFIS_GET_DATA: StatusCode: {resultGetData.StatusCode}, ErrorMessage: {resultGetData}");
                                }

                                string GetDataErrorMessage = resultGetData.ErrorMessage;

                                PN = resultGetData.Configuration.Sku;
                                DeviceDetail[] details = resultGetData.Configuration.DeviceDetails;

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

                                if (resultGetData.StatusCode == "0") //check OK
                                {
                                    regMessageAnalysis.resultTest = true;

                                    regMessageAnalysis.testAnswer = $"1>>SERIALNO={SN},CSN={CSN},CESN={CESN},PNNAME={PN}#OK,UNIT STATUS IS VALID";
                                }
                                else if (resultGetData.StatusCode == "1")   //check not OK
                                {
                                    regMessageAnalysis.resultTest = false;

                                    regMessageAnalysis.testAnswer = $"1>>SERIALNO={SN},CSN={CSN},CESN={CESN},PNNAME={PN}#{GetDataErrorMessage}";
                                }
                            }
                            else
                            {
                                regMessageAnalysis.resultTest = false;

                                regMessageAnalysis.testAnswer = $"1>>SERIALNO={SN},CSN={CSN},CESN={CESN},PNNAME={PN}#{statusMessage}";

                                MessageBox.Show($"StatusCode: {resultCheckStatus.StatusCode} {Environment.NewLine} Message: {resultCheckStatus.ErrorMessage}", "Route Verification", MessageBoxButton.OK, MessageBoxImage.Exclamation);

                            }


                            //testAnswer = $"1>>SERIALNO={SN},CSN={CSN},CESN={CESN},PNNAME={PN}#OK,UNIT STATUS IS VALID";

                            SendMessageToTest(regMessageAnalysis.testAnswer, "start");

                            /*-----------------------------------------------------------------------------------------------------------------------*/
                        }
                        else
                        {
                            regMessageAnalysis.testAnswer = $"1>>SERIALNO={SN},PNNAME=#Wrong hostname!";

                            SendMessageToTest($"1>>SERIALNO={SN},PNNAME=#Wrong hostname!", "start");
                            MessageBox.Show("Wrong hostname received!" + Environment.NewLine + $"Selected hostname: {hostName}" + Environment.NewLine + $"Received hostname: {Hostname}", "Alert", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    else
                    {
                        regMessageAnalysis.testAnswer = $"1>>SERIALNO={SN},PNNAME=#Wrong hostname!";

                        SendMessageToTest($"1>>SERIALNO={SN},PNNAME=#Wrong hostname!", "start");
                        MessageBox.Show("Wrong hostname received!" + Environment.NewLine + $"Selected hostname: {HostNameTest}" + Environment.NewLine + $"Received hostname: {HostNameTest}", "Alert", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                        regMessageAnalysis.testAnswer = $"2>>SERIALNO={SN}#OK,UNIT PASS!";

                        SendMessageToTest(regMessageAnalysis.testAnswer, "end"); //devemos devolver a resposta para o teste no step 2 o mais rapido possível, pode haver problema de timeout

                        /*-----------------------------------------------------------------------------------------------------------------------*/

                        var webservice = new WebServiceMethods();


                        /*--- Logout do SN ---*/

                        using (var writeLog = new WriteLog())
                        {
                            writeLog.WriteLogFile($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} --> Passo 2 - Enviado Para WebService SFIS_LOGOUT: SN: {SN}, operatorID: {operatorID}, productLine: {productLine}, groupName: {groupName}, hostName: {hostName}");
                        }

                        var resultLogout = webservice.SFIS_LOGOUT(SN, operatorID, productLine, groupName, hostName, "0");

                        using (var writeLog = new WriteLog())
                        {
                            writeLog.WriteLogFile($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} --> Passo 2 - Recebido WebService SFIS_LOGOUT: StatusCode: {resultLogout.StatusCode}, ErrorMessage: {resultLogout.ErrorMessage}");
                        }

                        /*-----------------------------------------------------------------------------------------------------------------------*/
                    }
                    else // Test result Fail
                    {
                        /*--- Resposta para o teste ---*/
                        regMessageAnalysis.testAnswer = $"2>>SERIALNO={SN}#{ResultTest}";

                        SendMessageToTest(regMessageAnalysis.testAnswer, "end"); //devemos devolver a resposta para o teste no step 2 o mais rapido possível, pode haver problema de timeout

                        /*-----------------------------------------------------------------------------------------------------------------------*/

                        var webservice = new WebServiceMethods();

                        /*--- Logout do SN ---*/
                        DataRow[] consultErrorCode = tableErrorCode.Select($"Description LIKE '%{ResultTest}%'");

                        if (consultErrorCode.Length <= 0)
                        {
                            consultErrorCode = tableErrorCode.Select($"CODE LIKE '%{ResultTest}%'");
                        }

                        string ErrorCode = consultErrorCode[0]["CODE"].ToString();

                        using (var writeLog = new WriteLog())
                        {
                            writeLog.WriteLogFile($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} --> Passo 2 - Enviado Para WebService SFIS_LOGOUT: SN: {SN}, operatorID: {operatorID}, productLine: {productLine}, groupName: {groupName}, hostName: {hostName}, ErrorCode: {ErrorCode}");
                        }

                        var resultLogout = webservice.SFIS_LOGOUT(SN, operatorID, productLine, groupName, hostName, ErrorCode);

                        using (var writeLog = new WriteLog())
                        {
                            writeLog.WriteLogFile($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} --> Passo 2 - Recebido WebService SFIS_LOGOUT: StatusCode: {resultLogout.StatusCode}, ErrorMessage: {resultLogout.ErrorMessage}");
                        }

                        /*-----------------------------------------------------------------------------------------------------------------------*/
                    }

                }
            }
            catch (Exception ex)
            {
                using (var writeLog = new WriteLog())
                {
                    writeLog.WriteLogFile($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} --> SendLASER_SFCDATA.cs Flag-1: {ex.Message}");
                }
                MessageBox.Show($"SendLASER_SFCDATA.cs Flag-1: {ex.Message}", "SentinelaRoku ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
            /*-----------------------------------------------------------------------------------------------------------------------*/


            return regMessageAnalysis;
        }

        /*************************************************************************************************************************/


        /*************************************************************************************************************************/
        /*--- Escreve o arquivo de resposta para o teste na pasta C:\SFCDATA_IN ---*/
        public RegMessageAnalysis SendMessageToTest(string sendMessage, string testStage)
        {
            RegMessageAnalysis regMessageAnalysis = new RegMessageAnalysis();


            /*--- Constroi o arquivo de resposta e salva na pasta do teste ---*/
            try
            {
                //escreve uma string num arquivo, cria o arquivo se não existir

                System.IO.File.WriteAllText($@"{ConfigurationManager.AppSettings["SFCDATA_IN"]}\{DateTime.Now.ToString("yyyyMMddHHmmssfffff")}_{testStage}.txt", sendMessage);

                using (var writeLog = new WriteLog())
                {
                    writeLog.WriteLogFile($@"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} --> File sent for testing: {ConfigurationManager.AppSettings["SFCDATA_IN"]}\{DateTime.Now.ToString("yyyyMMddHHmmssfffff")}_{testStage}.txt; Contendo: {sendMessage}");
                }
            }
            catch (Exception ex)
            {
                using (var writeLog = new WriteLog())
                {
                    writeLog.WriteLogFile($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} --> SendLASER_SFCDATA.cs Flag-2: {ex.Message}");
                }

                MessageBox.Show($"SendLASER_SFCDATA.cs Flag-2: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }

            /*-----------------------------------------------------------------------------------------------------------------------*/


            return regMessageAnalysis;
        }

        /*************************************************************************************************************************/


        /*************************************************************************************************************************/
        /*--- Destructor ---*/
        ~SendLASER_SFCDATA()
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
