using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using XamarinFileUploader;

[assembly: Xamarin.Forms.Dependency(typeof(FileUploaderService))]

namespace XamarinFileUploader
{
    public partial class FileUploaderService
    {

        public FileUploaderService()
        {


            Xamarin.Forms.Device.BeginInvokeOnMainThread(async () => {
                try {

                    await Task.Delay(1000);
                    lock (this)
                    {
                        string existing = ReadPreferences();

                        List<FileUploadRequest> requests = Requests;

                        requests.Clear();

                        if (!string.IsNullOrWhiteSpace(existing))
                        {
                            requests.AddRange(JsonConvert.DeserializeObject<List<FileUploadRequest>>(existing));
                        }
                    }

                    OnStarted();


                }
                catch (Exception ex) {
                    System.Diagnostics.Debug.Fail(ex.Message, ex.ToString());
                }
            });
            
        }

        private static FileUploaderService _Instance = null;
        public static FileUploaderService Instance =>
            _Instance ?? (_Instance = Xamarin.Forms.DependencyService.Get<FileUploaderService>());


        public List<FileUploadRequest> Requests { get; }
            = new List<FileUploadRequest>();


        public async Task StartUpload(string tag, string url, string method, HttpContent content) {

            FileUploadRequest request = new FileUploadRequest() {
                Tag = tag,
                ContentType = content.Headers.ContentType.ToString(),
                FilePath = System.IO.Path.GetTempFileName(),
                Method = method,
                Url = url,
                ResponseFilePath = System.IO.Path.GetTempFileName()
            };

            List<Header> headerlist = new List<Header>();
            foreach (var header in content.Headers) {
                if (string.Equals(header.Key, "content-type", StringComparison.OrdinalIgnoreCase))
                    continue;
                headerlist.Add(new Header(header.Key, string.Join(" ", header.Value)));
            }

            using (var fs = System.IO.File.OpenWrite(request.FilePath)) {
                await content.CopyToAsync(fs);
            }


            await QueueRequest(request);

        }



        protected virtual Task QueueRequest(FileUploadRequest request)
        {

            lock (this)
            {

                request.Identifier = Guid.NewGuid().ToString();

                string existing = ReadPreferences();

                List<FileUploadRequest> requests = Requests;

                requests.Clear();

                if (!string.IsNullOrWhiteSpace(existing))
                {
                    requests.AddRange(JsonConvert.DeserializeObject<List<FileUploadRequest>>(existing));
                }

                requests.Add(request);

                existing = JsonConvert.SerializeObject(requests);

                WritePreferences(existing);

            }

            StartUploadInternal(request);

            SaveState();

            return Task.CompletedTask;

        }


        protected void SaveState() {
            lock (this) {
                var existing = JsonConvert.SerializeObject(Requests);
                WritePreferences(existing);
            }
        }

        public event EventHandler<FileUploadRequestArgs> Progress;

        public event FileStatusUpdate Success;

        public event FileStatusUpdate Failed;

        public event EventHandler<ExceptionArgs> FatalError;

        internal void ReportProgress(FileUploadRequest r)
        {
            Progress?.Invoke(this, new FileUploadRequestArgs(r));
            SaveState();
        }

        internal Task ReportStatus(FileUploadRequest r) {


            return Task.Run(async () =>
            {

                try
                {

                    if (r.ResponseCode == 200)
                    {
                        if (Success != null)
                        {
                            if (await Success(this, r))
                            {
                                r.Processed = true;
                            }
                        }
                    }
                    else
                    {
                        if (Failed != null)
                        {
                            if (await Failed(this, r))
                            {
                                r.Processed = true;
                            }
                        }
                    }

                    if (r.Processed)
                    {
                        // delete file...
                        Requests.Remove(r);
                        SaveState();
                    }
                }
                catch (Exception ex)
                {
                    ReportFatalError(ex);
                }

            });

          
        }

        internal void ReportFatalError(Exception ex) {
            FatalError?.Invoke(this, new ExceptionArgs(ex));
        }
        

        //protected virtual Task QueueRequest(FileUploadRequest request)
        //{
        //    throw new NotImplementedException();
        //}
    }

    public delegate Task<bool> FileStatusUpdate(object sender, FileUploadRequest request);

    public class ExceptionArgs : EventArgs
    {
        public Exception Exception { get; }

        public ExceptionArgs(Exception ex)
        {
            this.Exception = ex;
        }
    }

    public class FileUploadRequestArgs : EventArgs {
        public FileUploadRequestArgs(FileUploadRequest request)
        {
            this.Request = request;
        }

        public FileUploadRequest Request { get; }
    }

    public class FileUploadRequest {

        /// <summary>
        /// 
        /// </summary>
        public string Identifier { get; set; }


        /// <summary>
        /// 
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Method { get; set; }

        public Header[] Headers { get; set; }


        /// <summary>
        /// 
        /// </summary>
        public string ContentType { get; set; }


        /// <summary>
        /// 
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int TotalBytes { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int TotalSent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int ResponseCode { get; set; }


        /// <summary>
        /// 
        /// </summary>
        public string ResponseContentType { get; set; }


        /// <summary>
        /// 
        /// </summary>
        public string ResponseFilePath { get; set; }


        /// <summary>
        /// 
        /// </summary>
        public bool Processed { get; set; }
    }

    public class Header {

        public Header()
        {

        }

        public Header(string key, string v)
        {
            this.Key = key;
            this.Value = v;
        }

        public string Key { get; set; }
        public string Value { get; set; }
    }
}
