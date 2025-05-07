using SentinelaRoku.Entity;
using SentinelaRoku.ServiceReferenceTEST;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WebServerSFC;
using WebServerSFC.Classes;

namespace SentinelaRoku.SendClasses_SFCDATA
{
    class SendPT_SFCATABAYSIDE: IDisposable
    {
        private DataTable tableErrorCode;
        private DataTable tableStation;

        private string operatorID;
        private string productLine;
        private string hostName;
        private string groupName;


        /*************************************************************************************************************************/
        /*--- Construtor ---*/
        public SendPT_SFCATABAYSIDE(string id, string host, string group, DataTable stations, DataTable errorCodes)
        {
            operatorID = id;
            productLine = ConfigurationManager.AppSettings["PRODUCT_LINE"];
            hostName = host;
            groupName = group;
            tableStation = stations;
            tableErrorCode = errorCodes;
        }

        /*************************************************************************************************************************/

        public static string BtMacClass(string macHex, int increment = 1)
        {
            if (macHex.Length != 12)
                throw new ArgumentException("MAC deve conter 12 caracteres hexadecimais.");

            // Converte de string hexadecimal para ulong
            ulong macValue = ulong.Parse(macHex, NumberStyles.HexNumber);

            // Incrementa
            macValue += (ulong)increment;

            // Converte de volta para string hexadecimal com 12 caracteres
            return macValue.ToString("X12");
        }



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
                    string message = Receive.Substring(3).Trim();

                    string[] componetMessage = message.Split(',');

                    string SN = componetMessage[0];


                    //char ultimoDigito = SN[SN.Length - 1];
                    //int novoUltimoDigito = (int)Char.GetNumericValue(ultimoDigito) + 1;

                    // Substituir o último dígito no serial
                    //string BTMAC = SN.Substring(0, SN.Length - 1) + novoUltimoDigito;

                    string BTMAC = BtMacClass(SN, 1);

                    using (var writeLog = new WriteLog())
                    {
                        writeLog.WriteLogFile($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} --> BTMAC {BTMAC}");
                    }

                    string HostNameTest = componetMessage[1];
                    string GroupNameTest = componetMessage[2];

                    if (HostNameTest.Contains("--"))
                    {
                        string hostNameOld = HostNameTest;

                        HostNameTest = HostNameTest.Replace("--", string.Empty).Trim();

                        string hostNameNew = HostNameTest;

                        using (var writeLog = new WriteLog())
                        {
                            writeLog.WriteLogFile($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} --> Passo 1 - Replace HostName: {hostNameOld}(Old) to {hostNameNew}(New)");
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
                                    writeLog.WriteLogFile($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} --> EnviadoParaWebServiceSFIS_GET_DATA {SN}");
                                    writeLog.WriteLogFile($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} --> RecebidoWebServiceSFIS_GET_DATA : StatusCode: {resultGetData.StatusCode}, ErrorMessage: {resultGetData.ErrorMessage}");
                                }

                                string GetDataErrorMessage = resultGetData.ErrorMessage;

                                PN = resultGetData.Configuration.Sku;


                                if (resultGetData.StatusCode == "0") //check OK
                                {
                                    regMessageAnalysis.resultTest = true;

                                    regMessageAnalysis.testAnswer = $"1>>SERIALNO={SN},BT={BTMAC},PNNAME={PN}#OK,UNIT STATUS IS VALID";
                                }
                                else if (resultGetData.StatusCode == "1")   //check not OK
                                {

                                    regMessageAnalysis.resultTest = false;

                                    regMessageAnalysis.testAnswer = $"1>>SERIALNO={SN},BT={BTMAC},PNNAME={PN}#{GetDataErrorMessage}";
                                }
                            }
                            else
                            {
                                regMessageAnalysis.resultTest = false;

                                regMessageAnalysis.testAnswer = $"1>>SERIALNO={SN},BT={BTMAC},PNNAME={PN}#{statusMessage}";

                                MessageBox.Show($"StatusCode: {resultCheckStatus.StatusCode} {Environment.NewLine} Message: {resultCheckStatus.ErrorMessage}", "Route Verification", MessageBoxButton.OK, MessageBoxImage.Exclamation);

                            }

                            //testAnswer = $"1>>SERIALNO={SN},PNNAME={PN}#OK,UNIT STATUS IS VALID";

                            SendMessageToTest(regMessageAnalysis.testAnswer, "start");

                            /*-----------------------------------------------------------------------------------------------------------------------*/
                        }
                        else
                        {
                            regMessageAnalysis.testAnswer = $"1>>SERIALNO={SN},BT={BTMAC},PNNAME=#Wrong hostname!";

                            SendMessageToTest($"1>>SERIALNO={SN},BT={BTMAC},PNNAME=#Wrong hostname!", "start");

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
                    string message = Receive.Substring(3).Trim();

                    message = message.Replace(';', ',');
                    string[] componetMessage = message.Split(',');

                    string SN = componetMessage[0];
                    string BTMAC = componetMessage[1].Split('=')[1];
                    string FW = componetMessage[2].Split('=')[1];
                    string CSN = componetMessage[3].Split('=')[1];
                    string CESN = componetMessage[4].Split('=')[1];
                    string ResultTest = componetMessage[5].Split('#')[1];

                    using (var writeLog = new WriteLog())
                    {
                        writeLog.WriteLogFile($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} --> Passo 2 - Dados identificados: SN: {SN}; CSN: {CSN}; CESN: {CESN}; ResultTest: {ResultTest}");
                    }

                    if (ResultTest.Trim() == "PASS")
                    {
                        if ((CSN != null) && (CSN != string.Empty) && (CESN != null) && (CESN != string.Empty) && (FW != null) && (FW != string.Empty) && (BTMAC != null) && (BTMAC != string.Empty))
                        {
                            /*--- Resposta para o teste ---*/
                            regMessageAnalysis.testAnswer = $"2>>SERIALNO={SN}#OK,UNIT PASS!";

                            SendMessageToTest(regMessageAnalysis.testAnswer, "end"); //devemos devolver a resposta para o teste no step 2 o mais rapido possível, pode haver problema de timeout

                            /*-----------------------------------------------------------------------------------------------------------------------*/

                            var webservice = new WebServiceMethods();

                            /*--- Envio dos dados de CSN e CESN ---*/
                            List<DeviceDetail> listDetail = new List<DeviceDetail>();

                            listDetail.Add(new DeviceDetail { Key = "BluetoothMac", Value = BTMAC });
                            listDetail.Add(new DeviceDetail { Key = "CSN", Value = CSN });
                            listDetail.Add(new DeviceDetail { Key = "CESN", Value = CESN });
                            listDetail.Add(new DeviceDetail { Key = "FW", Value = FW });

                            using (var writeLog = new WriteLog())
                            {
                                writeLog.WriteLogFile($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} --> Passo 2 - Envio para SFIS_SEND_DATA : {new DeviceLog { MotherBoardSerialNumber = SN, Details = listDetail.ToArray() }}");
                            }

                            var send_CSN_CESN = webservice.SFIS_SEND_DATA(new DeviceLog { MotherBoardSerialNumber = SN, Details = listDetail.ToArray() });

                            if (send_CSN_CESN.StatusCode == "1")
                            {
                                using (var writeLog = new WriteLog())
                                {
                                    writeLog.WriteLogFile($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} --> Passo 2 - Falha no SFIS_SEND_DATA : {send_CSN_CESN.ErrorMessage}");
                                    MessageBox.Show($"StatusCode: {send_CSN_CESN.ErrorMessage}", "Route Verification", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                                }
                            }

                            else
                            { 
                                using (var writeLog = new WriteLog())
                                {
                                    writeLog.WriteLogFile($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} --> Passo 2 - Recebido WebService SEND_DATA - StatusCode: {send_CSN_CESN.StatusCode}, ErrorMessage: {send_CSN_CESN.ErrorMessage}");
                                }

                                /*-----------------------------------------------------------------------------------------------------------------------*/


                                /*--- Logout do SN ---*/
                                using (var writeLog = new WriteLog())
                                {
                                    writeLog.WriteLogFile($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} --> Passo 2 - Enviado WebService SFIS_LOGOUT - SN: {SN}, operatorID: {operatorID}, productLine: {productLine}, groupName: {groupName}, hostName: {hostName}");
                                }

                                var resultLogout = webservice.SFIS_LOGOUT(SN, operatorID, productLine, groupName, hostName, "0");

                                using (var writeLog = new WriteLog())
                                {
                                    writeLog.WriteLogFile($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} --> Passo 2 - Recebido WebService SFIS_LOGOUT - StatusCode: {resultLogout.StatusCode}, ErrorMessage: {resultLogout.ErrorMessage}");
                                }
                            }

                            /*-----------------------------------------------------------------------------------------------------------------------*/
                        }
                        else
                        {
                            /*--- Resposta para o teste ---*/
                            regMessageAnalysis.testAnswer = $"2>>SERIALNO={SN}#Empty CSN, CESN, BTMac or FW";

                            SendMessageToTest(regMessageAnalysis.testAnswer, "end"); //devemos devolver a resposta para o teste no step 2 o mais rapido possível, pode haver problema de timeout

                            /*-----------------------------------------------------------------------------------------------------------------------*/
                        }

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
                            writeLog.WriteLogFile($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} --> Passo 2 - Enviado Para WebService SFIS_LOGOUT - SN: {SN}, operatorID: {operatorID}, productLine: {productLine}, groupName: {groupName}, hostName: {hostName}, ErrorCode: {ErrorCode}");
                        }

                        var resultLogout = webservice.SFIS_LOGOUT(SN, operatorID, productLine, groupName, hostName, ErrorCode);

                        using (var writeLog = new WriteLog())
                        {
                            writeLog.WriteLogFile($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} --> Passo 2 - Recebido WebService SFIS_LOGOUT - StatusCode: {resultLogout.StatusCode}, ErrorMessage: {resultLogout.ErrorMessage}");
                        }

                        /*-----------------------------------------------------------------------------------------------------------------------*/
                    }
                }
            }
            catch (Exception ex)
            {
                using (var writeLog = new WriteLog())
                {
                    writeLog.WriteLogFile($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} --> SendPT_SFCATABAYSIDE.cs Flag-1: {ex.Message}");
                }
                MessageBox.Show($"SendPT_SFCATABAYSIDE.cs Flag-1: {ex.Message}", "SentinelaRoku ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    writeLog.WriteLogFile($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} --> SendPT_SFCATABAYSIDE.cs Flag-2: {ex.Message}");
                }

                MessageBox.Show($"SendPT_SFCATABAYSIDE.cs Flag-2: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }

            /*-----------------------------------------------------------------------------------------------------------------------*/



            return regMessageAnalysis;
        }

        /*************************************************************************************************************************/


        /*************************************************************************************************************************/
        /*--- Destructor ---*/
        ~SendPT_SFCATABAYSIDE()
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
