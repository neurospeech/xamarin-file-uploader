using System.Collections.Generic;
using XamarinFileUploader;

namespace XamarinFileUploader
{
    public interface IFileUploadStorage {

        IEnumerable<FileUploadRequest> Get();

        void Save();

        void Add(FileUploadRequest request);

        bool Remove(FileUploadRequest request);

        string StorageKey { get; set; }
             

    }
}
