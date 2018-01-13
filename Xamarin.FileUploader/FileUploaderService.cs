using System;
using System.Collections.Generic;
using System.Text;

namespace Xamarin.FileUploader
{
    public partial class FileUploaderService
    {

        private static FileUploaderService _Instance = null;
        public static FileUploaderService Instance => 
            (_Instance ?? (_Instance = Xamarin.Forms.DependencyService.Get<FileUploaderService>()));





    }
}
