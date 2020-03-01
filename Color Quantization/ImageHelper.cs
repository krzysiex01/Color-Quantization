using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;

namespace Color_Quantization
{
    public class ImageHelper
    {
        public async Task<byte[]> ResizeImage(StorageFile file, uint newWidth, uint newHeight)
        {
            Stream imagestream = await file.OpenStreamForReadAsync();
            BitmapDecoder dec = await BitmapDecoder.CreateAsync(imagestream.AsRandomAccessStream());
            BitmapTransform transform = new BitmapTransform();
            transform.InterpolationMode = BitmapInterpolationMode.Fant;
            transform.ScaledHeight = newHeight;
            transform.ScaledWidth = newWidth;
            var data = await dec.GetPixelDataAsync(BitmapPixelFormat.Rgba8,
                        BitmapAlphaMode.Ignore,
                        transform,
                        ExifOrientationMode.IgnoreExifOrientation,
                        ColorManagementMode.DoNotColorManage);
            var bytes = data.DetachPixelData();
            return bytes;
        }
    }
}
