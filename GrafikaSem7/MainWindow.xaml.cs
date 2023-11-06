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
        add, sub, mul, div , bri, gray
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

            }



            writeableBitmap.WritePixels(new Int32Rect(0, 0, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight), pixels, stride, 0);
            Image.Source = writeableBitmap;

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
            if (redValue == 0|| blueValue == 0 || greenValue == 0 )
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
                pixels[i] = (byte)Math.Clamp( pixels[i] + factorValue,0,255);
                pixels[i + 1] = (byte)Math.Clamp(pixels[i+1] + factorValue, 0, 255);
                pixels[i + 2] = (byte)Math.Clamp(pixels[i+2] + factorValue, 0, 255);
            }
            return pixels;
        }

        byte[] ChangeToGray(byte[] pixels)
        {
            for (int i = 0; i < pixels.Length; i += 4)
            {
                var avg= (pixels[i]+ pixels[i+1]+ pixels[i+2])/ 3;

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
    }
}
