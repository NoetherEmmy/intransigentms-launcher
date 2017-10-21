using System;
using System.IO;
using System.Reflection;

namespace launcher {
    public class LogWriter {
        private string _exePath = "";
        public LogWriter(string logMessage) {
            logWrite(logMessage);
        }

        private void logWrite(string logMessage) {
            _exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            try {
                using (var w = File.AppendText(_exePath + "\\IntransigentMSLauncherLog.txt")) {
                    log(logMessage, w);
                }
            } catch (Exception) {
            }
        }

        private static void log(string logMessage, TextWriter txtWriter) {
            try {
                txtWriter.Write("Log entry : ");
                txtWriter.Write("{0} {1}", DateTime.Now.ToLongTimeString(),
                                           DateTime.Now.ToLongDateString());
                txtWriter.WriteLine(" :");
                var splitMessage = logMessage.Split('\n', '\r');
                foreach (var s in splitMessage) {
                    if (!string.IsNullOrEmpty(s)) {
                        txtWriter.WriteLine("    {0}", s);
                    }
                }
                txtWriter.WriteLine("-------------------------------");
                txtWriter.WriteLine("");
            } catch (Exception) {
            }
        }
    }
}
