using System;
using XamarinFileUploader;

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
