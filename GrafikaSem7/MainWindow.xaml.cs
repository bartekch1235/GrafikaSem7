using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Point = System.Drawing.Point;

namespace grafzad3
{
    public class Pixel
    {
        public int red = 0;
        public int green = 0;
        public int blue = 0;
        public int avg = 0;
        public Pixel(int red, int green, int blue)
        {
            this.red = red;
            this.green = green;
            this.blue = blue;
            this.avg = (red + blue + green) / 3;
        }
    }

    public enum TransformationType
    {
        findColor,
        findGreen
    }
    public partial class MainWindow : Window
    {
        int findRed = 0;
        int findGreen = 0;
        int findBlue = 0;

        int tolerance = 50;
        double prct = 0.03;

        WriteableBitmap writeableBitmap;
        int stride;
        int prog = 0;
        int redValue = 0;
        int blueValue = 0;
        int greenValue = 0;
        int factorValue = 1;

        private int[] red = null;
        private int[] green = null;
        private int[] blue = null;
        List<byte> redpixels = new();
        List<byte> bluepixels = new();
        List<byte> greenpixels = new();

        int[] LUTblue;
        int[] LUTgreen;
        int[] LUTred;
        public MainWindow()
        {
            InitializeComponent();
            SetImage();
        }



        void SetImage()
        {
            string fileContent = string.Empty;
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.FileName = "Document";
            dialog.DefaultExt = ".png";
            dialog.Filter = "zdjecie (*.png,*.jpg)|*.png;*.jpg";

            bool? result = dialog.ShowDialog();

            Image.Source = new BitmapImage(new Uri(dialog.FileName));

        }


        void GenerateImage(TransformationType type)
        {
            BitmapSource bitmapSource = (BitmapSource)Image.Source;
            writeableBitmap = new WriteableBitmap(bitmapSource);

            stride = writeableBitmap.PixelWidth * 4;
            int size = writeableBitmap.PixelHeight * stride;
            byte[] pixels = new byte[size];
            writeableBitmap.CopyPixels(pixels, stride, 0);

            CalculateHistogram(pixels, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight);


            switch (type)
            {


                case TransformationType.findColor:
                    pixels = BinaryByColor(pixels, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight);
                    break;
                case TransformationType.findGreen:
                    pixels = BinaryByGreen(pixels, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight);
                    break;
            }



            writeableBitmap.WritePixels(new Int32Rect(0, 0, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight), pixels, stride, 0);
            Image.Source = writeableBitmap;

        }
        byte[] BinaryByGreen(byte[] pixels, int width, int height)
        {
            byte[] newPixels = new byte[pixels.Length];
            width *= 4;

            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width; x += 4)
                {
                    int b = Math.Abs(pixels[x + y * width]);
                    int g = Math.Abs(pixels[x + 1 + y * width]);
                    int r = Math.Abs(pixels[x + 2 + y * width]);

                    

                    int rDif = g - r;
                    int bDif = g - b;

                    if (g > r && g > b && g > tolerance && rDif > prct * g && bDif > prct * g)
                    {
                        //MessageBox.Show(g+" "+b + " "+ r);
                        newPixels[x + y * width] = 255;
                        newPixels[x + 1 + y * width] = 255;
                        newPixels[x + 2 + y * width] = 255;

                    }
                    else
                    {
                        newPixels[x + y * width] = 0;
                        newPixels[x + 1 + y * width] = 0;
                        newPixels[x + 2 + y * width] = 0;
                    }

                    newPixels[x + 3 + y * width] = 255;
                }
            }
            // return newPixels;
            bool[] isProcesed = new bool[pixels.Length];
            List<(int x, int y)> pixelPoints = new();
            List<(int x, int y)> maxPoints = new();
            int rec = 0;

            int[] listNr = new int[pixels.Length];
            for (int i = 0; i < listNr.Length; i++)
            {
                listNr[i] = -1;
            }
            List<List<(int x, int y)>> listOfPoints = new();


            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width; x += 4)
                {
                    if (newPixels[x + width * y] == 255)
                    {
                        int lowestNr = -1;
                        if (x > 0)
                        {
                            lowestNr = listNr[x - 4 + y * width];
                        }
                        if (y > 0)
                        {
                            if (lowestNr == -1)
                            {
                                lowestNr = listNr[x + (y - 1) * width];
                            }
                            else
                            {
                                if (listNr[x - 4 + y * width] != listNr[x + (y - 1) * width] && listNr[x + (y - 1) * width] != -1)
                                    Merge(listNr[x - 4 + y * width], listNr[x + (y - 1) * width]);
                            }
                        }

                        if (lowestNr == -1)
                        {
                            lowestNr = listOfPoints.Count;
                            listNr[x + y * width] = listOfPoints.Count;
                            List<(int x, int y)> tmp = new();
                            tmp.Add((x, y));
                            listOfPoints.Add(tmp);
                        }
                        else
                        {
                            listNr[x + y * width] = lowestNr;
                            listOfPoints[lowestNr].Add((x, y));
                        }

                    }
                }
            }
            int pos = -1;
            int max = -1;
            for (int i = 0; i < listOfPoints.Count; i++)
            {
                if (listOfPoints[i].Count > max)
                {
                    pos = i;
                    max = listOfPoints[i].Count;
                }

            }
            var sum = 0;
            foreach(List<(int x, int y)> point in listOfPoints)
            {
                sum += point.Count;
            }

            MessageBox.Show( Math.Round((double)sum / (double)pixels.Length*4 * 100)+"% terenu to tereny zielone");

            foreach ((int x, int y) point in listOfPoints[pos])
            {
                newPixels[point.x  + point.y * width] = 0;
                newPixels[point.x + 1 + point.y * width] = 127;
                newPixels[point.x + 2 + point.y * width] = 0;
            }

            return newPixels;
            void CheckNearby(int x, int y)
            {
                if (isProcesed[x + y * width])
                    return;

                isProcesed[x + y * width] = true;
                if (newPixels[x + y * width] == 0)
                {
                    pixelPoints.Add((x, y));
                }
                else
                {
                    return;
                }
                rec++;
                if (x > 0 && false == isProcesed[(x - 4) + y * width])
                {
                    CheckNearby(x - 4, y);
                }
                if (y > 0 && false == isProcesed[x + (y - 1) * width])
                {
                    CheckNearby(x, y - 1);
                }
                if (x + 4 + y * width < pixels.Length && false == isProcesed[(x + 4) + y * width])
                {
                    CheckNearby(x + 4, y);
                }
                if (x + (y + 1) * width < pixels.Length && false == isProcesed[x + (y + 1) * width])
                {
                    CheckNearby(x, y + 1);
                }



            }

            void Merge(int l1, int l2)
            {
                for (int i = 0; i < listOfPoints[l2].Count; i++)
                {
                    listOfPoints[l1].Add(listOfPoints[l2][i]);
                    listNr[listOfPoints[l2][i].x + listOfPoints[l2][i].y * width] = l1;
                }
                listOfPoints[l2].Clear();
            }
        }
        byte[] BinaryByColor(byte[] pixels, int width, int height)
        {
            byte[] newPixels = new byte[pixels.Length];
            width *= 4;

            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width; x += 4)
                {
                    int rDif = Math.Abs(pixels[x + y * width] - findRed);
                    int gDif = Math.Abs(pixels[x + 1 + y * width] - findGreen);
                    int bDif = Math.Abs(pixels[x + 2 + y * width] - findBlue);

                    var a = rDif + gDif + bDif;
                    if (rDif + gDif + bDif < tolerance)
                    {
                        newPixels[x + y * width] = 0;
                        newPixels[x + 1 + y * width] = 0;
                        newPixels[x + 2 + y * width] = 0;
                    }
                    else
                    {
                        newPixels[x + y * width] = 255;
                        newPixels[x + 1 + y * width] = 255;
                        newPixels[x + 2 + y * width] = 255;
                    }

                    newPixels[x + 3 + y * width] = 255;
                }
            }

            bool[] isProcesed = new bool[pixels.Length / 4];
            /*
                        for (int y = 0; y < height - 1; y++)
                        {
                            for (int x = 0; x < width; x += 4)
                            {
                                if (isProcesed[x + y * width])
                                { 
                                    newPixels[x + y * width] = 0;
                                newPixels[x + 1 + y * width] = 0;
                                newPixels[x + 2 + y * width] = 0;
                                newPixels[x + 3 + y * width] = 255;
                                }
                            }
                        }*/


            return newPixels;
        }
        byte[] AndArrayOperation(byte[] pixels, byte[] pixels2, int width, int height)
        {
            byte[] newPixels = new byte[pixels.Length];
            width *= 4;

            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width; x += 4)
                {
                    if (pixels[x + y * width] != pixels2[x + y * width])
                    {
                        newPixels[x + y * width] = 255;
                        newPixels[x + 1 + y * width] = 255;
                        newPixels[x + 2 + y * width] = 255;
                        newPixels[x + 3 + y * width] = 255;
                    }
                    else
                    {
                        newPixels[x + y * width] = 0;
                        newPixels[x + 1 + y * width] = 0;
                        newPixels[x + 2 + y * width] = 0;
                        newPixels[x + 3 + y * width] = 255;
                    }
                }
            }

            return newPixels;
        }
        byte[] InvertArray(byte[] pixels, int width, int height)
        {
            byte[] newPixels = new byte[pixels.Length];
            width *= 4;

            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width; x += 4)
                {
                    newPixels[x + y * width] = pixels[x + 0 + y * width] == 0 ? (byte)255 : (byte)0;
                    newPixels[x + 1 + y * width] = pixels[x + 1 + y * width] == 0 ? (byte)255 : (byte)0;
                    newPixels[x + 2 + y * width] = pixels[x + 2 + y * width] == 0 ? (byte)255 : (byte)0;
                    newPixels[x + 3 + y * width] = 255;
                }
            }

            return newPixels;
        }

        byte[] EresionDylatation(byte[] pixels, int width, int height, int blackOrWhite = 0)
        {

            byte[] newPixels = new byte[pixels.Length];
            width *= 4;
            newPixels[3] = 255;

            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width; x += 4)
                {
                    newPixels[x + y * width] = pixels[x + y * width];
                    newPixels[x + 1 + y * width] = pixels[x + y * width];
                    newPixels[x + 2 + y * width] = pixels[x + y * width];
                }
            }

            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width; x += 4)
                {
                    int radius = 1;
                    int center = x + y * width;

                    for (int i = -radius; i <= radius; i++)
                    {
                        for (int j = -radius * 4; j <= radius * 4; j += 4)
                        {
                            if (0 < center + j + i * width && center + 3 + j + i * width < pixels.Length)
                            {


                                if (pixels[center] == blackOrWhite)
                                {
                                    newPixels[center + j + i * width] = (byte)blackOrWhite;
                                    newPixels[center + 1 + j + i * width] = (byte)blackOrWhite;
                                    newPixels[center + 2 + j + i * width] = (byte)blackOrWhite;

                                }

                            }
                        }
                    }
                }
            }

            return newPixels;



        }
        byte[] Otsu(byte[] pixels, int width, int height)
        {
            int[] histogram = new int[255];
            histogram = CreateAvgHistogram(red, green, blue);
            double[] normalizedHistogram = Array.ConvertAll(histogram, x => (double)x / (width * height));

            double maxVariance = double.MinValue;

            int threshold = 0;

            for (int t = 0; t < 256; t++)
            {
                double w0 = 0;
                double w1 = 0;

                double mu0 = 0;
                double mu1 = 0;

                for (int i = 0; i < t; i++)
                {
                    w0 += normalizedHistogram[i];
                    mu0 += i * normalizedHistogram[i];
                }

                for (int i = t; i < 256; i++)
                {
                    w1 += normalizedHistogram[i];
                    mu1 += i * normalizedHistogram[i];
                }

                if (w0 == 0 || w1 == 0)
                    continue;

                double variance = w0 * w1 * Math.Pow((mu0 / w0 - mu1 / w1), 2);


                if (variance > maxVariance)
                {
                    maxVariance = variance;
                    threshold = t;
                }

            }


            byte[] newPixels = new byte[pixels.Length];
            width *= 4;

            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width; x += 4)
                {
                    var avg = (pixels[x + y * width] + pixels[x + 1 + y * width] + pixels[x + 2 + y * width]) / 3;

                    if (avg < threshold)
                    {
                        newPixels[x + y * width] = 0;
                        newPixels[x + 1 + y * width] = 0;
                        newPixels[x + 2 + y * width] = 0;
                    }
                    else
                    {
                        newPixels[x + y * width] = 255;
                        newPixels[x + 1 + y * width] = 255;
                        newPixels[x + 2 + y * width] = 255;
                    }
                    newPixels[x + 3 + y * width] = 255;
                }
            }

            return newPixels;
        }

        byte[] Nickleback(byte[] pixels, int width, int height, int k = 1)
        {
            int[] histogram = new int[255];
            histogram = CreateAvgHistogram(red, green, blue);




            byte[] newPixels = new byte[pixels.Length];
            width *= 4;

            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width; x += 4)
                {
                    List<int> pixelsInRange = GetPixelsInRadius(pixels, width, height, x, y, 4);
                    double avgInRange = pixelsInRange.Average();

                    double SumSquare = 0;
                    foreach (int i in pixelsInRange)
                    {
                        SumSquare += (i - avgInRange) * (i - avgInRange);

                    }

                    double stdDev = Math.Sqrt(SumSquare / pixelsInRange.Count);

                    int threshold = (int)(avgInRange + k * stdDev);

                    var avg = (pixels[x + y * width] + pixels[x + 1 + y * width] + pixels[x + 2 + y * width]) / 3;

                    if (avg < threshold)
                    {
                        newPixels[x + y * width] = 0;
                        newPixels[x + 1 + y * width] = 0;
                        newPixels[x + 2 + y * width] = 0;
                    }
                    else
                    {
                        newPixels[x + y * width] = 255;
                        newPixels[x + 1 + y * width] = 255;
                        newPixels[x + 2 + y * width] = 255;
                    }
                    newPixels[x + 3 + y * width] = 255;
                }
            }

            return newPixels;
        }

        List<int> GetPixelsInRadius(byte[] pixels, int width, int height, int x, int y, int radius = 1)
        {
            List<int> newPixels = new();
            int center = x + y * width;

            for (int i = -radius; i <= radius; i++)
            {
                for (int j = -radius * 4; j <= radius * 4; j += 4)
                {
                    if (0 < center + j + i * width && center + 3 + j + i * width < pixels.Length)
                    {
                        newPixels.Add((pixels[center + j + i * width] + pixels[center + 1 + j + i * width] + pixels[center + 2 + j + i * width]) / 3);

                    }
                }
            }



            return newPixels;
        }

        byte[] Iterative(byte[] pixels, int width, int height, int maxIter)
        {
            float tolerance = 1;

            int[] histogram = new int[255];
            histogram = CreateAvgHistogram(red, green, blue);

            int threshold = CalculateTreshlodPosition(histogram, 50);

            for (int i = 0; i < maxIter; i++)
            {
                int[] classD = new int[256];
                int[] classU = new int[256];

                for (int j = 0; j < histogram.Length; j++)
                {
                    if (j < threshold)
                        classD[j] = histogram[j];
                    else
                        classU[j] = histogram[j];
                }
                var avgD = CalculateTreshlodPosition(classD, 50);
                var avgU = CalculateTreshlodPosition(classU, 50);

                var newTreshold = (avgD + avgU) / 2;

                if (Math.Abs(newTreshold - threshold) < tolerance)
                    break;

                threshold = newTreshold;
            }
            MessageBox.Show(threshold.ToString());

            byte[] newPixels = new byte[pixels.Length];
            width *= 4;

            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width; x += 4)
                {
                    var avg = (pixels[x + y * width] + pixels[x + 1 + y * width] + pixels[x + 2 + y * width]) / 3;

                    if (avg < threshold)
                    {
                        newPixels[x + y * width] = 0;
                        newPixels[x + 1 + y * width] = 0;
                        newPixels[x + 2 + y * width] = 0;
                    }
                    else
                    {
                        newPixels[x + y * width] = 255;
                        newPixels[x + 1 + y * width] = 255;
                        newPixels[x + 2 + y * width] = 255;
                    }
                    newPixels[x + 3 + y * width] = 255;
                }
            }

            return newPixels;
        }
        int CalculateTreshlodPosition(int[] histogram, float procent)
        {
            float sum = 0;
            foreach (int i in histogram)
            {
                sum += i;
            }

            float medianNumber = sum * (procent / 100);
            float sum2 = 0;
            for (int i = 0; i < histogram.Length; i++)
            {
                if (medianNumber < sum2)
                {
                    //MessageBox.Show(sum2+" "+sum +" "+ sum2/sum + "  "+ i);
                    return i;
                }
                sum2 += histogram[i];
            }


            return -1;
        }
        int[] CreateAvgHistogram(int[] r, int[] g, int[] b)
        {
            int[] avgHistogram = new int[r.Length];
            for (int i = 0; i < avgHistogram.Length; i++)
            {
                avgHistogram[i] = (r[i] + b[i] + g[i]) / 3;
            }
            return avgHistogram;
        }

        private byte[] BinaryByThreshold(byte[] pixels, int width, int height, int treshold)
        {
            byte[] newPixels = new byte[pixels.Length];
            width *= 4;

            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width; x += 4)
                {
                    var avg = (pixels[x + y * width] + pixels[x + 1 + y * width] + pixels[x + 2 + y * width]) / 3;

                    if (avg < treshold)
                    {
                        newPixels[x + y * width] = 0;
                        newPixels[x + 1 + y * width] = 0;
                        newPixels[x + 2 + y * width] = 0;
                    }
                    else
                    {
                        newPixels[x + y * width] = 255;
                        newPixels[x + 1 + y * width] = 255;
                        newPixels[x + 2 + y * width] = 255;
                    }
                    newPixels[x + 3 + y * width] = 255;
                }
            }

            return newPixels;
        }
        private byte[] BinaryByUser(byte[] pixels, int width, int height)
        {
            byte[] newPixels = new byte[pixels.Length];
            width *= 4;

            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width; x += 4)
                {
                    var avg = (pixels[x + y * width] + pixels[x + 1 + y * width] + pixels[x + 2 + y * width]) / 3;

                    if (avg < prog)
                    {
                        newPixels[x + y * width] = 0;
                        newPixels[x + 1 + y * width] = 0;
                        newPixels[x + 2 + y * width] = 0;
                    }
                    else
                    {
                        newPixels[x + y * width] = 255;
                        newPixels[x + 1 + y * width] = 255;
                        newPixels[x + 2 + y * width] = 255;
                    }
                    newPixels[x + 3 + y * width] = 255;
                }
            }

            return newPixels;
        }


        void CalculateHistogram(byte[] pixels, int width, int heigh)
        {
            red = new int[256];
            green = new int[256];
            blue = new int[256];
            width = width * 4;
            for (int y = 0; y < heigh; y++)
            {
                for (int x = 0; x < width; x += 4)
                {
                    int B = pixels[x + y * width];
                    int G = pixels[x + 1 + y * width];
                    int R = pixels[x + 2 + y * width];

                    redpixels.Add(pixels[x + 2 + y * width]);
                    greenpixels.Add(pixels[x + 1 + y * width]);
                    bluepixels.Add(pixels[x + y * width]);


                    red[R]++;
                    green[G]++;
                    blue[B]++;
                }
            }
        }



        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            SetImage();
        }



        private void TextBox_TextChanged_1(object sender, TextChangedEventArgs e)
        {
            findRed = int.Parse(redTB.Text);
        }

        private void greenTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            findGreen = int.Parse(greenTB.Text);
        }

        private void blueTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            findBlue = int.Parse(blueTB.Text);
        }

        private void Button_Click_12(object sender, RoutedEventArgs e)
        {
            GenerateImage(TransformationType.findColor);
        }

        private void toleranceTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            tolerance = int.Parse(toleranceTB.Text);
        }

        private void Button_Click_13(object sender, RoutedEventArgs e)
        {
            GenerateImage(TransformationType.findGreen);
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            prct =Double.Parse(prctTB.Text)/100;
        }
    }
}
