using Android.Content;

namespace XamarinFileUploader
{
    public partial class PrefStorage 
    {

        protected virtual string ReadPreferences()
        {
            return Context
                .GetSharedPreferences(StorageKey, Android.Content.FileCreationMode.Private)
                .GetString("files", null);
        }

        protected virtual void WritePreferences(string content)
        {
            Context
                .GetSharedPreferences(StorageKey, Android.Content.FileCreationMode.Private)
                .Edit()
                .PutString("files", content)
                .Commit();
        }


        public Context Context =>
            Android.App.Application.Context;

    }


}
