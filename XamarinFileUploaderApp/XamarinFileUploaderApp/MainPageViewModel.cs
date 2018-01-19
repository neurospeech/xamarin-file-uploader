using Plugin.Media;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace XamarinFileUploaderApp
{
    internal class MainPageViewModel: System.ComponentModel.INotifyPropertyChanged
    {
        public string Message { get; set; }

        public int Progress { get; set; }

        

        public MainPageViewModel()
        {
            UploadCommand = new AppCommand(OnUploadAsync);

            XamarinFileUploader.FileUploaderService.Instance.FatalError += Instance_FatalError;
            XamarinFileUploader.FileUploaderService.Instance.Progress += Instance_Progress;
            XamarinFileUploader.FileUploaderService.Instance.Success += Instance_Success;
            XamarinFileUploader.FileUploaderService.Instance.Failed += Instance_Failed;
        }

        private bool Instance_Failed(object sender, XamarinFileUploader.FileUploadRequest request)
        {
            Message = "Upload failed";
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Message)));
            return true;
        }

        private bool Instance_Success(object sender, XamarinFileUploader.FileUploadRequest e)
        {
            Message = "Upload Success";
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Message)));
            return true;
        }

        private void Instance_Progress(object sender, XamarinFileUploader.FileUploadRequestArgs e)
        {
            Progress = e.Request.TotalSent * 100 / e.Request.TotalBytes;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Progress)));
        }

        private void Instance_FatalError(object sender, XamarinFileUploader.ExceptionArgs e)
        {
            Message = e.Exception.ToString();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Message)));
        }

        private async Task OnUploadAsync()
        {
            var file = await CrossMedia.Current.PickVideoAsync();


            using (var fs = file.GetStream())
            {
                System.Net.Http.MultipartFormDataContent mfd = new System.Net.Http.MultipartFormDataContent();

                var content = new System.Net.Http.StreamContent(fs);

                mfd.Add(content, "file1", System.IO.Path.GetFileName(file.Path));

                await XamarinFileUploader.FileUploaderService.Instance.StartUpload("1",
                    "https://secure.800casting.com/upload/uploadtemp",
                    "POST",
                    mfd);
            }
            
        }

        public AppCommand UploadCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class AppCommand : ICommand
    {
        private Func<Task> task;

        public AppCommand(Func<Task> task)
        {
            this.task = task;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            Task.Run(async () => {
                try {
                    await task();
                } catch (Exception ex) {
                    System.Diagnostics.Debug.Fail(ex.Message, ex.ToString());
                }
            });
        }
    }
}