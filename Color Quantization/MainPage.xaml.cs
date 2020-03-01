using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

//Szablon elementu Pusta strona jest udokumentowany na stronie https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x415

namespace Color_Quantization
{
    /// <summary>
    /// Pusta strona, która może być używana samodzielnie lub do której można nawigować wewnątrz ramki.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.DataContext = new MainPageViewModel();
            this.InitializeComponent();
        }

        private int imageHeight;
        private int imageWidth;
        private byte[,,] imageData;
        private byte[] rawImageData;

        private WriteableBitmap bitmap1;
        private WriteableBitmap bitmap2;
        private WriteableBitmap bitmap3;

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            StorageFile file = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFileAsync(@"Assets\defaultImage.jpg");
            if (file != null)
            {
                Stream imagestream = await file.OpenStreamForReadAsync();
                var randomAccessStream = imagestream.AsRandomAccessStream();
                BitmapDecoder dec = await BitmapDecoder.CreateAsync(randomAccessStream);

                imageHeight = (int)dec.PixelHeight;
                imageWidth = (int)dec.PixelWidth;

                var data = await dec.GetPixelDataAsync(BitmapPixelFormat.Bgra8,
                            BitmapAlphaMode.Ignore,
                            new BitmapTransform(),
                            ExifOrientationMode.IgnoreExifOrientation,
                            ColorManagementMode.DoNotColorManage);

                var bytes = data.DetachPixelData();
                rawImageData = bytes;
                imageData = new byte[imageHeight, imageWidth, 4];
                Buffer.BlockCopy(bytes, 0, imageData, 0, bytes.Length);

                BitmapImage bitmap = new BitmapImage();
                bitmap.SetSource(randomAccessStream);
                ((MainPageViewModel)DataContext).OrginalImageSource = bitmap;

                bitmap1 = new WriteableBitmap(imageWidth, imageHeight);
                bitmap2 = new WriteableBitmap(imageWidth, imageHeight);
                bitmap3 = new WriteableBitmap(imageWidth, imageHeight);

                ((MainPageViewModel)DataContext).TransformedImageSource1 = bitmap1;
                ((MainPageViewModel)DataContext).TransformedImageSource2 = bitmap2;
                ((MainPageViewModel)DataContext).TransformedImageSource3 = bitmap3;
            }
        }

        private async void WriteToBitmap(WriteableBitmap bitmap, byte[,,] pixels)
        {
            //Check if bitmap was created.
            if (bitmap is null)
            {
                return;
            }
            int w, h;
            //Gets bitmap size
            w = bitmap.PixelWidth;
            h = bitmap.PixelHeight;
            int length = h * w * 4;
            //Format: 4bytes per pixel 
            byte[] sourcePixels = new byte[length];
            //Converts 3d array to 1d
            Buffer.BlockCopy(pixels, 0, sourcePixels, 0, length);

            //Opens a stream to copy the content of sourcePixels to the WriteableBitmap's pixel buffer 
            using (Stream stream = bitmap.PixelBuffer.AsStream())
            {
                await stream.WriteAsync(sourcePixels, 0, sourcePixels.Length);
            }
            bitmap.Invalidate();
        }

        private async void SelectImage(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");
            StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                Stream imagestream = await file.OpenStreamForReadAsync();
                BitmapDecoder dec = await BitmapDecoder.CreateAsync(imagestream.AsRandomAccessStream());
                imageHeight = (int)dec.PixelHeight;
                imageWidth = (int)dec.PixelWidth;
                var data = await dec.GetPixelDataAsync(BitmapPixelFormat.Bgra8,
                            BitmapAlphaMode.Ignore,
                            new BitmapTransform(),
                            ExifOrientationMode.IgnoreExifOrientation,
                            ColorManagementMode.DoNotColorManage);
                var bytes = data.DetachPixelData();
                imageData = new byte[imageWidth, imageHeight, 4];
                Buffer.BlockCopy(bytes, 0, imageData, 0, bytes.Length);
                BitmapImage bitmap = new BitmapImage();
                bitmap.SetSource(imagestream.AsRandomAccessStream());
                ((MainPageViewModel)DataContext).OrginalImageSource = bitmap;
                await new MessageDialog($"Your texture was successfully loaded.", "Success!").ShowAsync();
            }
        }

        private void ProcessingCompleted(int imageNumber, WriteableBitmap bitmap, byte[,,] pixels)
        {
            switch (imageNumber)
            {
                case 1:
                    WriteToBitmap(bitmap1, pixels);
                    ((MainPageViewModel)DataContext).Image1IsProcessing = false;
                    ((MainPageViewModel)DataContext).RaisePropertyChanged("TransformedImageSource1");
                    break;
                case 2:
                    WriteToBitmap(bitmap2, pixels);
                    ((MainPageViewModel)DataContext).Image2IsProcessing = false;
                    ((MainPageViewModel)DataContext).RaisePropertyChanged("TransformedImageSource2");
                    break;
                case 3:
                    WriteToBitmap(bitmap3, pixels);
                    ((MainPageViewModel)DataContext).Image3IsProcessing = false;
                    ((MainPageViewModel)DataContext).RaisePropertyChanged("TransformedImageSource3");
                    break;
                default:
                    break;
            }
        }

        private async void ReduceColors(object sender, RoutedEventArgs e)
        {
            int numberOfColors = (int)NumberOfColorsSlider.Value;
            ((MainPageViewModel)DataContext).Image1IsProcessing = true;
            ((MainPageViewModel)DataContext).Image2IsProcessing = true;
            ((MainPageViewModel)DataContext).Image3IsProcessing = true;

            var transformedImage1 = Task.Run(() =>
            {
                return GraphicAlgorithms.ErrorDiffusionDithering((byte[,,])imageData.Clone(), numberOfColors);
            });
            var transformedImage2 = Task.Run(() =>
            {
                return GraphicAlgorithms.PopularityAlgorithm((byte[,,])imageData.Clone(), (int)Math.Pow(numberOfColors, 3));
            });
            var transformedImage3 = Task.Run(() =>
            {
                return GraphicAlgorithms.K_MeansAlgorithm((byte[,,])imageData.Clone(), (int)Math.Pow(numberOfColors, 3));
            });

            ProcessingCompleted(1, bitmap1, await transformedImage1);
            ProcessingCompleted(2, bitmap2, await transformedImage2);
            ProcessingCompleted(3, bitmap3, await transformedImage3);
        }

        private void OnFileDragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;

            if (e.DragUIOverride != null)
            {
                e.DragUIOverride.Caption = "Add file";
                e.DragUIOverride.IsContentVisible = true;
            }

            this.AddFilePanel.Visibility = Visibility.Visible;
            this.InputColumn.Visibility = Visibility.Collapsed;
            this.OutputColumn.Visibility = Visibility.Collapsed;
        }

        private void OnFileDragLeave(object sender, DragEventArgs e)
        {
            this.AddFilePanel.Visibility = Visibility.Collapsed;
            this.InputColumn.Visibility = Visibility.Visible;
            this.OutputColumn.Visibility = Visibility.Visible;

        }

        private async void OnFileDrop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Count == 1)
                {
                    if (items[0] is StorageFile file)
                    {
                        if (file.FileType == ".jpg" || file.FileType == ".png" || file.FileType == ".jpeg")
                        {

                            Stream imagestream = await file.OpenStreamForReadAsync();
                            var randomAccessStream = imagestream.AsRandomAccessStream();
                            BitmapDecoder dec = await BitmapDecoder.CreateAsync(randomAccessStream);

                            imageHeight = (int)dec.PixelHeight;
                            imageWidth = (int)dec.PixelWidth;

                            var data = await dec.GetPixelDataAsync(BitmapPixelFormat.Bgra8,
                                        BitmapAlphaMode.Ignore,
                                        new BitmapTransform(),
                                        ExifOrientationMode.IgnoreExifOrientation,
                                        ColorManagementMode.DoNotColorManage);

                            var bytes = data.DetachPixelData();
                            rawImageData = bytes;
                            imageData = new byte[imageHeight, imageWidth, 4];
                            Buffer.BlockCopy(bytes, 0, imageData, 0, bytes.Length);

                            BitmapImage bitmap = new BitmapImage();
                            bitmap.SetSource(randomAccessStream);
                            ((MainPageViewModel)DataContext).OrginalImageSource = bitmap;

                            bitmap1 = new WriteableBitmap(imageWidth, imageHeight);
                            bitmap2 = new WriteableBitmap(imageWidth, imageHeight);
                            bitmap3 = new WriteableBitmap(imageWidth, imageHeight);

                            ((MainPageViewModel)DataContext).TransformedImageSource1 = bitmap1;
                            ((MainPageViewModel)DataContext).TransformedImageSource2 = bitmap2;
                            ((MainPageViewModel)DataContext).TransformedImageSource3 = bitmap3;
                        }
                    }
                }
            }

            this.AddFilePanel.Visibility = Visibility.Collapsed;
            this.InputColumn.Visibility = Visibility.Visible;
            this.OutputColumn.Visibility = Visibility.Visible;
        }
    }
}

