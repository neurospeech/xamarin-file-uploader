using Foundation;
using System;
using System.Collections.Generic;

namespace XamarinFileUploader
{
    public partial class FileUploaderService
    {

        private FileUploaderHandler _handler;
        public FileUploaderHandler Delegate =>
            (_handler ?? (_handler = new FileUploaderHandler()));

        public IEnumerable<FileUploadRequest> Requests =>
            Storage.Get();


        public NSUrlSession GetSession(string identifier) {

            var configuration = NSUrlSessionConfiguration.CreateBackgroundSessionConfiguration(identifier);

            // max timeout is one hour...

            configuration.TimeoutIntervalForResource = 60 * 60;

            return NSUrlSession.FromConfiguration(configuration, (INSUrlSessionDelegate)Delegate, NSOperationQueue.MainQueue);
        }


        public void HandleEventsForBackground(string sessionIdentifier, Action completionHandler)
        {
            Delegate.CompletionHandler = completionHandler;

            GetSession(sessionIdentifier);
        }


        private void OnStarted()
        {

            System.Threading.Tasks.Task.Run(async () =>
            {
                await System.Threading.Tasks.Task.Delay(1000);
                await FileUploaderService.Instance.ReportPendingStatus();
            });
        }

        private void OnCancel(FileUploadRequest r)
        {
            GetSession(r.Identifier).InvalidateAndCancel();
        }



        private void StartUploadInternal(FileUploadRequest r)
        {
            var session = GetSession(r.Identifier);

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
}
