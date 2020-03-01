using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Media.Imaging;

namespace Color_Quantization
{

    public class GraphicAlgorithms
    {
        private static int FindClosestColor(byte[,] palette, byte[] pixel)
        {
            int minDistance = int.MaxValue;
            int closestColorIndex = 0;
            int dist;
            for (int i = 0; i < palette.GetLength(0); i++)
            {
                int dist_r = palette[i, 2] - pixel[2];
                int dist_g = palette[i, 1] - pixel[1];
                int dist_b = palette[i, 0] - pixel[0];
                dist = (dist_r * dist_r) + (dist_g * dist_g) + (dist_b * dist_b);
                if (dist < minDistance)
                {
                    closestColorIndex = i;
                    minDistance = dist;
                }
            }
            return closestColorIndex;
        }

        private static Color FindClosestColor(IList<Color> palette, byte[] pixel)
        {
            int minDistance = int.MaxValue;
            Color closestColor;
            int dist;
            foreach (var color in palette)
            {
                int dist_r = color.R - pixel[2];
                int dist_g = color.G - pixel[1];
                int dist_b = color.B - pixel[0];
                dist = (dist_r * dist_r) + (dist_g * dist_g) + (dist_b * dist_b);
                if (dist < minDistance)
                {
                    closestColor = color;
                    minDistance = dist;
                }
            }
            return closestColor;
        }

        private static byte[,] CreatePalette(int k)
        {
            byte[,] palette = new byte[k * k * k, 3];
            int index = 0;
            for (int x = 0; x < k; x++)
            {
                for (int y = 0; y < k; y++)
                {
                    for (int z = 0; z < k; z++)
                    {
                        palette[index, 0] = (byte)((255 / k) * x + (255 / (k * 2)));
                        palette[index, 1] = (byte)((255 / k) * y + (255 / (k * 2)));
                        palette[index, 2] = (byte)((255 / k) * z + (255 / (k * 2)));
                        index++;
                    }
                }
            }
            return palette;
        }

        private static byte ToByte(int value)
        {
            return value > 255 ? (byte)255 : value < 0 ? (byte)0 : (byte)value;
        }

        private static byte[,] GetDominantColors(byte[,,] image, int numberOfColors)
        {
            int binsCount = 64;
            int binSize = 256 / binsCount;
            byte[,] maxIndexes = new byte[numberOfColors, 3];
            int[,,] histogram = new int[binsCount, binsCount, binsCount]; // For Blue-Green-Red
            int imageHeight = image.GetLength(0);
            int imageWidth = image.GetLength(1);
            for (int row = 0; row < imageHeight; row++)
            {
                for (int col = 0; col < imageWidth; col++)
                {
                    //Blue
                    int b = image[row, col, 0] / binSize;
                    //Green
                    int g = image[row, col, 1] / binSize;
                    //Red
                    int r = image[row, col, 2] / binSize;

                    histogram[b, g, r]++;
                }
            }

            for (int i = 0; i < numberOfColors; i++)
            {
                int max = -1;
                for (int x = 0; x < binsCount; x++)
                {
                    for (int y = 0; y < binsCount; y++)
                    {
                        for (int z = 0; z < binsCount; z++)
                        {
                            if (max < histogram[x, y, z])
                            {
                                maxIndexes[i, 0] = ToByte(x * binSize - binSize / 2);
                                maxIndexes[i, 1] = ToByte(y * binSize - binSize / 2);
                                maxIndexes[i, 2] = ToByte(z * binSize - binSize / 2);
                                max = histogram[x, y, z];
                                histogram[x, y, z] = -1;
                            }
                        }
                    }
                }
            }

            return maxIndexes;
        }

        public static byte[,,] ErrorDiffusionDithering(byte[,,] orginalImage, int k)
        {
            int imageHeight = orginalImage.GetLength(0);
            int imageWidth = orginalImage.GetLength(1);
            byte[,,] resultArray = new byte[imageHeight, imageWidth, 4];
            byte[,] palette = CreatePalette(k);

            for (int i = 0; i < imageHeight; i++)
            {
                for (int j = 0; j < imageWidth; j++)
                {
                    int colorIndex = FindClosestColor(palette, new byte[] { orginalImage[i, j, 0], orginalImage[i, j, 1], orginalImage[i, j, 2] });
                    byte[] transformedPixel = new byte[] { palette[colorIndex, 0], palette[colorIndex, 1], palette[colorIndex, 2] };

                    resultArray[i, j, 0] = transformedPixel[0];
                    resultArray[i, j, 1] = transformedPixel[1];
                    resultArray[i, j, 2] = transformedPixel[2];
                    resultArray[i, j, 3] = 255;

                    int redError;
                    int blueError;
                    int greenError;

                    blueError = orginalImage[i, j, 0] - transformedPixel[0];
                    greenError = orginalImage[i, j, 1] - transformedPixel[1];
                    redError = orginalImage[i, j, 2] - transformedPixel[2];

                    if (j + 1 < imageWidth)
                    {
                        // right
                        orginalImage[i, j + 1, 2] = ToByte(orginalImage[i, j + 1, 2] + ((redError * 7) >> 4));
                        orginalImage[i, j + 1, 1] = ToByte(orginalImage[i, j + 1, 1] + ((greenError * 7) >> 4));
                        orginalImage[i, j + 1, 0] = ToByte(orginalImage[i, j + 1, 0] + ((blueError * 7) >> 4));
                    }

                    if (i + 1 < imageHeight)
                    {
                        if (j - 1 > 0)
                        {
                            // left and down
                            orginalImage[i + 1, j - 1, 2] = ToByte(orginalImage[i + 1, j - 1, 2] + ((redError * 3) >> 4));
                            orginalImage[i + 1, j - 1, 1] = ToByte(orginalImage[i + 1, j - 1, 1] + ((greenError * 3) >> 4));
                            orginalImage[i + 1, j - 1, 0] = ToByte(orginalImage[i + 1, j - 1, 0] + ((blueError * 3) >> 4));
                        }

                        // down
                        orginalImage[i + 1, j, 2] = ToByte(orginalImage[i + 1, j, 2] + ((redError * 5) >> 4));
                        orginalImage[i + 1, j, 1] = ToByte(orginalImage[i + 1, j, 1] + ((greenError * 5) >> 4));
                        orginalImage[i + 1, j, 0] = ToByte(orginalImage[i + 1, j, 0] + ((blueError * 5) >> 4));

                        if (j + 1 < imageWidth)
                        {
                            // right and down
                            orginalImage[i + 1, j + 1, 2] = ToByte(orginalImage[i + 1, j + 1, 2] + ((redError * 1) >> 4));
                            orginalImage[i + 1, j + 1, 1] = ToByte(orginalImage[i + 1, j + 1, 1] + ((greenError * 1) >> 4));
                            orginalImage[i + 1, j + 1, 0] = ToByte(orginalImage[i + 1, j + 1, 0] + ((blueError * 1) >> 4));
                        }
                    }
                }
            }

            return resultArray;
        }



        public static byte[,,] PopularityAlgorithm(byte[,,] orginalImage, int numberOfColors)
        {
            byte[,] palette = GetDominantColors(orginalImage, numberOfColors);
            int imageHeight = orginalImage.GetLength(0);
            int imageWidth = orginalImage.GetLength(1);
            byte[,,] resultArray = new byte[imageHeight, imageWidth, 4];

            for (int i = 0; i < imageHeight; i++)
            {
                for (int j = 0; j < imageWidth; j++)
                {
                    int colorIndex = FindClosestColor(palette, new byte[] { orginalImage[i, j, 0], orginalImage[i, j, 1], orginalImage[i, j, 2] });

                    resultArray[i, j, 0] = palette[colorIndex, 0];
                    resultArray[i, j, 1] = palette[colorIndex, 1];
                    resultArray[i, j, 2] = palette[colorIndex, 2];
                    resultArray[i, j, 3] = 255;
                }
            }

            return resultArray;
        }

        public static byte[,,] K_MeansAlgorithm(byte[,,] orginalImage, int numberOfColors)
        {
            int imageHeight = orginalImage.GetLength(0);
            int imageWidth = orginalImage.GetLength(1);
            byte[,,] resultArray = new byte[imageHeight, imageWidth, 4];
            IList<Color> data = new List<Color>(orginalImage.Length);


            for (int row = 0; row < imageHeight; row++)
            {
                for (int col = 0; col < imageWidth; col++)
                {
                    data.Add(Color.FromArgb(0, orginalImage[row, col, 2], orginalImage[row, col, 1], orginalImage[row, col, 0]));
                }
            }

            IList<Color> colors = KMeansHelper.GetPalette(numberOfColors, data, 20d);

            for (int i = 0; i < imageHeight; i++)
            {
                for (int j = 0; j < imageWidth; j++)
                {
                    Color color = FindClosestColor(colors, new byte[] { orginalImage[i, j, 0], orginalImage[i, j, 1], orginalImage[i, j, 2] });

                    resultArray[i, j, 0] = color.B;
                    resultArray[i, j, 1] = color.G;
                    resultArray[i, j, 2] = color.R;
                    resultArray[i, j, 3] = 255;
                }
            }
            return resultArray;
        }
    }
}