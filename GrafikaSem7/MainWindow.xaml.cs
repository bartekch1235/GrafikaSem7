using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Linq;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace grafzad3
{
    public enum TransformationType
    {
        add, sub, mul, div, bri, gray,
        smooth,
        median
    }
    public partial class MainWindow : Window
    {
        int redValue = 0;
        int blueValue = 0;
        int greenValue = 0;
        int factorValue = 1;

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
            dialog.Filter = "zdjecie (*.png)|*.png";

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

            switch (type)
            {
                case TransformationType.add:
                    pixels = AddRGB(pixels);
                    break;

                case TransformationType.sub:
                    pixels = SubRGB(pixels);
                    break;
                case TransformationType.div:
                    pixels = DivRGB(pixels);
                    break;
                case TransformationType.mul:
                    pixels = MulRGB(pixels);
                    break;
                case TransformationType.bri:
                    pixels = ChangeBrightness(pixels);
                    break;
                case TransformationType.gray:
                    pixels = ChangeToGray(pixels);
                    break;
                case TransformationType.smooth:
                    pixels = SmoothTransformation(pixels, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight);
                    break;
                case TransformationType.median:
                    pixels = MedianTransformation(pixels, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight);
                    break;

            }



            writeableBitmap.WritePixels(new Int32Rect(0, 0, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight), pixels, stride, 0);
            Image.Source = writeableBitmap;

        }

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

                    int pixelCount = 1;//Middle
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
    }
}
