using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebServerSFC.Classes
{
    public class WriteLog : IDisposable
    {
        public void Dispose()
        {
        }

        public void WriteLogFile(string Info)
        {
            bool control = true;
            if (!System.IO.File.Exists($@"{ Directory.GetCurrentDirectory()}\log\WebServerSFC.log"))
            {
                control = false;
                Directory.CreateDirectory($@"{ Directory.GetCurrentDirectory()}\log");
            }
            string path = $@"{Directory.GetCurrentDirectory()}\log";
            if (!System.IO.File.Exists(path) && !control)
            {
                using (StreamWriter sw = System.IO.File.CreateText($@"{path}\WebServerSFC.log"))
                {
                    sw.WriteLine($@"{Info} >> {DateTime.Now}{Environment.NewLine}");
                }
            }
            else
            {
                System.IO.File.AppendAllText($@"{ Directory.GetCurrentDirectory()}\log\WebServerSFC.log", $@"{Info} >> {DateTime.Now} {Environment.NewLine}{Environment.NewLine}");
            }
        }

        public void WriteSerialList(string Info)
        {
            bool control = true;
            if (!System.IO.File.Exists($@"{ Directory.GetCurrentDirectory()}\log\SerialList.log"))
            {
                control = false;
                Directory.CreateDirectory($@"{ Directory.GetCurrentDirectory()}\log");
            }
            string path = $@"{Directory.GetCurrentDirectory()}\log";
            if (!System.IO.File.Exists(path) && !control)
            {
                using (StreamWriter sw = File.CreateText($@"{path}\SerialList.log"))
                {
                    sw.WriteLine($@"{Info}");
                }
            }
            else
            {
                System.IO.File.AppendAllText($@"{ Directory.GetCurrentDirectory()}\log\SerialList.log", $@"{Info}  {Environment.NewLine}");
            }
        }
    }
}
