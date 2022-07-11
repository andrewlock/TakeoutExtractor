﻿
using System.Threading;
using System.Threading.Tasks;

using uk.andyjohnson.TakeoutExtractor.Lib;


namespace uk.andyjohnson.TakeoutExtractor.Gui
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        
        protected override void OnAppearing()
        {
#if DEBUG
            InputDirEntry.Text = "D:\\Temp\\takeout-20220331T163017Z-001\\Takeout";
            OutputDirEntry.Text = "D:\\Temp\\TakeoutData";
#endif

            PhotosExtractCbx.IsChecked = true;
            var photoOptions = new PhotoOptions();  // for default values
            PhotosFileNameFormatTxt.Text = !string.IsNullOrEmpty(photoOptions.OutputFileNameFormat) ? photoOptions.OutputFileNameFormat : "";
            PhotosUpdateExifCbx.IsChecked = photoOptions.UpdateExif;
            PhotosKeepOriginalsCbx.IsChecked = photoOptions.KeepOriginalsForEdited;
            PhotosSuffixOriginalsTxt.Text = !string.IsNullOrEmpty(photoOptions.OriginalsSuffix) ? photoOptions.OriginalsSuffix : "";
            PhotosSubdirOriginalsTxt.Text = !string.IsNullOrEmpty(photoOptions.OriginalsSubdirName) ? photoOptions.OriginalsSubdirName : "";
        }



        private void OnInputDirButtonClicked(object sender, EventArgs e)
        {
            // TODO: Browser for input dir
        }


        private void OnOutputDirButtonClicked(object sender, EventArgs e)
        {
            // TODO: Browse for output dir
        }


        void OnPhotosExtractChanged(object sender, CheckedChangedEventArgs e)
        {
            PhotosOptionsGrid.IsVisible = e.Value;
        }

        void OnPhotosKeepOriginalsChanged(object sender, CheckedChangedEventArgs e)
        {
            PhotosSuffixOriginalsTxt.IsEnabled = e.Value;
            PhotosSubdirOriginalsTxt.IsEnabled = e.Value;

            // Perform required operation after examining e.Value
            SemanticScreenReader.Announce(PhotosKeepOriginalsLbl.Text);
        }



        private async void OnStartBtnClicked(
            object sender,
            EventArgs e)
        {
            // Show the overlay that give feedback progress.
            MainGrid.IsEnabled = false;
            ProgressOverlay? progressOverlay = new ProgressOverlay();
            progressOverlay.ZIndex = 99;
            // TODO: Improve how this is added to the layout.
            progressOverlay.SetValue(Grid.RowProperty, 0);
            progressOverlay.SetValue(Grid.ColumnProperty, 0);
            MainGrid.Children.Add(progressOverlay);
            //progressOverlay.IsEnabled = true;

            // Set-up extractor and options.
            var options = new List<IExtractorOptions>();
            if (this.PhotosExtractCbx.IsChecked)
            {
                var photoOptions = new PhotoOptions();
                photoOptions.OutputFileNameFormat = PhotosFileNameFormatTxt.Text;
                photoOptions.UpdateExif = PhotosUpdateExifCbx.IsChecked;
                photoOptions.KeepOriginalsForEdited = PhotosKeepOriginalsCbx.IsChecked;
                photoOptions.OriginalsSuffix = PhotosKeepOriginalsCbx.IsChecked ? PhotosSuffixOriginalsTxt.Text : "";
                photoOptions.OriginalsSubdirName = PhotosKeepOriginalsCbx.IsChecked ? PhotosSubdirOriginalsTxt.Text : "";
                options.Add(photoOptions);
            }
            var extractor = new ExtractorManager(new DirectoryInfo(InputDirEntry.Text),
                                                 new DirectoryInfo(OutputDirEntry.Text),
                                                 options,
                                                 verbosity: 1);
            extractor.Progress += (sender, e) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    progressOverlay.ShowProgress(sender, e);
                });
            };

            // Create a cancellation token for the extraction operation, and attach a lambda to the overlay's cancel button
            // that will trigger a cancellation.
            var cancellationTokenSource = new CancellationTokenSource();
            progressOverlay.Cancelled += (sender, e) => 
            { 
                cancellationTokenSource.Cancel(); 
            };

            // Do the extraction.
            try
            {
                await Task.Run(() => extractor.ExtractAsync(cancellationTokenSource.Token));
                await DisplayAlert("Completed", "The extraction operation completed successfully", "Ok");
            }
            catch(OperationCanceledException)
            {
                await DisplayAlert("Cancelled", "The extraction operation was cancelled", "Ok");
            }
            catch(Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "Ok");
            }
            finally
            {
                // Hide the overlay.
                progressOverlay.IsVisible = false;
                progressOverlay = null;
                MainGrid.IsEnabled = true;
            }
        }
    }
}
