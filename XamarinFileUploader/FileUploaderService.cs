﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using XamarinFileUploader;

[assembly: Xamarin.Forms.Dependency(typeof(FileUploaderService))]

namespace XamarinFileUploader
{

    public interface IFileUploadReceiver {


        Task<bool> CompletedAsync(FileUploadRequest request);

        Task<bool> FailedAsync(FileUploadRequest request);

        void OnProgress(FileUploadRequest request);

        void FatalError(Exception ex);

        HttpClient GetHttpClient();

    }

    public partial class FileUploaderService
    {

        public IFileUploadReceiver Receiver { get; set; }


        public FileUploaderService()
        {

            Receiver = Xamarin.Forms.DependencyService.Get<IFileUploadReceiver>();

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


        public async Task ReportPendingStatus() {

            SaveState();

            var pending = Requests
                .Where(x => x.ResponseCode != 0 && x.Processed == false)
                .ToList();
            foreach (var r in pending)
            {
                await ReportStatus(r);
            }
        }

        public async Task StartUpload(string tag, string url, string method, HttpContent content) {

            FileUploadRequest request = new FileUploadRequest() {
                Identifier = tag,
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

                // request.Identifier = Guid.NewGuid().ToString();

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


        internal void ReportProgress(FileUploadRequest r)
        {
            Receiver.OnProgress(r);
            SaveState();
        }

        internal Task ReportStatus(FileUploadRequest r) {


            return Task.Run(async () =>
            {

                try
                {

                    lock (r)
                    {
                        if (r.IsNotifying)
                            return;
                        r.IsNotifying = true;
                    }

                    if (r.ResponseCode == 200)
                    {
                        if (await Receiver.CompletedAsync(r))
                        {
                            r.Processed = true;
                        }
                    }
                    else
                    {
                        if (await Receiver.FailedAsync(r))
                        {
                            r.Processed = true;
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
                finally
                {
                    lock (r)
                    {
                        r.IsNotifying = false;
                    }
                }

            });

          
        }

        internal void ReportFatalError(Exception ex) {
            Receiver.FatalError(ex);
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

        internal bool IsNotifying = false;

        /// <summary>
        /// 
        /// </summary>
        public string Identifier { get; set; }


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
