using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SentinelaRoku.Entity
{
    public class RegMessageAnalysis
    {
        public bool resultTest { get; set; }
        public string testAnswer { get; set; }

        public string EnviadoParaWebServiceSEND_DATA { get; set; }
        public string EnviadoParaWebServiceDeviceDetailSEND_DATA { get; set; }
        public string RecebidoWebServiceSEND_DATA { get; set; }

        public string EnviadoParaWebServiceSFIS_LOGOUT { get; set; }
        public string RecebidoWebServiceSFIS_LOGOUT { get; set; }

        public string EnviadoParaWebServiceSFIS_GET_DATA { get; set; }
        public string RecebidoWebServiceSFIS_GET_DATA { get; set; }
    }
}
