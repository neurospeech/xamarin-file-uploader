using Android.App;
using Android.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace XamarinFileUploader
{


    [Service]
    public class BackgroundUploadService : IntentService
    {

        private static object lockObject = new object();

        public BackgroundUploadService()
        {
            this.SetIntentRedelivery(true);
        }

        private static bool isRunning = false;
       

        protected override void OnHandleIntent(Intent intent)
        {

            lock (lockObject) {
                if (isRunning)
                    return;
                isRunning = true;
            }


            while (true) {

                var pending = FileUploaderService.Instance.Storage.Get().Where(x => x.ResponseCode == 0).Take(4).ToList();
                if (!pending.Any())
                {
                    FileUploaderService.Instance.ReportPendingStatus().Wait();
                    break;
                }

                try {

                    this.ProcessAsync(pending)
                        .Wait();
                } catch (Exception ex) {
                    FileUploaderService.Instance.ReportFatalError(ex);
                }


            }


            lock (lockObject) {
                isRunning = false;
            }

        }

        private async Task ProcessAsync(IEnumerable<FileUploadRequest> pendings)
        {
            var client = FileUploaderService.Instance.Receiver.GetHttpClient();

            var tasks = pendings.Select(async pending =>
            {

                var msg = new HttpRequestMessage(new HttpMethod(pending.Method.ToUpper()), pending.Url);

                using (var input = System.IO.File.OpenRead(pending.FilePath))
                {
                    var body = new StreamContent(input);
                    body.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(pending.ContentType);

                    var fileInfo = new System.IO.FileInfo(pending.FilePath);

                    body.Headers.ContentLength = fileInfo.Length;

                    if (pending.Headers != null)
                    {
                        foreach (var h in pending.Headers)
                        {
                            body.Headers.TryAddWithoutValidation(h.Key, h.Value);
                        }
                    }

                    msg.Content = new ProgressableStreamContent(body, (n, t) =>
                    {
                        try
                        {
                            pending.TotalBytes = (int)t;
                            pending.TotalSent = (int)n;
                            FileUploaderService.Instance.ReportProgress(pending);
                        }
                        catch (Exception ex)
                        {
                            FileUploaderService.Instance.ReportFatalError(ex);
                        }
                    });

                    System.Diagnostics.Debug.WriteLine($"Starting upload for {pending.Identifier}");

                    using (var response = await client.SendAsync(msg, HttpCompletionOption.ResponseHeadersRead))
                    {
                        using (var s = await response.Content.ReadAsStreamAsync())
                        {

                            pending.ResponseContentType = response.Content.Headers.ContentType.ToString();

                            pending.ResponseCode = (int)response.StatusCode;

                            using (var outs = System.IO.File.OpenWrite(pending.ResponseFilePath))
                            {
                                await s.CopyToAsync(outs);
                            }

                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"Finished upload for {pending.Identifier}");
                }
            });

            await Task.WhenAll(tasks);

            await FileUploaderService.Instance.ReportPendingStatus();

        }

    }


}
