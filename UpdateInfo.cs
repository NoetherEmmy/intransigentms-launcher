using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace launcher {
    public static class UpdateInfo {
        private static string _exePath = "";

        public static bool update() {
            _exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            try {
                using (var w = File.CreateText(_exePath + "\\IntransigentMSUpdateInfo.txt")) {
                    try {
                        w.Write("{0}", DateTime.Now.ToUniversalTime().ToString(new DateTimeFormatInfo()));
                    } catch (Exception e) {
                        new LogWriter(e.ToString());
                        return false;
                    }
                }
            } catch (Exception e) {
                new LogWriter(e.ToString());
                return false;
            }

            return true;
        }

        public static bool isUpdated() {
            _exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            try {
                var fi = new FileInfo(_exePath + "\\IntransigentMSUpdateInfo.txt");
                if (!fi.Exists) {
                    new LogWriter("No IntransigentMSUpdateInfo.txt in exe path");
                    return false;
                }
                var lastUpdate = new DateTime();
                foreach (var line in File.ReadLines(_exePath + "\\IntransigentMSUpdateInfo.txt", Encoding.UTF8)) {
                    lastUpdate = DateTime.Parse(line, new DateTimeFormatInfo(), DateTimeStyles.AssumeUniversal);
                    break;
                }

                DateTime? latestRevisionDate = null;
                using (var service = DriveUtil.authenticateServiceAccount(Form1.serviceAccountEmail)) {
                    if (service == null) {
                        return false;
                    }

                    var listRequest = service.Files.List();
                    listRequest.MaxResults = 35;

                    var files = listRequest.Execute().Items;

                    foreach (var file in files) {
                        if (!Form1.monitoredFiles.Contains(file.Title)) continue;
                        var _latestRevisionDate = file.ModifiedDate;
                        if (!latestRevisionDate.HasValue ||
                            (_latestRevisionDate.HasValue &&
                             DateTime.Compare(_latestRevisionDate.Value, latestRevisionDate.Value) > 0)) {
                            latestRevisionDate = _latestRevisionDate;
                        }
                    }
                }
                if (!latestRevisionDate.HasValue) {
                    new LogWriter("!latestRevisionDate.HasValue");
                    return true;
                }
                if (lastUpdate.CompareTo(latestRevisionDate.Value.ToUniversalTime()) > 0) {
                    return true;
                }
            } catch (Exception e) {
                new LogWriter(e.ToString());
            }
            return false;
        }
    }
}
