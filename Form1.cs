using Google.Apis.Drive.v2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

namespace launcher {
    [SuppressMessage("ReSharper", "LocalizableElement")]
    public partial class Form1 : Form {
        private ButtonMode _lButtonMode = ButtonMode.NONE;
        private ButtonMode _rButtonMode = ButtonMode.EXIT;
        private ButtonMode _mButtonMode = ButtonMode.NONE;
        private string _mainDirectory = "";
        private string _zipFilepath = "";
        private IList<Google.Apis.Drive.v2.Data.File> _lastFailedFiles;
        public const string applicationName = "IntransigentMS Launcher";
        public const string serviceAccountEmail = "[IntransigentMS Google Drive service account email]";
        public static string[] scopes = { DriveService.Scope.DriveReadonly };
        public static readonly List<string> monitoredFiles = new List<string> {
                                                            "Base.wz",  "Character.wz", "Effect.wz",
                                                            "Etc.wz",   "Item.wz",      "Map.wz",
                                                            "Mob.wz",   "Morph.wz",     "Npc.wz",
                                                            "Quest.wz", "Reactor.wz",   "Skill.wz",
                                                            "Sound.wz", "String.wz",    "TamingMob.wz",
                                                            "UI.wz",    "IntransigentMS.exe"};
        public static readonly List<string> monitoredPeripherals = new List<string> {
                                                            "Canvas.dll",          "GameGuard.des", "Gr2D_DX8.dll",
                                                            "ijl15.dll",           "l3codeca.acm",  "MapleStoryUS.ini",
                                                            "NameSpace.dll",       "npkcrypt.dll",  "npkcrypt.sys",
                                                            "npkcrypt.vxd",        "npkcusb.sys",   "npkpdb.dll",
                                                            "Patcher.exe",         "PCOM.dll",      "ResMan.dll",
                                                            "Setup.exe",           "Shape2D.dll",   "Sound_DX8.dll",
                                                            "WzFlashRenderer.dll", "WzMss.dll",     "ZLZ.dll",
                                                            "List.wz"};

        private enum ButtonMode : byte { NONE, PLAY, EXIT, INSTALL, UPDATE, EXTRACT, MD5, DOWNLOAD }

        public Form1() {
            InitializeComponent();
            progressBar1.Visible = false;
        }

        private void Form1_Load(object sender, EventArgs ea) {
            const int filesToDisplay = 6;

            var missingFiles = monitoredFiles.FindAll(f => !File.Exists(f));
            var missingPeripherals = monitoredPeripherals.FindAll(f => !File.Exists(f));

            if (missingFiles.Count > 0 || missingPeripherals.Count > 0) {
                var missingFilesMsg = "It looks like you're missing the following files:\r\n\r\n";
                var i = 0;
                for (; i < missingFiles.Count && i < filesToDisplay; ++i) {
                    missingFilesMsg += missingFiles[i] + (i % 2 == 0 ? "    " : "\r\n");
                }
                for (var j = 0; j < missingPeripherals.Count && j < filesToDisplay - i; ++j) {
                    missingFilesMsg += missingPeripherals[j] + ((j + i) % 2 == 0 ? "    " : "\r\n");
                }
                if (missingFiles.Count + missingPeripherals.Count > filesToDisplay) {
                    missingFilesMsg += "...and " + (missingFiles.Count + missingPeripherals.Count - filesToDisplay) + " more\r\n";
                }

                if (missingFiles.Count >= monitoredFiles.Count || missingPeripherals.Count > 0) {
                    if (!File.Exists("IntransigentMS.zip")) {
                        missingFilesMsg += "\r\nIf you've already installed IntransigentMS, ensure that this launcher"
                                         + " is in the folder you extracted to, along with all the other files.";
                        missingFilesMsg += " If you don't have IntransigentMS installed, just click the install button"
                                         + " below to begin installation automatically:";
                        label2.Text = missingFilesMsg;

                        _lButtonMode = ButtonMode.INSTALL;
                        _rButtonMode = ButtonMode.EXIT;
                        button1.Text = "Install";
                        button2.Text = "Exit";
                    } else {
                        missingFilesMsg += "\r\nBut it looks like you've already got the compressed game files.";
                        missingFilesMsg += " Would you like to extract them here to install the game, or download";
                        missingFilesMsg += " and install from scratch?";
                        _mainDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                        label2.Text = missingFilesMsg;

                        _lButtonMode = ButtonMode.EXTRACT;
                        _rButtonMode = ButtonMode.EXIT;
                        _mButtonMode = ButtonMode.INSTALL;
                        button1.Text = "Extract";
                        button2.Text = "Exit";
                        button3.Text = "Install";
                        button3.Visible = true;
                    }
                } else {
                    // They are only missing some files
                    using (var service = DriveUtil.authenticateServiceAccount(serviceAccountEmail)) {
                        if (service == null) {
                            label2.Text = "Oops!\r\nThere was an error authenticating the connection to Google Drive!"
                                        + "\r\n\r\nCheck the log file (IntransigentMSLauncherLog.txt) for details.";

                            _lButtonMode = ButtonMode.NONE;
                            _rButtonMode = ButtonMode.EXIT;
                            button1.Text = "";
                            button2.Text = "Exit";
                            return;
                        }

                        var listRequest = service.Files.List();
                        listRequest.MaxResults = 35;

                        // List Google Drive files
                        var files = listRequest.Execute().Items;

                        _lastFailedFiles = files.Where(f => missingFiles.Contains(f.Title)).ToList();
                    }

                    missingFilesMsg += "\r\nWould you like to download the missing files, or install from scratch?";
                    label2.Text = missingFilesMsg;

                    _lButtonMode = ButtonMode.UPDATE;
                    _rButtonMode = ButtonMode.EXIT;
                    _mButtonMode = ButtonMode.INSTALL;
                    button1.Text = "Update";
                    button2.Text = "Exit";
                    button3.Text = "Install";
                    button3.Visible = true;
                }
            } else {
                // Not missing any files
                if (UpdateInfo.isUpdated()) {
                    label2.Text = "Looks like everything is up to date!\r\n\r\nHave fun, and remember to not die!";

                    _lButtonMode = ButtonMode.PLAY;
                    _rButtonMode = ButtonMode.EXIT;
                    _mButtonMode = ButtonMode.MD5;
                    button1.Text = "Play!";
                    button2.Text = "Exit";
                    button3.Text = "Verify";
                    button3.Visible = true;
                } else {
                    label2.Text = "It looks like there's a new update you can grab.\r\n\r\nYou can download the new file(s),"
                                + " or just play anyways (NOT recommended, you may experience lots of crashes).";

                    _lButtonMode = ButtonMode.MD5;
                    _rButtonMode = ButtonMode.EXIT;
                    _mButtonMode = ButtonMode.PLAY;
                    button1.Text = "Update";
                    button2.Text = "Exit";
                    button3.Text = "Play";
                    button3.Visible = true;
                }
            }
        }

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImport("user32.dll")]
        internal static extern long SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        internal static extern bool ReleaseCapture();

        private void Form1_MouseDown(object sender, MouseEventArgs mea) {
            if (mea.Button == MouseButtons.Left) {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private async void button1_Click(object sender, EventArgs ea) {
            if (_lButtonMode == ButtonMode.INSTALL) {
                button1.Enabled = false;
                button3.Visible = false;
                // Prompt for user to select folder they want to install to
                var result = folderBrowserDialog1.ShowDialog();
                if (result == DialogResult.OK) {
                    _mainDirectory = folderBrowserDialog1.SelectedPath;

                    // Create Drive API service
                    using (var service = DriveUtil.authenticateServiceAccount(serviceAccountEmail)) {
                        if (service == null) {
                            label2.Text = "Oops!\r\nThere was an error authenticating the download!"
                                        + "\r\n\r\nCheck the log file (IntransigentMSLauncherLog.txt) for details.";

                            _lButtonMode = ButtonMode.NONE;
                            _rButtonMode = ButtonMode.EXIT;
                            _mButtonMode = ButtonMode.NONE;
                            button1.Text = "";
                            button2.Text = "Exit";
                            button3.Visible = false;
                            return;
                        }

                        // Define parameters of request
                        var listRequest = service.Files.List();
                        listRequest.MaxResults = 35;

                        // List files
                        var files = listRequest.Execute().Items;
                        if (files != null && files.Count > 0) {
                            // Find the file we need to download, by name
                            var setupFile = files.FirstOrDefault(file => file.Title == "IntransigentMS.zip");

                            if (setupFile == null) {
                                // Couldn't find a file with that name in the Drive
                                label2.Text = "Oops!\r\n\r\nError: No files with the name \"IntransigentMS.zip\" found for download!";
                                new LogWriter("Error: No files with the name \"IntransigentMS.zip\""
                                            + " found in Google Drive: (setupFile == null)");
                                _lButtonMode = ButtonMode.NONE;
                                _rButtonMode = ButtonMode.EXIT;
                                button1.Text = "";
                                button2.Text = "Exit";
                                button1.Enabled = true;
                            } else {
                                // Put up waiting screen with animated ellipsis and random text blurbs
                                bool success;
                                using (var timer = waitingScreen("Downloading IntransigentMS.zip", label2)) {
                                    // Start actually downloading file
                                    _zipFilepath = _mainDirectory + "\\IntransigentMS.zip";
                                    success = await DriveUtil.streamDownloadFile(service, setupFile, _zipFilepath, progressBar1);
                                }
                                if (success) {
                                    // New waiting screen for .zip extraction
                                    using (var timer = waitingScreen("IntransigentMS.zip downloaded successfully!"
                                                                   + "  Extracting game files", label2)) {
                                        // Actual .zip extraction (may have to be async'd)
                                        try {
                                            foreach (var filename in monitoredFiles) {
                                                if (File.Exists(_mainDirectory + "\\" + filename)) {
                                                    File.Delete(_mainDirectory + "\\" + filename);
                                                }
                                            }
                                            foreach (var filename in monitoredPeripherals) {
                                                if (File.Exists(_mainDirectory + "\\" + filename)) {
                                                    File.Delete(_mainDirectory + "\\" + filename);
                                                }
                                            }
                                            await Task.Run(() => ZipFile.ExtractToDirectory(_zipFilepath, _mainDirectory));
                                        } catch (Exception e) {
                                            timer.Enabled = false;
                                            label2.Text = "Oh, no... The extraction failed!\r\n\r\n"
                                                        + "Check the log file (IntransigentMSLauncherLog.txt) for details.";
                                            new LogWriter(e.ToString());

                                            _lButtonMode = ButtonMode.EXTRACT;
                                            _rButtonMode = ButtonMode.EXIT;
                                            button1.Text = "Retry";
                                            button2.Text = "Exit";
                                            button1.Enabled = true;
                                            return;
                                        }
                                    }

                                    File.Delete(_zipFilepath);

                                    await verifyIntegrity(files);
                                } else {
                                    // Download failed for some reason
                                    label2.Text = "Uh oh! The download failed!\r\n\r\n"
                                                + "Check the log file (IntransigentMSLauncherLog.txt) for details.";
                                    _lButtonMode = ButtonMode.DOWNLOAD;
                                    _rButtonMode = ButtonMode.EXIT;
                                    button1.Text = "Retry";
                                    button2.Text = "Exit";
                                    button1.Enabled = true;
                                }
                            }
                        } else {
                            label2.Text = "Oops!\r\n\r\nError: No files found!";
                            new LogWriter("Error: No files found in Google Drive while searching for"
                                        + " IntransigentMS.zip: (files == null || files.Count <= 0)");
                            _lButtonMode = ButtonMode.NONE;
                            _rButtonMode = ButtonMode.EXIT;
                            button1.Text = "";
                            button2.Text = "Exit";
                            button1.Enabled = true;
                        }
                    }
                } else {
                    button1.Enabled = true;
                }
            } else if (_lButtonMode == ButtonMode.EXTRACT) {
                button1.Enabled = false;
                button3.Visible = false;

                if (string.IsNullOrEmpty(_mainDirectory)) {
                    folderBrowserDialog1.Description = "Select the location of the IntransigentMS.zip file (IntransigentMS"
                                                     + " will be extracted to this location; no new folders will be created):";
                    var result = folderBrowserDialog1.ShowDialog();
                    if (result == DialogResult.OK) {
                        _mainDirectory = folderBrowserDialog1.SelectedPath;
                    } else {
                        button1.Enabled = true;
                        return;
                    }
                }
                if (string.IsNullOrEmpty(_zipFilepath)) {
                    _zipFilepath = _mainDirectory + "\\IntransigentMS.zip";
                }

                var zipFileInfo = new FileInfo(_zipFilepath);

                if (!zipFileInfo.Exists) {
                    label2.Text = "It doesn't look like the IntransigentMS.zip is in the selected folder.\r\n\r\n"
                                + "Would you like to install from scratch, or just locate the .zip?";
                    _mainDirectory = "";
                    _zipFilepath = "";

                    _lButtonMode = ButtonMode.INSTALL;
                    _rButtonMode = ButtonMode.EXIT;
                    _mButtonMode = ButtonMode.EXTRACT;
                    button1.Text = "Install";
                    button2.Text = "Exit";
                    button3.Text = "Locate .zip";
                    button1.Enabled = true;
                    button3.Visible = true;
                    return;
                }

                // Waiting screen for .zip extraction
                using (var timer = waitingScreen("Extracting game files", label2)) {
                    // Actual .zip extraction (may have to be async'd)
                    try {
                        foreach (var filename in monitoredFiles) {
                            if (File.Exists(_mainDirectory + "\\" + filename)) {
                                File.Delete(_mainDirectory + "\\" + filename);
                            }
                        }
                        foreach (var filename in monitoredPeripherals) {
                            if (File.Exists(_mainDirectory + "\\" + filename)) {
                                File.Delete(_mainDirectory + "\\" + filename);
                            }
                        }
                        await Task.Run(() => ZipFile.ExtractToDirectory(_zipFilepath, _mainDirectory));
                    } catch (Exception e) {
                        timer.Enabled = false;
                        label2.Text = "Oh, no... The extraction failed!\r\n\r\n"
                                    + "Check the log file (IntransigentMSLauncherLog.txt) for details.";
                        new LogWriter(e.ToString());

                        _lButtonMode = ButtonMode.EXTRACT;
                        _rButtonMode = ButtonMode.EXIT;
                        button1.Text = "Retry";
                        button2.Text = "Exit";
                        button1.Enabled = true;
                        return;
                    }
                }

                File.Delete(_zipFilepath);

                // Create Drive API service
                using (var service = DriveUtil.authenticateServiceAccount(serviceAccountEmail)) {
                    if (service == null) {
                        label2.Text = "Oops!\r\nThere was an error authenticating the connection to Google Drive!"
                                    + "\r\n\r\nCheck the log file (IntransigentMSLauncherLog.txt) for details.";
                        _lButtonMode = ButtonMode.NONE;
                        _rButtonMode = ButtonMode.EXIT;
                        _mButtonMode = ButtonMode.NONE;
                        button1.Text = "";
                        button2.Text = "Exit";
                        button3.Visible = false;
                        return;
                    }

                    // Define parameters of request
                    var listRequest = service.Files.List();
                    listRequest.MaxResults = 35;

                    // List files for use in verifying integrity
                    var files = listRequest.Execute().Items;

                    await verifyIntegrity(files);
                }
            } else if (_lButtonMode == ButtonMode.PLAY) {
                button1.Enabled = false;
                button3.Visible = false;
                using (var intransigentProcess = new Process()) {
                    try {
                        intransigentProcess.StartInfo.UseShellExecute = false;
                        if (!string.IsNullOrEmpty(_mainDirectory)) {
                            intransigentProcess.StartInfo.FileName = _mainDirectory + "\\IntransigentMS.exe";
                        } else {
                            intransigentProcess.StartInfo.FileName = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)
                                                                   + "\\IntransigentMS.exe";
                        }
                        intransigentProcess.Start();
                    } catch (Exception e) {
                        label2.Text = "Oh, boy...\r\nThere was an error launching the game.\r\n\r\n"
                                    + "Make sure you're running this launcher as an Administrator."
                                    + " (Right click and select \"Run As Administrator\").\r\n"
                                    + "Check the log file (IntransigentMSLauncherLog.txt) for details.";
                        new LogWriter(e.ToString());

                        _lButtonMode = ButtonMode.PLAY;
                        _rButtonMode = ButtonMode.EXIT;
                        button1.Text = "Retry";
                        button2.Text = "Exit";
                        button1.Enabled = true;
                        return;
                    }
                }
                Application.Exit();
            } else if (_lButtonMode == ButtonMode.UPDATE) {
                await updateFiles();
            } else if (_lButtonMode == ButtonMode.MD5) {
                using (var service = DriveUtil.authenticateServiceAccount(serviceAccountEmail)) {
                    if (service == null) {
                        label2.Text = "Oops!\r\nThere was an error authenticating the connection to Google Drive!"
                                    + "\r\n\r\nCheck the log file (IntransigentMSLauncherLog.txt) for details.";

                        _lButtonMode = ButtonMode.NONE;
                        _rButtonMode = ButtonMode.EXIT;
                        _mButtonMode = ButtonMode.NONE;
                        button1.Text = "";
                        button2.Text = "Exit";
                        button3.Visible = false;
                        return;
                    }

                    // Define parameters of request
                    var listRequest = service.Files.List();
                    listRequest.MaxResults = 35;

                    // List files for use in verifying integrity
                    var files = listRequest.Execute().Items;

                    await verifyIntegrity(files);
                }
            } else if (_lButtonMode == ButtonMode.DOWNLOAD) {
                if (string.IsNullOrEmpty(_mainDirectory)) {
                    _mainDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                }
                await downloadZip();
            } else if (_lButtonMode == ButtonMode.NONE) {
            } else {
                label2.Text = "Oh dear —\r\n\r\nError: Unrecognized left button mode.";
                new LogWriter("Error: Unrecognized lButtonMode");

                _lButtonMode = ButtonMode.NONE;
                _rButtonMode = ButtonMode.EXIT;
                _mButtonMode = ButtonMode.NONE;
                button1.Text = "";
                button2.Text = "Exit";
                button3.Visible = false;
            }
        }

        private void button2_Click(object sender, EventArgs e) {
            if (_rButtonMode == ButtonMode.EXIT) {
                Application.Exit();
            } else if (_rButtonMode == ButtonMode.NONE) {
            } else {
                label2.Text = "Oh dear —\r\n\r\nError: Unrecognized right button mode.";
                new LogWriter("Error: Unrecognized rButtonMode");

                _lButtonMode = ButtonMode.NONE;
                _rButtonMode = ButtonMode.EXIT;
                _mButtonMode = ButtonMode.NONE;
                button1.Text = "";
                button2.Text = "Exit";
                button3.Visible = false;
            }
        }

        private async void button3_Click(object sender, EventArgs ea) {
            if (_mButtonMode == ButtonMode.MD5) {
                using (var service = DriveUtil.authenticateServiceAccount(serviceAccountEmail)) {
                    if (service == null) {
                        label2.Text = "Oops!\r\nThere was an error authenticating the connection to Google Drive!"
                                    + "\r\n\r\nCheck the log file (IntransigentMSLauncherLog.txt) for details.";

                        _lButtonMode = ButtonMode.NONE;
                        _rButtonMode = ButtonMode.EXIT;
                        _mButtonMode = ButtonMode.NONE;
                        button1.Text = "";
                        button2.Text = "Exit";
                        button3.Visible = false;
                        return;
                    }

                    // Define parameters of request
                    var listRequest = service.Files.List();
                    listRequest.MaxResults = 35;

                    // List files for use in verifying integrity
                    var files = listRequest.Execute().Items;

                    await verifyIntegrity(files);
                }
            } else if (_mButtonMode == ButtonMode.INSTALL) {
                button1.Enabled = false;
                button3.Enabled = false;
                if (string.IsNullOrEmpty(_mainDirectory)) {
                    folderBrowserDialog1.Description = "Select where you would like to install IntransigentMS (no new folders will be created):";
                    var result = folderBrowserDialog1.ShowDialog();
                    if (result == DialogResult.OK) {
                        _mainDirectory = folderBrowserDialog1.SelectedPath;
                    } else {
                        button1.Enabled = true;
                        button3.Enabled = true;
                        return;
                    }
                }
                await downloadZip();
            } else if (_mButtonMode == ButtonMode.PLAY) {
                button1.Enabled = false;
                button3.Enabled = false;
                using (var intransigentProcess = new Process()) {
                    try {
                        intransigentProcess.StartInfo.UseShellExecute = false;
                        if (!string.IsNullOrEmpty(_mainDirectory)) {
                            intransigentProcess.StartInfo.FileName = _mainDirectory + "\\IntransigentMS.exe";
                        } else {
                            intransigentProcess.StartInfo.FileName = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)
                                                                   + "\\IntransigentMS.exe";
                        }
                        intransigentProcess.Start();
                    } catch (Exception e) {
                        label2.Text = "Oh, boy...\r\nThere was an error launching the game.\r\n\r\n"
                                    + "Make sure you're running this launcher as an Administrator."
                                    + " (Right click and select \"Run As Administrator\").\r\n"
                                    + "Check the log file (IntransigentMSLauncherLog.txt) for details.";
                        new LogWriter(e.ToString());

                        _lButtonMode = ButtonMode.PLAY;
                        _rButtonMode = ButtonMode.EXIT;
                        button1.Text = "Retry";
                        button2.Text = "Exit";
                        button1.Enabled = true;
                        button3.Visible = false;
                        return;
                    }
                }
                Application.Exit();
            } else if (_mButtonMode == ButtonMode.NONE) {
            } else {
                label2.Text = "Oh dear —\r\n\r\nError: Unrecognized middle button mode.";
                new LogWriter("Error: Unrecognized mButtonMode");

                _lButtonMode = ButtonMode.NONE;
                _rButtonMode = ButtonMode.EXIT;
                _mButtonMode = ButtonMode.NONE;
                button1.Text = "";
                button2.Text = "Exit";
                button3.Visible = false;
            }
        }

        /// <summary>
        /// Checks for file existence, byte size matching, and MD5 checksum.
        /// </summary>
        /// <param name="files">List of files listed on Google Drive</param>
        /// <returns>void</returns>
        private async Task verifyIntegrity(IEnumerable<Google.Apis.Drive.v2.Data.File> files) {
            button3.Visible = false;
            button1.Enabled = false;

            if (string.IsNullOrEmpty(_mainDirectory)) {
                _mainDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            }

            var failedFiles = new List<Google.Apis.Drive.v2.Data.File>();
            using (var timer = waitingScreen("Game files are successfully extracted!  Checking for file intergrity", label2)) {
                await Task.Run(() => {
                    foreach (var file in files) {
                        if (monitoredFiles.Contains(file.Title)) {
                            var fileInfo = new FileInfo(_mainDirectory + "\\" + file.Title);
                            if (!fileInfo.Exists) {
                                // The file just ain't there
                                failedFiles.Add(file);
                                new LogWriter("Could not locate file " + file.Title +
                                              " when checking for file integrity");
                                continue;
                            }
                            if (file.FileSize != fileInfo.Length) {
                                // File is wrong size
                                failedFiles.Add(file);
                                new LogWriter("File with the name " + file.Title +
                                              " found to be wrong size (in bytes) when checking"
                                              + "for file integrity:\nReference filesize: " + file.FileSize +
                                              "\nUser filesize: "
                                              + fileInfo.Length);
                                continue;
                            }
                            var referenceChecksum = file.Md5Checksum;
                            string userChecksum;
                            try {
                                using (var md5 = MD5.Create()) {
                                    using (var stream = File.OpenRead(_mainDirectory + "\\" + file.Title)) {
                                        userChecksum =
                                            BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
                                    }
                                }
                            } catch (Exception e) {
                                label2.Text = "Ah, shit. The file integrity check failed!\r\n\r\n"
                                              + "Check the log file (IntransigentMSLauncherLog.txt) for details.";
                                new LogWriter(e.ToString());

                                _lButtonMode = ButtonMode.PLAY;
                                _rButtonMode = ButtonMode.EXIT;
                                _mButtonMode = ButtonMode.MD5;
                                button1.Text = "Play anyways";
                                button2.Text = "Exit";
                                button3.Text = "Retry";
                                button3.Visible = true;
                                button1.Enabled = true;
                                return;
                            }
                            if (!referenceChecksum.Equals(userChecksum)) {
                                // MD5 check failed
                                failedFiles.Add(file);
                                new LogWriter("MD5 checksum comparison failed for " + file.Title + ":\n"
                                              + "referenceChecksum = " + referenceChecksum + "\nuserChecksum = " +
                                              userChecksum);
                            }
                        }
                    }
                });
            }

            _lastFailedFiles = failedFiles;
            if (failedFiles.Count > 0) { // At least one file failed the check
                label2.Text = "It looks like " + failedFiles.Count + " of your files " + (failedFiles.Count > 1 ? "are" : "is")
                            + " missing or need to be updated.\r\n\r\nWould you like to update now?";

                _lButtonMode = ButtonMode.UPDATE;
                _rButtonMode = ButtonMode.EXIT;
                button1.Text = "Update";
                button2.Text = "Exit";
                button1.Enabled = true;
            } else { // Good to go
                UpdateInfo.update();
                label2.Text = "Everything looks good to go!";

                _lButtonMode = ButtonMode.PLAY;
                _rButtonMode = ButtonMode.EXIT;
                button1.Text = "Play!";
                button2.Text = "Exit";
                button1.Enabled = true;
            }
        }

        private async Task downloadZip() {
            button1.Enabled = false;
            // Create Drive API service
            using (var service = DriveUtil.authenticateServiceAccount(serviceAccountEmail)) {
                if (service == null) {
                    label2.Text = "Oops!\r\nThere was an error authenticating the download!"
                                + "\r\n\r\nCheck the log file (IntransigentMSLauncherLog.txt) for details.";

                    _lButtonMode = ButtonMode.NONE;
                    _rButtonMode = ButtonMode.EXIT;
                    _mButtonMode = ButtonMode.NONE;
                    button1.Text = "";
                    button2.Text = "Exit";
                    button3.Visible = false;
                    button1.Enabled = true;
                    return;
                }

                // Define parameters of request
                var listRequest = service.Files.List();
                listRequest.MaxResults = 35;

                // List files
                var files = listRequest.Execute().Items;
                if (files != null && files.Count > 0) {
                    // Find the file we need to download, by name
                    var setupFile = files.FirstOrDefault(file => file.Title == "IntransigentMS.zip");

                    if (setupFile == null) {
                        // Couldn't find a file with that name in the Drive
                        label2.Text = "Oops!\r\n\r\nError: No files with the name \"IntransigentMS.zip\" found!";
                        new LogWriter("Error: No files with the name \"IntransigentMS.zip\""
                                    + " found in Google Drive: (setupFile == null)");
                    } else {
                        // Put up waiting screen with animated ellipsis and random text blurbs
                        bool success;
                        using (var timer = waitingScreen("Downloading IntransigentMS.zip", label2)) {
                            // Start actually downloading file
                            _zipFilepath = _mainDirectory + "\\IntransigentMS.zip";
                            success = await DriveUtil.streamDownloadFile(service, setupFile, _zipFilepath, progressBar1);
                        }
                        if (success) {
                            // New waiting screen for .zip extraction
                            using (var timer = waitingScreen("IntransigentMS.zip downloaded successfully!"
                                                           + "  Extracting game files", label2)) {
                                try {
                                    foreach (var filename in monitoredFiles) {
                                        if (File.Exists(_mainDirectory + "\\" + filename)) {
                                            File.Delete(_mainDirectory + "\\" + filename);
                                        }
                                    }
                                    foreach (var filename in monitoredPeripherals) {
                                        if (File.Exists(_mainDirectory + "\\" + filename)) {
                                            File.Delete(_mainDirectory + "\\" + filename);
                                        }
                                    }
                                    await Task.Run(() => ZipFile.ExtractToDirectory(_zipFilepath, _mainDirectory));
                                } catch (Exception e) {
                                    timer.Enabled = false;
                                    label2.Text = "Oh, no... The extraction failed!\r\n\r\n"
                                                + "Check the log file (IntransigentMSLauncherLog.txt) for details.";
                                    new LogWriter(e.ToString());

                                    _lButtonMode = ButtonMode.EXTRACT;
                                    _rButtonMode = ButtonMode.EXIT;
                                    button1.Text = "Retry";
                                    button2.Text = "Exit";
                                    button1.Enabled = true;
                                    return;
                                }
                            }

                            File.Delete(_zipFilepath);

                            await verifyIntegrity(files);
                        } else {
                            // Download failed for some reason
                            label2.Text = "Uh oh! The download failed!\r\n\r\n"
                                        + "Check the log file (IntransigentMSLauncherLog.txt) for details.";

                            _lButtonMode = ButtonMode.DOWNLOAD;
                            _rButtonMode = ButtonMode.EXIT;
                            button1.Text = "Retry";
                            button2.Text = "Exit";
                            button1.Enabled = true;
                        }
                    }
                } else {
                    label2.Text = "Oops!\r\n\r\nError: No files found!";
                    new LogWriter("Error: No files found in Google Drive while searching for"
                                + " IntransigentMS.zip: (files == null || files.Count <= 0)");

                    _lButtonMode = ButtonMode.NONE;
                    _rButtonMode = ButtonMode.EXIT;
                    button1.Text = "";
                    button2.Text = "Exit";
                    button1.Enabled = true;
                }
            }
        }

        private async Task updateFiles() {
            button1.Enabled = false;
            button3.Visible = false;

            if (string.IsNullOrEmpty(_mainDirectory)) {
                _mainDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            }
            using (var timer = waitingScreen("Updating files", label2)) {
                // Create Drive API service
                using (var service = DriveUtil.authenticateServiceAccount(serviceAccountEmail)) {
                    while (_lastFailedFiles.Count > 0) {
                        var file = _lastFailedFiles[_lastFailedFiles.Count - 1];
                        var success = await DriveUtil.streamDownloadFile(service, file, _mainDirectory + "\\" + file.Title, progressBar1);
                        if (!success) {
                            label2.Text = "Oh, bother. There was an error downloading the update.\r\n\r\n"
                                        + "Check the log file (IntransigentMSLauncherLog.txt) for details.";

                            _lButtonMode = ButtonMode.UPDATE;
                            _rButtonMode = ButtonMode.EXIT;
                            button1.Text = "Retry";
                            button2.Text = "Exit";
                            button1.Enabled = true;
                            return;
                        }
                        _lastFailedFiles.RemoveAt(_lastFailedFiles.Count - 1);
                    }
                }
                UpdateInfo.update();
            }
            label2.Text = "Alright, you're all updated!";

            _lButtonMode = ButtonMode.PLAY;
            _rButtonMode = ButtonMode.EXIT;
            button1.Text = "Play!";
            button2.Text = "Exit";
            button1.Enabled = true;
        }

        private static System.Timers.Timer waitingScreen(string waitMessage, Control label, long period=1000, int randomMsgFreq=14) {
            var timer = new System.Timers.Timer(period);

            Func<Action<object, ElapsedEventArgs>> timerCallbackClosure = () => {
                var counter = 0;
                var randomMsg = "";
                var randomMsgs = new[] {
                      "The More You Know: IntransigentMS is a permadeath server!"
                    + " Yes, when you die, you go back to level 1.\r\nBut on the bright side,"
                    + " you get to keep a lot of your stuff, including some of your EXP multiplier!"
                    + "\r\nPlus, what's the fun if there's no excitement and tension involved?",
                      "IntransigentMS might be a v62 server, but don't expect a nostalgia-fest."
                    + "\r\nIntransigentMS is heavily modified and rebalanced, with lots of custom"
                    + " gameplay mechanics, content, and items.",
                      "New to the server? While you're waiting, you can take a peek on our website"
                    + " (intransigentms.com) at the changes we've made,\r\nor go from there to our"
                    + " forum for some neat game guides, or just pop on our Discord to say hello"
                    + " and/or ask your questions.",
                      "Not new to the server? Well, welcome back!\r\nDon't die. Oh, and remember"
                    + " to vote, also. Since you're not logged in, you can do that right now —"
                    + " even if you haven't created a character yet.",
                      "For Your Health: Remember that all incoming damage to your character is"
                    + " scaled according to your level, until you hit 100!\r\nUse the @truedamage"
                    + " command in-game so you can view the real amount of damage you're taking"
                    + " at all times.",
                      "If you'd like to get permanently banned for no reason, please let Xin know."
                    + "\r\nThey would be more than happy to help you out with that.",
                      "We Are Real People: If you have any suggestions, whether it be for game mechanics,"
                    + " content, items, or just small changes, just ask an admin about it.\r\nWe take"
                    + " player suggestions very seriously, even when they can't be implemented or aren't"
                    + " feasible; the most part of this server has been built with the help and"
                    + " suggestions of the community.",
                      "Do you know how to program, *.wz edit, or just like writing stories or doing art?"
                    + " IntransigentMS is a community-driven server: our server's code is open-source,"
                    + " and anyone can be a contributor to any aspect of the game, whether it's"
                    + " artwork for a custom party quest, or just some NPC scripts/dialogue.",
                      "Like the server, or just like the idea? Toss some spare change our way, and we'll"
                    + " give you donor status — unlimited NX and unlimited free chairs for life.",
                      "The More You Know: IntransigentMS has a custom class! Battle Priest adds to the"
                    + " available paths for Clerics/Priests/Bishops, being a STR-based class that"
                    + " boasts high melee damage, devastating summons, and the incredible healing power"
                    + " and utility of a Bishop.",
                      "The More You Know: IntransigentMS is not your typical Maplestory server. For one,"
                    + " all classes have been heavily balanced (and in some cases revamped) in order to"
                    + " ensure that all classes are unique and worthwhile in their own ways.\r\nBut make"
                    + " no mistake: not all classes are equally difficult to play. Check out the forum"
                    + " for a class guide."};
                RandomUtil.shuffleArray(randomMsgs);

                return (source, eea) => {
                    label.Invoke(new Action(() => {
                        if (counter % randomMsgFreq == 0) {
                            randomMsg = randomMsgs[counter / randomMsgFreq % randomMsgs.Length];
                        }
                        var newText = waitMessage;
                        for (var i = 0; i < counter % 4; ++i) {
                            newText += ".";
                        }
                        newText += "\r\n\r\n" + randomMsg;
                        label.Text = newText;
                    }));
                    counter++;
                };
            };
            var timerCallback = timerCallbackClosure();

            timer.Elapsed += (source, eea) => {
                timerCallback(source, eea);
            };

            timer.Enabled = true;
            timerCallback(null, null);

            return timer;
        }
    }
}
