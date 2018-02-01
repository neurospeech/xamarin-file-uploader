using Newtonsoft.Json;
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

    public partial class FileUploaderService
    {

        public IFileUploadReceiver Receiver { get; set; }

        public IFileUploadStorage Storage { get; set; }


        public FileUploaderService()
        {

            Receiver = Xamarin.Forms.DependencyService.Get<IFileUploadReceiver>();

            Storage = Receiver.GetStorage() ?? new PrefStorage();

            Storage.StorageKey = StorageKey;

            Xamarin.Forms.Device.BeginInvokeOnMainThread(async () => {
                try {

                    await Task.Delay(1000);

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



        public async Task ReportPendingStatus() {

            SaveState();

            var pending = Storage
                .Get()
                .Where(x => x.ResponseCode != 0 && x.Processed == false)
                .ToList();
            foreach (var r in pending)
            {
                await ReportStatus(r);
            }
        }

        public void CancelUpload(string key) {
            var r = Storage.Get().Where(x => x.Identifier == key).FirstOrDefault();
            if (r == null)
                return;

            r.Cancelled = true;
            SaveState();

            OnCancel(r);
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

        public string StorageKey { get; set; } = "files3";

        protected virtual Task QueueRequest(FileUploadRequest request)
        {

            Storage.Add(request);

            StartUploadInternal(request);

            SaveState();

            return Task.CompletedTask;

        }


        protected void SaveState() {
            Storage.Save();
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

                    //lock (r)
                    //{
                    //    if (r.IsNotifying)
                    //        return;
                    //    r.IsNotifying = true;
                    //}

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
                        Storage.Remove(r);
                        if (System.IO.File.Exists(r.FilePath))
                        {
                            System.IO.File.Delete(r.FilePath);
                        }
                        if (r.ResponseFilePath != null && System.IO.File.Exists(r.ResponseFilePath))
                        {
                            System.IO.File.Delete(r.ResponseFilePath);
                        }
                        SaveState();
                    }
                }
                catch (Exception ex)
                {
                    ReportFatalError(ex);
                }
                //finally
                //{
                //    lock (r)
                //    {
                //        r.IsNotifying = false;
                //    }
                //}

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
}
