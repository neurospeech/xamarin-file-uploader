using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using XamarinFileUploader;

namespace XamarinFileUploader
{
    public partial class PrefStorage : IFileUploadStorage
    {

        public string StorageKey { get; set; }

        public List<FileUploadRequest> requests = new List<FileUploadRequest>();

        public PrefStorage()
        {
            Read();
        }

        private void Read()
        {
            string existing = Xamarin.Essentials.Preferences.Get("xfu", null);

            lock (this)
            {
                //requests.Clear();

                if (!string.IsNullOrWhiteSpace(existing))
                {
                    foreach (var item in (JsonConvert.DeserializeObject<List<FileUploadRequest>>(existing))) {
                        int index = requests.FindIndex(x => x.Identifier == item.Identifier);
                        if (index == -1)
                        {
                            requests.Add(item);
                        }
                        else {
                            requests[index] = item;
                        }
                    }
                }
            }
        }

        public void Add(FileUploadRequest request)
        {
            lock (this)
            {
                if (requests.Any(x => x.Identifier == request.Identifier))
                    throw new InvalidOperationException();
                requests.Add(request);
                Save(false);
            }
        }


        public bool Remove(FileUploadRequest request)
        {
            lock (this)
            {
                int index = requests.FindIndex(x => x.Identifier == request.Identifier);
                if (index != -1)
                {
                    requests.RemoveAt(index);
                    Save(false);
                    return true;
                }
            }
            return false;
        }


        public IEnumerable<FileUploadRequest> Get()
        {
            lock (this)
            {
                return requests.ToList();
            }
        }

        private void Save(bool locked) {
            if (locked)
            {
                lock (this)
                {
                    SaveInternal();
                }
            }
            else {
                SaveInternal();
            }
        }

        private void SaveInternal()
        {
            Xamarin.Essentials.Preferences.Set("xfu", JsonConvert.SerializeObject(requests));
        }

        public void Save()
        {
            Save(true);
        }
    }
}
