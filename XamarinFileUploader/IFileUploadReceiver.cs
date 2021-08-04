using System;
using System.Net.Http;
using System.Threading.Tasks;
using XamarinFileUploader;

namespace XamarinFileUploader
{
    public interface IFileUploadReceiver {


        Task<bool> CompletedAsync(FileUploadRequest request);

        Task<bool> FailedAsync(FileUploadRequest request);

        void OnProgress(FileUploadRequest request);

        void FatalError(Exception ex);

        HttpClient GetHttpClient();

        IFileUploadStorage GetStorage();

    }
}
