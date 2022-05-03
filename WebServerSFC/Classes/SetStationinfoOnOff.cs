using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace WebServerSFC.Classes
{
    public class SetStationinfoOnOff : IDisposable
    {
        public void Dispose()
        {

        }

        public void SetOnOff(bool status)
        {
            try
            {
                string line = string.Empty;

                List<string> lines = new List<string>();

                if (File.Exists(@"C:\mtp\cfg_HNM\StationInfo.txt"))
                {
                    bool Control = false;

                    // Read the file and display it line by line.
                    StreamReader file = new System.IO.StreamReader(@"C:\mtp\cfg_HNM\StationInfo.txt");

                    while ((line = file.ReadLine()) != null)
                    {
                        if (line.Contains("<Prop Name='FlowDefines' Type='Obj'>"))
                        {
                            lines.Add(line);
                            Control = true;
                        }
                        else if (Control && line.Contains("Value"))
                        {
                            if (line.Contains("True") && status)
                            {
                                Control = false;
                                lines.Add(line);
                            }
                            else if (line.Contains("False") && status == true)
                            {
                                Control = false;
                                lines.Add(line.Replace("False", "True"));
                            }
                            else if (line.Contains("False") && status == false)
                            {
                                Control = false;
                                lines.Add(line);
                            }
                            else if (line.Contains("True") && status == false)
                            {
                                Control = false;
                                lines.Add(line.Replace("True", "False"));
                            }
                            else
                            {
                                lines.Add(line);
                            }
                        }
                        else
                            lines.Add(line);

                    }

                    file.Close();

                    TextWriter tw = new StreamWriter($@"C:\mtp\cfg_HNM\StationInfo.txt", false);

                    foreach (String s in lines)
                        tw.WriteLine(s);

                    tw.Close();
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Alert", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
