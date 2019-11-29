using Foundation;
using System;
using System.IO;
using System.Linq;
using System.Text;
using UIKit;

namespace XamarinFileUploader
{

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
            if (r == null)
            {
                return;
            }
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

            if (System.IO.File.Exists(r.FilePath)) {
                System.IO.File.Delete(r.FilePath);
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
            if (r == null)
                return;
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
            if (r == null)
            {
                return;
            }
            r.TotalBytes = (int)totalBytesExpectedToSend;
            r.TotalSent = (int)totalBytesSent;

            FileUploaderService.Instance.ReportProgress(r);
        }

    }
}
