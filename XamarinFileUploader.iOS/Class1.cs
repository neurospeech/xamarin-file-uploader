using Foundation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UIKit;

namespace XamarinFileUploader
{
    public partial class FileUploaderService
    {

        private FileUploaderHandler _handler;
        public FileUploaderHandler Delegate =>
            (_handler ?? (_handler = new FileUploaderHandler()));

        private void OnStarted()
        {

            System.Threading.Tasks.Task.Run(async () =>
            {
                await System.Threading.Tasks.Task.Delay(1000);
                await FileUploaderService.Instance.ReportPendingStatus();
            });
        }

        protected virtual string ReadPreferences()
        {
            return NSUserDefaults.StandardUserDefaults.StringForKey("pending-upload-files");
        }

        protected virtual void WritePreferences(string content)
        {
            NSUserDefaults.StandardUserDefaults.SetString(content, "pending-upload-files");
            NSUserDefaults.StandardUserDefaults.Synchronize();
        }

        private void StartUploadInternal(FileUploadRequest r)
        {
            var configuration = NSUrlSessionConfiguration.CreateBackgroundSessionConfiguration(r.Identifier);

            // max timeout is one hour...

            configuration.TimeoutIntervalForResource = 60 * 60;

            NSUrlSession session = NSUrlSession.FromConfiguration(configuration, (INSUrlSessionDelegate)this, NSOperationQueue.MainQueue);

            var request = new NSMutableUrlRequest(NSUrl.FromString(r.Url));
            request.HttpMethod = r.Method;


            NSMutableDictionary headers = new NSMutableDictionary();
            headers.SetValueForKey(new NSString(r.ContentType), (NSString)"Content-Type");

            if (r.Headers != null)
            {
                foreach (var h in r.Headers)
                {
                    headers.SetValueForKey(new NSString(h.Key), new NSString(h.Value));
                }
            }
            request.Headers = headers;


            var uploadTask = session.CreateUploadTask(request, NSUrl.FromFilename(r.FilePath));

            uploadTask.Resume();

        }
    }

    public class FileUploaderHandler : NSUrlSessionDataDelegate { 

        public Action CompletionHandler { get; set; }


        public override void DidBecomeInvalid(NSUrlSession session, NSError error)
        {
            Update(session.Configuration.Identifier, error);

        }

        public override void DidCompleteWithError(NSUrlSession session, NSUrlSessionTask task, NSError error)
        {
            string key = session.Configuration.Identifier;

            if (error != null)
            {
                Update(session.Configuration.Identifier, error);
            }
            else
            {
                Update(key, null);
            }

        }

        private void Update(string identifier, NSError error)
        {
            var urlKey = identifier;

            var r = FileUploaderService.Instance.Requests.FirstOrDefault(x => x.Identifier == identifier);
            if (error != null)
            {
                r.ResponseCode = 500;

                using (var s = System.IO.File.OpenWrite(r.ResponseFilePath))
                {
                    s.Seek(0, SeekOrigin.End);
                    string errors = error.LocalizedFailureReason 
                        + error.LocalizedDescription ;
                    var bytes = System.Text.Encoding.UTF8.GetBytes(errors);
                    s.Write(bytes, 0, bytes.Length);
                }

            }
            else {
                r.ResponseCode = 200;
            }

            System.Threading.Tasks.Task.Run(async () => {
                await FileUploaderService.Instance.ReportPendingStatus();
                CompletionHandler?.Invoke();
            });
        }

        public override void DidReceiveData(NSUrlSession session, NSUrlSessionDataTask dataTask, NSData data)
        {
            //Update(session.Configuration.Identifier, data.ToArray(), null);
            string key = session.Configuration.Identifier;

            var r = FileUploaderService.Instance.Requests.FirstOrDefault(x => x.Identifier == session.Configuration.Identifier);

            var bytes = data.ToArray();
            using (var s = System.IO.File.OpenWrite(r.ResponseFilePath))
            {
                s.Seek(0, SeekOrigin.End);
                s.Write(bytes, 0, bytes.Length);
            }
        }

        public override void DidSendBodyData(NSUrlSession session, NSUrlSessionTask task, long bytesSent, long totalBytesSent, long totalBytesExpectedToSend)
        {
            string urlKey = session.Configuration.Identifier;

            var r = FileUploaderService.Instance.Requests.FirstOrDefault(x => x.Identifier == session.Configuration.Identifier);
            r.TotalBytes = (int)totalBytesExpectedToSend;
            r.TotalSent = (int)totalBytesSent;

            FileUploaderService.Instance.ReportProgress(r);
        }

    }
}
