using System;
using XamarinFileUploader;

[assembly: Xamarin.Forms.Dependency(typeof(FileUploaderService))]

namespace XamarinFileUploader
{
    public class FileUploadRequestArgs : EventArgs {
        public FileUploadRequestArgs(FileUploadRequest request)
        {
            this.Request = request;
        }

        public FileUploadRequest Request { get; }
    }
}
