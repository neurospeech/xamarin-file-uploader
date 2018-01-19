using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace XamarinFileUploader
{
    public partial class FileUploaderService
    {

        private static FileUploaderService _Instance = null;
        public static FileUploaderService Instance =>
            _Instance ?? (_Instance = Xamarin.Forms.DependencyService.Get<FileUploaderService>());



        public async Task StartUpload(string tag, string url, string method, HttpContent content) {

            FileUploadRequest request = new FileUploadRequest() {
                Tag = tag,
                ContentType = content.Headers.ContentType.ToString(),
                FilePath = System.IO.Path.GetTempFileName(),
                Method = method,
                Url = url
            };

            

            using (var fs = System.IO.File.OpenWrite(request.FilePath)) {
                await content.CopyToAsync(fs);
            }


            await QueueRequest(request);

        }

        protected virtual Task QueueRequest(FileUploadRequest request)
        {
            throw new NotImplementedException();
        }
    }

    public class FileUploadRequest {

        /// <summary>
        /// 
        /// </summary>
        public string Identifier { get; set; }


        /// <summary>
        /// 
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Method { get; set; }


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
    }
}
