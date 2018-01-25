using Foundation;

namespace XamarinFileUploader
{
    public partial class PrefStorage
    {

        protected virtual string ReadPreferences()
        {
            return NSUserDefaults.StandardUserDefaults.StringForKey($"{StorageKey}-pending-upload-files");
        }

        protected virtual void WritePreferences(string content)
        {
            NSUserDefaults.StandardUserDefaults.SetString(content, $"{StorageKey}-pending-upload-files");
            NSUserDefaults.StandardUserDefaults.Synchronize();
        }
    }
}
