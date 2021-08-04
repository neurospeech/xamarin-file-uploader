using System;
using System.Collections.Generic;
using XamarinFileUploader;

[assembly: Xamarin.Forms.Dependency(typeof(FileUploaderServiceImpl))]

namespace XamarinFileUploader
{
    public class FileUploaderServiceImpl : FileUploaderService
    {
        protected override void OnCancel(FileUploadRequest r)
        {
            throw new NotImplementedException();
        }

        protected override void OnStarted()
        {
            throw new NotImplementedException();
        }

        protected override void StartUploadInternal(FileUploadRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
