using XamarinFileUploader;

namespace XamarinFileUploader
{
    public class Header {

        public Header()
        {

        }

        public Header(string key, string v)
        {
            this.Key = key;
            this.Value = v;
        }

        public string Key { get; set; }
        public string Value { get; set; }
    }
}
