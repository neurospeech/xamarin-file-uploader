using Android.Content;

namespace XamarinFileUploader
{
    public partial class FileUploaderService
    {
        public Context Context =>
            Android.App.Application.Context;

        private void OnStarted() {
            Intent intent = new Intent(Context, typeof(BackgroundUploadService));
            Context.StartService(intent);
        }

        


        private void StartUploadInternal(FileUploadRequest request)
        {
            
            Intent intent = new Intent(Context, typeof(BackgroundUploadService));
            Context.StartService(intent);
        }

        private void OnCancel(FileUploadRequest request) {
            BackgroundUploadService.CancellationTokenSource?.Cancel(false);
        }

    }


}
