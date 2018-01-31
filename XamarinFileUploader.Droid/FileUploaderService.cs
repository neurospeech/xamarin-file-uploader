using Android.Content;

namespace XamarinFileUploader
{
    public partial class FileUploaderService
    {
        public Context Context =>
            Xamarin.Forms.Forms.Context;

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
