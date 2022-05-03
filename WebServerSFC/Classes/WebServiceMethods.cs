using SentinelaRoku.ServiceReferenceTEST;
using System;
using System.Configuration;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace WebServerSFC

{
    class WebServiceMethods : IDisposable
    {
        HttpRequestMessageProperty customerHeader = new HttpRequestMessageProperty();
        WebServiceTestSoapClient client = new WebServiceTestSoapClient("WebServiceTestSoap");

        public WebServiceMethods()
        {
            customerHeader.Headers.Add("X-Type", ConfigurationManager.AppSettings["Type"].ToString());
            customerHeader.Headers.Add("X-Customer", ConfigurationManager.AppSettings["Customer"].ToString());
        }

        public ReturnDefault SFIS_CHECK_STATUS(string motherBoardSerialNumber, string stationGroup)
        {
            using (new OperationContextScope(client.InnerChannel))
            {
                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = customerHeader;

                return client.SFIS_CHECK_STATUS(motherBoardSerialNumber, stationGroup);
            }
        }

        public ReturnData SFIS_GET_DATA(string motherBoardSerialNumber)
        {
            using (new OperationContextScope(client.InnerChannel))
            {
                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = customerHeader;

                return client.SFIS_GET_DATA(motherBoardSerialNumber);
            }
        }

        public ReturnDefault SFIS_LOGIN(string operatorId, string password, string productionLine, string stationGroup, string hostname)
        {
            using (new OperationContextScope(client.InnerChannel))
            {
                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = customerHeader;

                return client.SFIS_LOGIN(operatorId, password, productionLine, stationGroup, hostname);
            }
        }

        public ReturnDefault SFIS_LOGOUT(string motherBoardSerialNumber, string operatorId, string productionLine, string stationGroup, string hostname, string statusCode)
        {
            using (new OperationContextScope(client.InnerChannel))
            {
                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = customerHeader;

                return client.SFIS_LOGOUT(motherBoardSerialNumber, operatorId, productionLine, stationGroup, hostname, statusCode);
            }
        }


        public ReturnDefault SFIS_SEND_DATA(DeviceLog data)
        {
            using (new OperationContextScope(client.InnerChannel))
            {
                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = customerHeader;

                return client.SFIS_SEND_DATA(data);
            }
        }


        public void Dispose()
        {

        }
    }
}
