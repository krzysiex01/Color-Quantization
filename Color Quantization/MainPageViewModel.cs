using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;

namespace Color_Quantization
{
    class MainPageViewModel : INotifyPropertyChanged
    {
        private BitmapImage orginalImageSource;
        private BitmapSource transformedImageSource1;
        private BitmapSource transformedImageSource2;
        private BitmapSource transformedImageSource3;
        private bool image1IsProcessing;
        private bool image2IsProcessing;
        private bool image3IsProcessing;

        //Implementation of INotifyPropertyChanged interface
        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        public BitmapImage OrginalImageSource { get => orginalImageSource; set { orginalImageSource = value; RaisePropertyChanged("OrginalImageSource"); } }
        public BitmapSource TransformedImageSource1 { get => transformedImageSource1; set { transformedImageSource1 = value; } }
        public BitmapSource TransformedImageSource2 { get => transformedImageSource2; set { transformedImageSource2 = value; } }
        public BitmapSource TransformedImageSource3 { get => transformedImageSource3; set { transformedImageSource3 = value; } }

        public bool Image1IsProcessing { get => image1IsProcessing; set { image1IsProcessing = value; RaisePropertyChanged("Image1IsProcessing"); } }
        public bool Image2IsProcessing { get => image2IsProcessing; set { image2IsProcessing = value; RaisePropertyChanged("Image2IsProcessing"); } }
        public bool Image3IsProcessing { get => image3IsProcessing; set { image3IsProcessing = value; RaisePropertyChanged("Image3IsProcessing"); } }
    }
}

