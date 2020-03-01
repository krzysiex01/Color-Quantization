using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI;

namespace Color_Quantization
{

    public class KMeansHelper
    {
        private class KCluster
        {
            public KCluster(Color centre)
            {
                Centre = centre;
            }

            public Color Centre { get; set; }

            private int colorsCount;
            private int rSum;
            private int gSum;
            private int bSum;

            public void Add(Color colour)
            {
                Interlocked.Increment(ref colorsCount);
                Interlocked.Add(ref rSum, colour.R);
                Interlocked.Add(ref gSum, colour.G);
                Interlocked.Add(ref bSum, colour.B);
            }

            public bool RecalculateCentre(double threshold = 0.0d)
            {
                Color updatedCentre;

                if (colorsCount > 0)
                {
                    updatedCentre = Color.FromArgb(0, (byte)Math.Round((float)rSum / colorsCount), (byte)Math.Round((float)gSum / colorsCount), (byte)Math.Round((float)bSum / colorsCount));
                }
                else
                {
                    updatedCentre = Color.FromArgb(0, 0, 0, 0);
                }

                int distance = Distance(Centre, updatedCentre);
                Centre = updatedCentre;

                rSum = 0;
                gSum = 0;
                bSum = 0;
                colorsCount = 0;

                return distance > threshold;
            }

            public int GetDistanceFromClusterCentre(Color colour)
            {
                return Distance(colour, Centre);
            }

            public static int Distance(Color c1, Color c2)
            {
                //This isn't euclidean distance!!! For preformance reasons we skip Sqrt() function, because we use returned values only when comparing them. 
                int distance = (c1.R - c2.R)* (c1.R - c2.R) + (c1.G - c2.G)* (c1.G - c2.G) + (c1.B - c2.B)*(c1.B - c2.B);
                return distance;
            }
        }

        public static IList<Color> GetPalette(int k, IList<Color> imageData, double threshold = 0.0d)
        {
            List<KCluster> clusters = new List<KCluster>(k);
            //Argument threshold is passed as euclidean distance. So we convert it to match our distance function. 
            threshold = threshold * threshold;

            //Set clusters initial location
            Random random = new Random();
            List<int> usedIndexes = new List<int>();
            while (clusters.Count < k)
            {
                int index = random.Next(0, imageData.Count);
                if (!usedIndexes.Contains(index))
                {
                    usedIndexes.Add(index);
                    clusters.Add(new KCluster(imageData[index]));
                }
            }

            bool updated = false;
            do
            {
                updated = false;
                //Iterate through imageData and add each pixel value (color) to closest cluster.
                Parallel.For(0, imageData.Count, i =>
                {
                    int shortestDistance = int.MaxValue;
                    KCluster closestCluster = null;

                    foreach (KCluster cluster in clusters)
                    {
                        int distance = cluster.GetDistanceFromClusterCentre(imageData[i]);
                        if (distance < shortestDistance)
                        {
                            shortestDistance = distance;
                            closestCluster = cluster;
                        }
                    }

                    closestCluster.Add(imageData[i]);
                });

                //Find new clusters centres.
                foreach (KCluster cluster in clusters)
                {
                    if (cluster.RecalculateCentre(threshold))
                    {
                        updated = true;
                    }
                }

            } while (updated == true);

            return clusters.Select(c => c.Centre).ToList();
        }
    }

}