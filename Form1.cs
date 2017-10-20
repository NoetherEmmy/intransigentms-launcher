using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace launcher {
    public partial class Form1 : Form {
        private string lButtonMode;
        private string rButtonMode;
        public const string setupURI = "http://download854.mediafire.com/icfg1lxy76vg/2w9d2dc88eavb7s/1979+Semi-Finalist.zip";

        public Form1() {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e) {
            const int filesToDisplay = 6;
            List<string> monitoredFiles = new List<string> {"Base.wz",  "Character.wz", "Effect.wz",
                                                            "Etc.wz",   "Item.wz",      "Map.wz",
                                                            "Mob.wz",   "Morph.wz",     "Npc.wz",
                                                            "Quest.wz", "Reactor.wz",   "Skill.wz",
                                                            "Sound.wz", "String.wz",    "TamingMob.wz",
                                                            "UI.wz",    "IntransigentMS.exe"};
            List<string> missingFiles = new List<string>();
            monitoredFiles.ForEach(f => { if (!File.Exists(f)) missingFiles.Add(f); });
            if (missingFiles.Count > 0) {
                string missingFilesMsg = "It looks like you're missing the following files:\r\n\r\n";
                for (int i = 0; i < missingFiles.Count && i < filesToDisplay; ++i) {
                    missingFilesMsg += missingFiles[i] + (i % 2 == 0 ? "    " : "\r\n");
                }
                if (missingFiles.Count > filesToDisplay) {
                    missingFilesMsg += "...and " + (missingFiles.Count - filesToDisplay) + " more\r\n";
                }
                missingFilesMsg += "\r\nIf you've already installed IntransigentMS, ensure that this launcher is in the folder you extracted to, along with all the other files.";
                missingFilesMsg += " If you don't have IntransigentMS installed, just click the install button below to begin installation automatically:";
                label2.Text = missingFilesMsg;

                lButtonMode = "install";
                rButtonMode = "exit";
                button1.Text = "Install";
                button2.Text = "Exit";
            }
        }

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImportAttribute("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();

        private void Form1_MouseDown(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void button1_Click(object sender, EventArgs e) {
            if (lButtonMode == "install") {
                DialogResult result = folderBrowserDialog1.ShowDialog();

                if (result == DialogResult.OK) {
                    string folderName = folderBrowserDialog1.SelectedPath;
                    WebClient client = new WebClient();
                    client.DownloadFile(new Uri(setupURI), folderName + "IntransigentMS.zip");
                    ZipFile.ExtractToDirectory(folderName + "IntransigentMS.zip", folderName);
                }
            }
        }
    }
}
