using Android.App;
using Android.Content;
using Java.IO;
using Newtonsoft.Json;
using Square.OkHttp3;
using Square.OkIO;
using System;
using System.Collections.Generic;
using System.Linq;
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

                    using (Square.OkHttp3.OkHttpClient client = new Square.OkHttp3.OkHttpClient()) {

                        

                        var contentType = Square.OkHttp3.MediaType.Parse(pending.ContentType);
                        var body = // Square.OkHttp3.RequestBody.Create(contentType, new Java.IO.File(pending.FilePath));
                            new CountingFileRequestBody(new Java.IO.File(pending.FilePath), pending.ContentType, (s, t) => {
                                pending.TotalSent = s;
                                pending.TotalBytes = t;
                                XamarinFileUploader.FileUploaderService.Instance.ReportProgress(pending);
                            });

                        var headers = new Headers.Builder();
                        if (pending.Headers != null)
                        {
                            foreach (var h in pending.Headers)
                            {
                                headers.Add(h.Key, h.Value);
                            }
                        }

                        var request = new Square.OkHttp3.Request.Builder()
                            .Url(pending.Url)
                            .Post(body)
                            .Headers(headers.Build())
                            .Build();

                        var response = client.NewCall(request).Execute();

                        using (var fs = System.IO.File.OpenWrite(pending.ResponseFilePath))
                        {
                            var b = response.Body().Bytes();
                            fs.Write(b, 0, b.Length);
                        }

                        if (response.IsSuccessful)
                        {
                            pending.ResponseCode = 200;
                        }
                        else {
                            pending.ResponseCode = 500;
                        }

                        XamarinFileUploader.FileUploaderService.Instance.ReportStatus(pending);

                    }

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

    }

    public class CountingFileRequestBody : Square.OkHttp3.RequestBody
    {

        private static int SEGMENT_SIZE = 5120; // okio.Segment.SIZE

        private File file;
        private Action<int,int> progress;
        private String _contentType;

        public CountingFileRequestBody(File file, String contentType, Action<int,int> progress)
        {
            this.file = file;
            this._contentType = contentType;
            this.progress = progress;
        }

        public long contentLength()
        {
            return file.Length();
        }

        public override MediaType ContentType()
        {
            return MediaType.Parse(_contentType);
        }

        public override void WriteTo(IBufferedSink sink) 
        {
            
            using (var source = Square.OkIO.OkIO.Source(file))
            {
                long total = 0;
                long read;
                long totaltoRead = file.Length();

                while ((read = source.Read(sink.Buffer, SEGMENT_SIZE)) != -1)
                {
                    total += read;
                    sink.Flush();
                    this.progress?.Invoke((int)total, (int)totaltoRead);

                }
            }
        }


    }
}
