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

namespace grafzad3
{
    public class Pixel
    {
        public int red=0;
        public int green=0;
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
        expand, equalize,
        binarUser,
        bpt,
        iterative,
        otsu,
        nickleback
    }
    public partial class MainWindow : Window
    {
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
            WriteableBitmap writeableBitmap = new WriteableBitmap(bitmapSource);

            int stride = writeableBitmap.PixelWidth * 4;
            int size = writeableBitmap.PixelHeight * stride;
            byte[] pixels = new byte[size];
            writeableBitmap.CopyPixels(pixels, stride, 0);

            CalculateHistogram(pixels, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight);


            switch (type)
            {

                case TransformationType.expand:
                    LUTblue = CalculateExpandLUT(bluepixels.ToArray());
                    LUTgreen = CalculateExpandLUT(greenpixels.ToArray());
                    LUTred = CalculateExpandLUT(redpixels.ToArray());
                    pixels = ChangeHistogram(pixels, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight);
                    break;
                case TransformationType.equalize:
                    int sizeTmp = writeableBitmap.PixelWidth * writeableBitmap.PixelHeight;
                    LUTblue = CalculateEqualizeLUT(blue.ToArray(), sizeTmp);
                    LUTgreen = CalculateEqualizeLUT(green.ToArray(), sizeTmp);
                    LUTred = CalculateEqualizeLUT(red.ToArray(), sizeTmp);
                    pixels = ChangeHistogram(pixels, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight);
                    break;

                case TransformationType.binarUser:
                    pixels = BinaryByUser(pixels, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight);
                    break;
                case TransformationType.bpt:
                    pixels = BinaryByThreshold(pixels, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight,
                        CalculateTreshlodPosition(CreateAvgHistogram(red, green, blue), prog));
                    break;
                case TransformationType.iterative:
                    pixels = Iterative(pixels, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight, 100);
                    break;
                case TransformationType.otsu:
                    pixels = Otsu(pixels, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight);
                    break;
                case TransformationType.nickleback:
                    pixels = Nickleback(pixels, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight,prog);
                    break;
            }



            writeableBitmap.WritePixels(new Int32Rect(0, 0, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight), pixels, stride, 0);
            Image.Source = writeableBitmap;

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

        byte[] Nickleback(byte[] pixels, int width, int height,int k=1)
        {
            int[] histogram = new int[255];
            histogram = CreateAvgHistogram(red, green, blue);
            

            

            byte[] newPixels = new byte[pixels.Length];
            width *= 4;

            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width; x += 4)
                {
                    List<int> pixelsInRange = GetPixelsInRadius( pixels,  width, height,x,y,4);
                    double avgInRange= pixelsInRange.Average();

                    double SumSquare = 0;
                    foreach(int i in pixelsInRange)
                    {
                        SumSquare += (i - avgInRange) * (i - avgInRange);

                    }

                    double stdDev = Math.Sqrt(SumSquare / pixelsInRange.Count);

                    int threshold = (int)(avgInRange+k*stdDev);

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
        
        List<int> GetPixelsInRadius(byte[] pixels, int width, int height, int x, int y, int radius=1)
        {
            List<int> newPixels = new();
            int center = x + y * width;

            for (int i= -radius;i<=radius;i++)
            {
                for (int j = -radius * 4; j <= radius * 4; j += 4)
                {
                    if (0 < center + j + i * width && center +3+ j + i * width < pixels.Length)
                    {
                        newPixels.Add((pixels[center + j + i * width]+ pixels[center +1+ j + i * width]+ pixels[center +2+ j + i * width]) /3);

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
        private byte[] ChangeHistogram(byte[] pixels, int width, int height)
        {
            byte[] newPixels = new byte[pixels.Length];
            width *= 4;

            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width; x += 4)
                {

                    newPixels[x + y * width] = (byte)Math.Clamp(LUTblue[pixels[x + y * width]], 0, 255);
                    newPixels[x + 1 + y * width] = (byte)Math.Clamp(LUTgreen[pixels[x + 1 + y * width]], 0, 255);
                    newPixels[x + 2 + y * width] = (byte)Math.Clamp(LUTred[pixels[x + 2 + y * width]], 0, 255);
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

        int[] CalculateEqualizeLUT(int[] values, int size)
        {
            //poszukaj wartości minimalnej - czyli pierwszej niezerowej wartosci dystrybuanty
            double minValue = 0;
            for (int i = 0; i < 256; i++)
            {
                if (values[i] != 0)
                {
                    minValue = values[i];
                    break;
                }
            }

            //przygotuj tablice zgodnie ze wzorem
            int[] result = new int[256];
            double sum = 0;
            for (int i = 0; i < 256; i++)
            {
                sum += values[i];
                result[i] = (int)(((sum - minValue) / (size - minValue)) * 255.0);
            }

            return result;
        }
        //cumulativeDistribution[i] = cumulativeDistribution[i - 1] + histogram[i];
        int[] CalculateExpandLUT(byte[] values)
        {
            int minValue = values.Min();

            int maxValue = values.Max();


            int[] result = new int[256];
            double a = 255.0 / (maxValue - minValue);
            //MessageBox.Show(minValue + " " + maxValue + "  " + a);

            for (int i = 0; i < 256; i++)
            {
                result[i] = (int)(a * (i - minValue));
            }

            return result;
        }

        /*
        private byte[] SmoothTransformation(byte[] pixels, int width, int heigh)
        {
            byte[] newPixels = new byte[pixels.Length];
            width *= 4;
            for (int y = 0; y < heigh; y++)
            {
                for (int x = 0; x < width; x += 4)
                {
                    int avgR = 0;
                    int avgG = 0;
                    int avgB = 0;

                    int pixelCount = 1;//Middle
                    avgB = pixels[x + y * width];
                    avgG = pixels[x + 1 + y * width];
                    avgR = pixels[x + 2 + y * width];


                    if (y > 0 && x > 0)//U-L
                    {
                        //MessageBox.Show("U-L", x.ToString() + " " + y.ToString());
                        pixelCount += 1;
                        avgB += pixels[(x - 4) + (y - 1) * width];
                        avgG += pixels[(x - 4) + 1 + (y - 1) * width];
                        avgR += pixels[(x - 4) + 2 + (y - 1) * width];
                    }
                    if (y > 0)//Up
                    {
                        //MessageBox.Show("U", x.ToString() + " " + y.ToString());
                        pixelCount += 1;
                        avgB += pixels[x + (y - 1) * width];
                        avgG += pixels[x + 1 + (y - 1) * width];
                        avgR += pixels[x + 2 + (y - 1) * width];
                    }
                    if (y > 0 && x < width)//U-R
                    {
                        // MessageBox.Show("U-R", x.ToString() + " " + y.ToString());
                        pixelCount += 1;
                        avgB += pixels[(x + 4) + (y - 1) * width];
                        avgG += pixels[(x + 4) + 1 + (y - 1) * width];
                        avgR += pixels[(x + 4) + 2 + (y - 1) * width];
                    }
                    if (x > 0)//L
                    {
                        //MessageBox.Show("L", x.ToString() + " " + y.ToString());
                        pixelCount += 1;
                        avgB += pixels[(x - 4) + (y) * width];
                        avgG += pixels[(x - 4) + 1 + (y) * width];
                        avgR += pixels[(x - 4) + 2 + (y) * width];
                    }
                    if (x < width - 4)//R
                    {
                        //MessageBox.Show("R", x.ToString() + " " + y.ToString());
                        pixelCount += 1;
                        avgB += pixels[(x + 4) + (y) * width];
                        avgG += pixels[(x + 4) + 1 + (y) * width];
                        avgR += pixels[(x + 4) + 2 + (y) * width];
                    }
                    if (y < heigh - 1 && x > 0)//D-L
                    {
                        // MessageBox.Show("D-L", x.ToString() + " " + y.ToString());
                        pixelCount += 1;
                        avgB += pixels[(x - 4) + (y + 1) * width];
                        avgG += pixels[(x - 4) + 1 + (y + 1) * width];
                        avgR += pixels[(x - 4) + 2 + (y + 1) * width];
                    }
                    if (y < heigh - 1)//Down
                    {
                        //MessageBox.Show("D", x.ToString() + " " + y.ToString());
                        pixelCount += 1;
                        avgB += pixels[x + (y + 1) * width];
                        avgG += pixels[x + 1 + (y + 1) * width];
                        avgR += pixels[x + 2 + (y + 1) * width];
                    }
                    if (y < heigh - 1 && x < width - 4)//D-R
                    {
                        //MessageBox.Show("D-R", x.ToString() + " " + y.ToString());
                        pixelCount += 1;
                        avgB += pixels[(x + 4) + (y + 1) * width];
                        avgG += pixels[(x + 4) + 1 + (y + 1) * width];
                        avgR += pixels[(x + 4) + 2 + (y + 1) * width];
                    }


                    // MessageBox.Show(avgB.ToString(),pixelCount.ToString());

                    newPixels[x + y * width] = (byte)(avgB / pixelCount);
                    newPixels[x + 1 + y * width] = (byte)(avgG / pixelCount);
                    newPixels[x + 2 + y * width] = (byte)(avgR / pixelCount);
                }
            }

            return newPixels;
        }
        private byte[] MedianTransformation(byte[] pixels, int width, int heigh)
        {
            byte[] newPixels = new byte[pixels.Length];
            width *= 4;
            for (int y = 0; y < heigh; y++)
            {
                for (int x = 0; x < width; x += 4)
                {
                    List<int> red = new List<int>();
                    List<int> green = new List<int>();
                    List<int> blue = new List<int>();

                    blue.Add(pixels[x + y * width]);
                    green.Add(pixels[x + 1 + y * width]);
                    red.Add(pixels[x + 2 + y * width]);


                    if (y > 0 && x > 0)//U-L
                    {

                        blue.Add(pixels[(x - 4) + (y - 1) * width]);
                        green.Add(pixels[(x - 4) + 1 + (y - 1) * width]);
                        red.Add(pixels[(x - 4) + 2 + (y - 1) * width]);
                    }
                    if (y > 0)//Up
                    {

                        blue.Add(pixels[x + (y - 1) * width]);
                        green.Add(pixels[x + 1 + (y - 1) * width]);
                        red.Add(pixels[x + 2 + (y - 1) * width]);
                    }
                    if (y > 0 && x < width)//U-R
                    {

                        blue.Add(pixels[(x + 4) + (y - 1) * width]);
                        green.Add(pixels[(x + 4) + 1 + (y - 1) * width]);
                        red.Add(pixels[(x + 4) + 2 + (y - 1) * width]);
                    }
                    if (x > 0)//L
                    {

                        blue.Add(pixels[(x - 4) + (y) * width]);
                        green.Add(pixels[(x - 4) + 1 + (y) * width]);
                        red.Add(pixels[(x - 4) + 2 + (y) * width]);
                    }
                    if (x < width - 4)//R
                    {

                        blue.Add(pixels[(x + 4) + (y) * width]);
                        green.Add(pixels[(x + 4) + 1 + (y) * width]);
                        red.Add(pixels[(x + 4) + 2 + (y) * width]);
                    }
                    if (y < heigh - 1 && x > 0)//D-L
                    {

                        blue.Add(pixels[(x - 4) + (y + 1) * width]);
                        green.Add(pixels[(x - 4) + 1 + (y + 1) * width]);
                        red.Add(pixels[(x - 4) + 2 + (y + 1) * width]);
                    }
                    if (y < heigh - 1)//Down
                    {
                        blue.Add(pixels[x + (y + 1) * width]);
                        green.Add(pixels[x + 1 + (y + 1) * width]);
                        red.Add(pixels[x + 2 + (y + 1) * width]);
                    }
                    if (y < heigh - 1 && x < width - 4)//D-R
                    {

                        blue.Add(pixels[(x + 4) + (y + 1) * width]);
                        green.Add(pixels[(x + 4) + 1 + (y + 1) * width]);
                        red.Add(pixels[(x + 4) + 2 + (y + 1) * width]);
                    }


                    blue.Sort();
                    green.Sort();
                    red.Sort();

                    newPixels[x + y * width] = (byte)(blue[blue.Count/2]);
                    newPixels[x + 1 + y * width] = (byte)(green[green.Count/2]);
                    newPixels[x + 2 + y * width] = (byte)(red[red.Count/2]);
                }
            }

            return newPixels;
        }

        private byte[] SobelTransformation(byte[] pixels, int width, int height)
        {
            int[,] sobelX = {{-1,0,1 },{ -2,0,2},{-1,0,1 }};
            int[,] sobelY = {{-1,-2,-1 },{ 0,0,0},{1,2,1 }};
            byte[] newPixels = new byte[pixels.Length];
            width *= 4;
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 4; x < width - 4; x += 4)
                {
                    int gradientX = 0;
                    int gradientY = 0;

                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -4; j <= 4; j += 4)
                        {
                            gradientX += pixels[(x + j) + (y + i) * width] * sobelX[i + 1, (j / 4) + 1];
                            gradientY += pixels[(x + j) + (y + i) * width] * sobelY[i + 1, (j / 4) + 1];
                        }
                    }

                    int gradient = (int)Math.Sqrt(gradientX * gradientX + gradientY * gradientY);

                    newPixels[x + y * width] = (byte)gradient;
                    newPixels[x + 1 + y * width] = (byte)gradient;
                    newPixels[x + 2 + y * width] = (byte)gradient;
                }
            }

            return newPixels;
        }

        private byte[] HighPassTransformation(byte[] pixels, int width, int height)
        {
            int[,] highPassMask = {{-1, -2, -1},
                    {-2, 12, -2},
                    {-1, -2, -1}};
            byte[] newPixels = new byte[pixels.Length];
            width *= 4;
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 4; x < width - 4; x += 4)
                {
                    int resultB = 0;
                    int resultG = 0;
                    int resultR = 0;

                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -4; j <= 4; j += 4)
                        {
                            resultB += pixels[(x + j) + (y + i) * width] * highPassMask[i + 1, (j / 4) + 1];
                            resultG += pixels[(x + j) + 1 + (y + i) * width] * highPassMask[i + 1, (j / 4) + 1];
                            resultR += pixels[(x + j) + 2 + (y + i) * width] * highPassMask[i + 1, (j / 4) + 1];
                        }
                    }

                    resultB = Math.Max(0, Math.Min(255, resultB));
                    resultG = Math.Max(0, Math.Min(255, resultG));
                    resultR = Math.Max(0, Math.Min(255, resultR));

                    newPixels[x + y * width] = (byte)resultB;
                    newPixels[x + 1 + y * width] = (byte)resultG;
                    newPixels[x + 2 + y * width] = (byte)resultR;
                }
            }
            return newPixels;
        }

        byte[] AddRGB(byte[] pixels)
        {
            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i] = (byte)Math.Min(255, pixels[i] + blueValue);
                pixels[i + 1] = (byte)Math.Min(255, pixels[i + 1] + greenValue);
                pixels[i + 2] = (byte)Math.Min(255, pixels[i + 2] + redValue);
            }
            return pixels;
        }
        byte[] SubRGB(byte[] pixels)
        {
            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i] = (byte)Math.Max(0, pixels[i] - blueValue);
                pixels[i + 1] = (byte)Math.Max(0, pixels[i + 1] - greenValue);
                pixels[i + 2] = (byte)Math.Max(0, pixels[i + 2] - redValue);
            }
            return pixels;
        }
        byte[] MulRGB(byte[] pixels)
        {
            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i] = (byte)Math.Min(255, pixels[i] * blueValue);
                pixels[i + 1] = (byte)Math.Min(255, pixels[i + 1] * greenValue);
                pixels[i + 2] = (byte)Math.Min(255, pixels[i + 2] * redValue);
            }
            return pixels;
        }
        byte[] DivRGB(byte[] pixels)
        {
            if (redValue == 0 || blueValue == 0 || greenValue == 0)
            {
                MessageBox.Show("Divison by 0");
            }
            else
            {
                for (int i = 0; i < pixels.Length; i += 4)
                {
                    pixels[i] = (byte)Math.Max(0, pixels[i] / blueValue);
                    pixels[i + 1] = (byte)Math.Max(0, pixels[i + 1] / greenValue);
                    pixels[i + 2] = (byte)Math.Max(0, pixels[i + 2] / redValue);
                }
            }
            return pixels;
        }
        byte[] ChangeBrightness(byte[] pixels)
        {
            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i] = (byte)Math.Clamp(pixels[i] + factorValue, 0, 255);
                pixels[i + 1] = (byte)Math.Clamp(pixels[i + 1] + factorValue, 0, 255);
                pixels[i + 2] = (byte)Math.Clamp(pixels[i + 2] + factorValue, 0, 255);
            }
            return pixels;
        }

        byte[] ChangeToGray(byte[] pixels)
        {
            for (int i = 0; i < pixels.Length; i += 4)
            {
                var avg = (pixels[i] + pixels[i + 1] + pixels[i + 2]) / 3;

                pixels[i] = (byte)avg;
                pixels[i + 1] = (byte)avg;
                pixels[i + 2] = (byte)avg;
            }
            return pixels;
        }


        private void Add_RGB_Click(object sender, RoutedEventArgs e)
        {
            GenerateImage(TransformationType.add);
        }
        private void Sub_RGB_Click(object sender, RoutedEventArgs e)
        {
            GenerateImage(TransformationType.sub);
        }

        private void RedTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int.TryParse(RedTextBox.Text, out redValue);
        }

        private void GreenTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int.TryParse(GreenTextBox.Text, out greenValue);
        }

        private void BlueTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int.TryParse(BlueTextBox.Text, out blueValue);
        }

        private void Multiplication_Click(object sender, RoutedEventArgs e)
        {
            GenerateImage(TransformationType.mul);
        }

        private void Divison_Click(object sender, RoutedEventArgs e)
        {
            GenerateImage(TransformationType.div);
        }

        private void FactorTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int.TryParse(FactorTextBox.Text, out factorValue);
        }

        private void Brightness_Click(object sender, RoutedEventArgs e)
        {
            GenerateImage(TransformationType.bri);
        }

        private void Gray_Click(object sender, RoutedEventArgs e)
        {
            GenerateImage(TransformationType.gray);
        }

        private void Smoothing_Click(object sender, RoutedEventArgs e)
        {
            GenerateImage(TransformationType.smooth);
        }

        private void Median_Click(object sender, RoutedEventArgs e)
        {
            GenerateImage(TransformationType.median);
        }

        private void Sobel_Click(object sender, RoutedEventArgs e)
        {
            GenerateImage(TransformationType.sobel);
        }

        private void HighFrequency_Click(object sender, RoutedEventArgs e)
        {
            GenerateImage(TransformationType.hp);
        }*/

        private void Rozszerz_Histogram_Click(object sender, RoutedEventArgs e)
        {
            GenerateImage(TransformationType.expand);
        }
        private void Wyrownaj_Histogram_Click(object sender, RoutedEventArgs e)
        {
            GenerateImage(TransformationType.equalize);
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            prog = int.Parse(prog_TextBox.Text);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            GenerateImage(TransformationType.binarUser);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            GenerateImage(TransformationType.bpt);
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            SetImage();
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            GenerateImage(TransformationType.iterative);
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            GenerateImage(TransformationType.otsu);
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            GenerateImage(TransformationType.nickleback);
        }

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {

        }
    }
}
