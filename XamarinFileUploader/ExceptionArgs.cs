using System;
using XamarinFileUploader;

[assembly: Xamarin.Forms.Dependency(typeof(FileUploaderService))]

namespace XamarinFileUploader
{
    public class ExceptionArgs : EventArgs
    {
        public Exception Exception { get; }

        public ExceptionArgs(Exception ex)
        {
            this.Exception = ex;
        }
    }
}
