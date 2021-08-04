using Foundation;
using System;
using System.Collections.Generic;
using XamarinFileUploader;

[assembly: Xamarin.Forms.Dependency(typeof(FileUploaderServiceImpl))]

namespace XamarinFileUploader
{
    public class FileUploaderServiceImpl: FileUploaderService
    {

        private FileUploaderHandler _handler;
        public FileUploaderHandler Delegate =>
            (_handler ?? (_handler = new FileUploaderHandler()));

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


        protected override void OnStarted()
        {

            System.Threading.Tasks.Task.Run(async () =>
            {
                await System.Threading.Tasks.Task.Delay(1000);
                await FileUploaderService.Instance.ReportPendingStatus();
            });
        }

        protected override void OnCancel(FileUploadRequest r)
        {
            GetSession(r.Identifier).InvalidateAndCancel();
        }



        protected override void StartUploadInternal(FileUploadRequest r)
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
