using System;
using System.Drawing;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Windows.Forms;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;
using Google.Apis.Drive.v2;
using Google.Apis.Services;

namespace launcher {
    public static class DriveUtil {
        public static readonly byte[] keyData = {
            /* Dummy data */
        };

        public static DriveService authenticateServiceAccount(string serviceAccountEmail) {
            var certificate = new X509Certificate2(keyData, "notasecret", X509KeyStorageFlags.Exportable);

            try {
                var credential = new ServiceAccountCredential(
                    new ServiceAccountCredential.Initializer(serviceAccountEmail) {
                        Scopes = Form1.scopes
                    }.FromCertificate(certificate)
                );

                // Create the service
                var service = new DriveService(new BaseClientService.Initializer() {
                    HttpClientInitializer = credential,
                    ApplicationName = Form1.applicationName,
                });
                return service;
            } catch (Exception e) {
                new LogWriter(e.ToString());
                return null;
            }
        }

        public static async Task<bool> streamDownloadFile(DriveService service, Google.Apis.Drive.v2.Data.File fileResource, string saveTo, ProgressBar progressBar=null) {
            if (!string.IsNullOrEmpty(fileResource.DownloadUrl)) {
                try {
                    // Load up the request and a memory stream to download the file into
                    var request = service.Files.Get(fileResource.Id);
                    using (var stream = new MemoryStream()) {
                        var totalSize = fileResource.FileSize;

                        new LogWriter("Loading up the request and a memory stream to download the file into:\n"
                                      + "request: " + request.FileId + ", total size: " + totalSize);

                        // Add a handler that is notified upon the download progress changing;
                        // notifies on each chunk download and any time the download completes or fails
                        if (progressBar != null) {
                            request.MediaDownloader.ProgressChanged +=
                                progress => {
                                    switch(progress.Status) {
                                        case DownloadStatus.Downloading: {
                                            new LogWriter("DownloadStatus.Downloading");
                                            progressBar.Invoke(new Action(() => {
                                                var l = 100 * progress.BytesDownloaded / totalSize;
                                                if (l != null) {
                                                    progressBar.Value = (int) l;
                                                }
                                            }));
                                            break;
                                        }
                                        case DownloadStatus.Completed: {
                                            new LogWriter("DownloadStatus.Completed");
                                            progressBar.Invoke(new Action(() => {
                                                progressBar.Visible = false;
                                            }));
                                            break;
                                        }
                                        case DownloadStatus.Failed: {
                                            new LogWriter("DownloadStatus.Failed");
                                            progressBar.Invoke(new Action(() => {
                                                progressBar.BackColor = Color.Crimson;
                                                progressBar.ForeColor = Color.DarkRed;
                                            }));
                                            break;
                                        }
                                    }
                                };
                            // Show the progress bar
                            progressBar.BackColor = Color.FromArgb(72, 72, 72);
                            progressBar.ForeColor = Color.FromArgb(2, 2, 2);
                            progressBar.Visible = true;
                        }

                        // Download the file
                        new LogWriter("Downloading file");
                        var finalStatus = await request.DownloadAsync(stream);
                        new LogWriter("File downloading method returned");

                        // Check that our download was actually successful
                        if (finalStatus.Status == DownloadStatus.Failed) {
                            new LogWriter(finalStatus.Exception.ToString());
                            return false;
                        }

                        // Save memory stream to file
                        await Task.Run(() => {
                            using (var fileStream = new FileStream(saveTo, FileMode.Create)) {
                                new LogWriter("FileStream created");
                                stream.WriteTo(fileStream);
                                new LogWriter("Wrote stream to file; success");
                            }
                        });

                        if (progressBar != null) {
                            progressBar.Visible = false;
                        }

                        // Success
                        return true;
                    }
                } catch (Exception e) {
                    new LogWriter(e.ToString());
                    return false;
                }
            }
            // The file doesn't have any content stored on Drive
            new LogWriter("fileResource has no download URL: (string.IsNullOrEmpty(fileResource.DownloadUrl))");
            return false;
        }
    }
}
