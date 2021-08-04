using System;
using XamarinFileUploader;

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
