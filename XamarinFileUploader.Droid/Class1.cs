using Android.App;
using Android.Content;
using Java.IO;
using Newtonsoft.Json;
using Square.OkHttp3;
using Square.OkIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace XamarinFileUploader
{
    public partial class FileUploaderService
    {

        private void OnStarted() {
            Intent intent = new Intent(Context, typeof(BackgroundUploadService));
            Context.StartService(intent);
        }

        protected virtual string ReadPreferences()
        {
            return Context
                .GetSharedPreferences("files", Android.Content.FileCreationMode.Private)
                .GetString("files", null);
        }

        protected virtual void WritePreferences(string content)
        {
            Context
                .GetSharedPreferences("files", Android.Content.FileCreationMode.Private)
                .Edit()
                .PutString("files", content)
                .Commit();
        }

        public Context Context =>
            Xamarin.Forms.Forms.Context;

        private void StartUploadInternal(FileUploadRequest request)
        {
            
            Intent intent = new Intent(Context, typeof(BackgroundUploadService));
            Context.StartService(intent);
        }


    }


    [Service]
    public class BackgroundUploadService : IntentService
    {
        public BackgroundUploadService()
        {
            this.SetIntentRedelivery(true);
        }

        private bool isRunning = false;
       

        protected override void OnHandleIntent(Intent intent)
        {

            lock (this) {
                if (isRunning)
                    return;
                isRunning = true;
            }


            while (true) {
                
                var pending = FileUploaderService.Instance.Requests.FirstOrDefault(x => x.ResponseCode == 0);
                if (pending == null)
                    break;

                try {

                    this.ProcessAsync(pending)
                        .Wait();

                    //using (Square.OkHttp3.OkHttpClient client = new Square.OkHttp3.OkHttpClient()) {

                        

                    //    var contentType = Square.OkHttp3.MediaType.Parse(pending.ContentType);
                    //    var body = // Square.OkHttp3.RequestBody.Create(contentType, new Java.IO.File(pending.FilePath));
                    //        new CountingFileRequestBody(new Java.IO.File(pending.FilePath), pending.ContentType, (s, t) => {
                    //            pending.TotalSent = s;
                    //            pending.TotalBytes = t;
                    //            XamarinFileUploader.FileUploaderService.Instance.ReportProgress(pending);
                    //        });

                    //    var headers = new Headers.Builder();
                    //    if (pending.Headers != null)
                    //    {
                    //        foreach (var h in pending.Headers)
                    //        {
                    //            headers.Add(h.Key, h.Value);
                    //        }
                    //    }

                    //    var request = new Square.OkHttp3.Request.Builder()
                    //        .Url(pending.Url)
                    //        .Post(body)
                    //        .Headers(headers.Build())
                    //        .Build();

                    //    var response = client.NewCall(request).Execute();

                    //    using (var fs = System.IO.File.OpenWrite(pending.ResponseFilePath))
                    //    {
                    //        var b = response.Body().Bytes();
                    //        fs.Write(b, 0, b.Length);
                    //    }

                    //    if (response.IsSuccessful)
                    //    {
                    //        pending.ResponseCode = 200;
                    //    }
                    //    else {
                    //        pending.ResponseCode = 500;
                    //    }

                    //    XamarinFileUploader.FileUploaderService.Instance.ReportStatus(pending);

                    //}

                } catch (Exception ex) {
                    FileUploaderService.Instance.ReportFatalError(ex);
                }



                Java.Lang.Thread.Sleep(1000);

                // first fire all unprocessed events...
                foreach (var r in FileUploaderService.Instance.Requests.Where(x => x.ResponseCode != 0 && x.Processed == false))
                {
                    FileUploaderService.Instance.ReportStatus(r);
                }

            }


            lock (this) {
                isRunning = false;
            }

        }

        private async Task ProcessAsync(FileUploadRequest pending)
        {
            var client = FileUploaderService.Instance.Receiver.GetHttpClient();

            var msg = new HttpRequestMessage(new HttpMethod(pending.Method.ToUpper()), pending.Url);

            using (var input = System.IO.File.OpenRead(pending.FilePath)) {
                var body = new StreamContent(input);
                body.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(pending.ContentType);

                if (pending.Headers != null) {
                    foreach (var h in pending.Headers)
                    {
                        body.Headers.TryAddWithoutValidation(h.Key, h.Value);
                    }
                }

                msg.Content = new ProgressableStreamContent(body, (n,t) => {
                    try {
                        pending.TotalBytes = (int)t;
                        pending.TotalSent = (int)n;
                        FileUploaderService.Instance.ReportProgress(pending);
                    } catch (Exception ex) {
                        FileUploaderService.Instance.ReportFatalError(ex);
                    }
                });

                using (var response = await client.SendAsync(msg, HttpCompletionOption.ResponseHeadersRead)) {
                    using (var s = await response.Content.ReadAsStreamAsync()) {

                        pending.ResponseContentType = response.Content.Headers.ContentType.ToString();

                        pending.ResponseCode = (int)response.StatusCode;

                        using (var outs = System.IO.File.OpenWrite(pending.ResponseFilePath)) {
                            await s.CopyToAsync(outs);
                        }

                    }
                }


            }

        }

    }

    internal class ProgressableStreamContent : HttpContent
    {

        /// <summary>
        /// Lets keep buffer of 20kb
        /// </summary>
        private const int defaultBufferSize = 5 * 4096;

        private HttpContent content;
        private int bufferSize;
        //private bool contentConsumed;
        private Action<long, long> progress;

        public ProgressableStreamContent(HttpContent content, Action<long, long> progress) : this(content, defaultBufferSize, progress) { }

        public ProgressableStreamContent(HttpContent content, int bufferSize, Action<long, long> progress)
        {
            if (content == null)
            {
                throw new ArgumentNullException("content");
            }
            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException("bufferSize");
            }

            this.content = content;
            this.bufferSize = bufferSize;
            this.progress = progress;

            foreach (var h in content.Headers)
            {
                this.Headers.Add(h.Key, h.Value);
            }
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {

            return Task.Run(async () =>
            {
                var buffer = new Byte[this.bufferSize];
                long size;
                TryComputeLength(out size);
                var uploaded = 0;


                using (var sinput = await content.ReadAsStreamAsync())
                {
                    while (true)
                    {
                        var length = sinput.Read(buffer, 0, buffer.Length);
                        if (length <= 0) break;

                        //downloader.Uploaded = uploaded += length;
                        uploaded += length;
                        progress?.Invoke(uploaded, size);

                        //System.Diagnostics.Debug.WriteLine($"Bytes sent {uploaded} of {size}");

                        stream.Write(buffer, 0, length);
                        stream.Flush();
                    }
                }
                stream.Flush();
            });
        }

        protected override bool TryComputeLength(out long length)
        {
            length = content.Headers.ContentLength.GetValueOrDefault();
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                content.Dispose();
            }
            base.Dispose(disposing);
        }

    }


}
