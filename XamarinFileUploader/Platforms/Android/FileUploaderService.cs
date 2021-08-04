using Android.Content;
using XamarinFileUploader;

[assembly: Xamarin.Forms.Dependency(typeof(FileUploaderServiceImpl))]

namespace XamarinFileUploader
{
    public class FileUploaderServiceImpl: FileUploaderService
    {
        public Context Context =>
            Android.App.Application.Context;

        protected override void OnStarted() {
            Intent intent = new Intent(Context, typeof(BackgroundUploadService));
            Context.StartService(intent);
        }




        protected override void StartUploadInternal(FileUploadRequest request)
        {
            
            Intent intent = new Intent(Context, typeof(BackgroundUploadService));
            Context.StartService(intent);
        }

        protected override void OnCancel(FileUploadRequest request) {
            BackgroundUploadService.CancellationTokenSource?.Cancel(false);
        }

    }


}
