using XamarinFileUploader;

[assembly: Xamarin.Forms.Dependency(typeof(FileUploaderService))]

namespace XamarinFileUploader
{
    public class FileUploadRequest {

        internal bool IsNotifying = false;

        /// <summary>
        /// 
        /// </summary>
        public string Identifier { get; set; }


        /// <summary>
        /// 
        /// </summary>
        public string Method { get; set; }

        public Header[] Headers { get; set; }


        /// <summary>
        /// 
        /// </summary>
        public string ContentType { get; set; }


        /// <summary>
        /// 
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int TotalBytes { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int TotalSent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int ResponseCode { get; set; }


        /// <summary>
        /// 
        /// </summary>
        public string ResponseContentType { get; set; }


        /// <summary>
        /// 
        /// </summary>
        public string ResponseFilePath { get; set; }


        /// <summary>
        /// 
        /// </summary>
        public bool Processed { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool Cancelled { get; set; }
    }
}
