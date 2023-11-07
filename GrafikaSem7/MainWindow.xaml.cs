using Microsoft.Win32;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace grafzad3
{
    public partial class MainWindow : Window
    {

        enum pType { p1, p2 }

        private ConcurrentQueue<Action> eventQueue = new ConcurrentQueue<Action>() { };
        private CancellationTokenSource cancellationTokenSource;



        private async Task ProcessEventsAsync()
        {

            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {

                if (eventQueue.TryDequeue(out Action item))
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        item.Invoke();
                    });
                }
                else
                {
                    await Task.Delay(100); // Oczekiwanie na nowe zdarzenia
                }
            }
        }




        public MainWindow()
        {

            InitializeComponent();
            Init();
            eventQueue.Enqueue(OpenImage);
            

        }
        public async void Init()
        {
            cancellationTokenSource = new CancellationTokenSource();
            await Task.Run(() => ProcessEventsAsync(), cancellationTokenSource.Token);
        }


        #region  zad3
        string GetStringFromFileDialog()
        {
            string fileContent = string.Empty;
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.FileName = "Document";
            dialog.DefaultExt = ".txt";
            //dialog.Filter = "Text documents (*.txt)|*.txt|P1 files (*.pbm)|*.pbm|P2Files (*.pgm)|*.pgm";
            dialog.Filter = "Text documents (*.txt)|*.txt|P1 files (*.pbm)|*.pbm|P2Files (*.pgm)|*.pgm";

            bool? result = dialog.ShowDialog();
            Stream file;

            file = dialog.OpenFile();
            using (StreamReader reader = new StreamReader(file))
            {
                fileContent = reader.ReadToEnd();
            }
            return fileContent;
        }
        void OpenImage()
        {
            pType imageType = pType.p1;

            List<byte> pixelByteArray = new List<byte>();

            int heigh = 0, width = 0;

            string fileContent = GetStringFromFileDialog();

            //MessageBox.Show(fileContent, "File Content at path: ", MessageBoxButton.OK);


            int first3varaibles = 0;//ustawiwanie metadata
            for (int i = 0; i < fileContent.Length; i++)
            {
                if (fileContent[i].Equals('#'))
                    fileContent = DeleteCommentedLine(fileContent, i, out i);

                if (!IsWhiteSign(fileContent[i]))
                {
                    var extracted = ExtractNumber(fileContent, i, out i);

                    if (first3varaibles == 0)
                    {
                        if (extracted.ToLower().Equals("p1"))
                            imageType = pType.p1;
                        if (extracted.ToLower().Equals("p2"))
                            imageType = pType.p2;
                    }
                    else if (first3varaibles == 1)
                    {
                        width = int.Parse(extracted);
                    }
                    else if (first3varaibles == 2)
                    {
                        heigh = int.Parse(extracted);
                    }
                    else
                    {
                        if (imageType == pType.p1)//musialem tak zrobic bo nie wiadomo dlaczego  nie dziala bitmapsource.Create() w przypadku 1 bitow pixeli
                        {
                            if (extracted.Length > 1)
                            {
                                char[] splitted = extracted.ToCharArray();
                                foreach (char s in splitted)
                                {


                                    if (int.Parse(s.ToString()).Equals(0))
                                        pixelByteArray.Add((byte)0);
                                    else if (int.Parse(s.ToString()).Equals(1))
                                        pixelByteArray.Add((byte)255);
                                }
                            }
                            else
                            {
                                if (int.Parse(extracted).Equals(0))
                                    pixelByteArray.Add((byte)0);
                                else if (int.Parse(extracted).Equals(1))
                                    pixelByteArray.Add((byte)255);
                            }

                        }
                        else
                        {
                            pixelByteArray.Add((byte)int.Parse(extracted));
                        }

                    }
                    first3varaibles++;
                }
            }
            GenerateImage(width, heigh, pixelByteArray, imageType);

        }

        void GenerateImage(int width, int heigh, List<Byte> pixels, pType imageType)
        {
            var array = pixels.ToArray();

            BitmapSource bitmapSource = null;

            int rawStride = (width * 8 + 7) / 8;
            bitmapSource = BitmapSource.Create(width, heigh, 300, 300, PixelFormats.Indexed8, BitmapPalettes.Gray256, array, rawStride);

            Image.Source = bitmapSource;

        }

        bool IsWhiteSign(char c)
        {
            if ((c.Equals('\n') || c.Equals(' ') || c.Equals('\t') || c.Equals('\r')))
            {
                return true;
            }
            return false;
        }
        string DeleteCommentedLine(string file, int pos, out int i)
        {
            char[] fileInChar = file.ToCharArray();
            while (!file[pos].Equals('\n'))
            {

                fileInChar[pos] = ' ';

                pos++;
            }
            i = pos;
            return new string(fileInChar);
        }
        string ExtractNumber(string file, int pos, out int i)
        {
            string exctractedNumber = string.Empty;
            while (file.Length > pos && !IsWhiteSign(file[pos]))
            {
                exctractedNumber += file[pos];

                pos++;
            }

            i = pos;
            return exctractedNumber;
        }



        void SaveImageAsP2(BitmapSource bitmapSource)
        {
            int arrayCount = bitmapSource.PixelHeight * bitmapSource.PixelWidth;
            byte[] ar = new byte[arrayCount];
            var stride = (bitmapSource.PixelWidth * 8 + 7) / 8;
            bitmapSource.CopyPixels(ar, stride, 0);


            var imageData = "p2 \n" + bitmapSource.PixelWidth + " " + bitmapSource.PixelHeight + "\n";
            foreach (byte b in ar)
            {
                imageData += b.ToString() + "\n";
            }


            string docPath =
              Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            using (StreamWriter outputFile = new StreamWriter(System.IO.Path.Combine(docPath, "ImageP2.txt")))
            {
                outputFile.WriteLine(imageData);
                MessageBox.Show("File was saved in documents folder!");
            }
        }
        void SaveImageAsP1(BitmapSource bitmapSource)
        {
            int arrayCount = bitmapSource.PixelHeight * bitmapSource.PixelWidth;
            byte[] ar = new byte[arrayCount];
            var stride = (bitmapSource.PixelWidth * 8 + 7) / 8;
            bitmapSource.CopyPixels(ar, stride, 0);


            var imageData = "p1 \n" + bitmapSource.PixelWidth + " " + bitmapSource.PixelHeight + "\n";
            foreach (byte b in ar)
            {
                if (b == 0)
                    imageData += b.ToString() + "\n";
                else
                    imageData += 1 + "\n";
            }


            string docPath =
              Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            using (StreamWriter outputFile = new StreamWriter(System.IO.Path.Combine(docPath, "ImageP2.txt")))
            {
                outputFile.WriteLine(imageData);
                MessageBox.Show("File was saved in documents folder!");
            }
        }



        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            SaveImageAsP1(Image.Source as BitmapSource);
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SaveImageAsP2(Image.Source as BitmapSource);
        }
        #endregion


    }
}
