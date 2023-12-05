using Microsoft.Win32;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.Serialization;
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
        bool bezier = true;
        private List<Point> controlPoints = new List<Point>();
        private Polyline bezierCurve = new Polyline();
        private int draggedPointIndex = -1;

        private List<Point> polygonPoints = new List<Point>();
        private Polygon currentPolygon;
        private Point startPoint;
        private TransformType currentTransform = TransformType.None;

        public enum TransformType
        {
            None,
            Translate,
            Rotate,
            Scale
        }


        public MainWindow()
        {
            InitializeComponent();
            canvas.Children.Add(bezierCurve);

        }




        #region save
        private void SaveFiguresToFile(string fileName)
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                DataContractSerializer serializer = new DataContractSerializer(typeof(List<List<Point>>));
                serializer.WriteObject(fs, GetFigures());
            }
        }

        private void LoadFiguresFromFile(string fileName)
        {
            if (File.Exists(fileName))
            {
                using (FileStream fs = new FileStream(fileName, FileMode.Open))
                {
                    DataContractSerializer serializer = new DataContractSerializer(typeof(List<List<Point>>));
                    List<List<Point>> loadedFigures = (List<List<Point>>)serializer.ReadObject(fs);

                    controlPoints.Clear();
                    bezierCurve.Points.Clear();
                    DrawControlPoints();

                    polygonPoints.Clear();

                    foreach (List<Point> figurePoints in loadedFigures)
                    {
                        polygonPoints = figurePoints;
                        DrawPolygon();
                    }
                }
            }
        }

        private List<List<Point>> GetFigures()
        {
            List<List<Point>> figures = new List<List<Point>>();
            foreach (UIElement element in canvas.Children)
            {
                if (element is Polygon polygon)
                {
                    List<Point> figurePoints = new List<Point>(polygon.Points);
                    figures.Add(figurePoints);
                }
            }
            return figures;
        }
        #endregion

        #region bezier
        private void DrawBezierCurve()
        {
            if (controlPoints.Count < 2) return;

            // Aktualizuj krzywą Béziera
            bezierCurve.Points.Clear();
            for (double t = 0; t <= 1; t += 0.01)
            {
                Point point = CalculateBezierPoint(t, controlPoints);
                bezierCurve.Points.Add(point);
            }

            // Aktualizuj punkty kontrolne na ekranie
            DrawControlPoints();

            // Odśwież widok
            bezierCurve.Stroke = Brushes.Black;
            bezierCurve.StrokeThickness = 2;
        }

        private void DrawControlPoints()
        {
            canvas.Children.Clear();
            canvas.Children.Add(bezierCurve);

            foreach (Point point in controlPoints)
            {
                Ellipse ellipse = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = Brushes.Red,
                    Margin = new Thickness(point.X - 5, point.Y - 5, 0, 0)
                };

                canvas.Children.Add(ellipse);
            }
        }

        private Point CalculateBezierPoint(double t, List<Point> points)
        {
            int n = points.Count - 1;
            double x = 0, y = 0;

            for (int i = 0; i <= n; i++)
            {
                double factor = BinomialCoefficient(n, i) * Math.Pow(1 - t, n - i) * Math.Pow(t, i);
                x += factor * points[i].X;
                y += factor * points[i].Y;
            }

            return new Point(x, y);
        }

        private int BinomialCoefficient(int n, int k)
        {
            int result = 1;

            for (int i = 1; i <= k; i++)
            {
                result = result * (n - i + 1) / i;
            }

            return result;
        }
        #endregion

        #region polygons
        private void DrawPolygon()
        {
            if (currentPolygon != null)
            {
                canvas.Children.Remove(currentPolygon);
            }

            currentPolygon = new Polygon
            {
                Points = new PointCollection(polygonPoints),
                Stroke = Brushes.Black,
                StrokeThickness = 2,
                Fill = Brushes.LightBlue
            };

            canvas.Children.Add(currentPolygon);
        }

        private void Translate(double deltaX, double deltaY)
        {
            if (currentPolygon != null)
            {
                for (int i = 0; i < polygonPoints.Count; i++)
                {
                    polygonPoints[i] = new Point(polygonPoints[i].X + deltaX, polygonPoints[i].Y + deltaY);
                }

                DrawPolygon();
            }
        }

        private void Rotate(double angle)
        {
            if (currentPolygon != null)
            {
                Point center = CalculatePolygonCenter();
                RotateTransform rotateTransform = new RotateTransform(angle, center.X, center.Y);

                for (int i = 0; i < polygonPoints.Count; i++)
                {
                    polygonPoints[i] = rotateTransform.Transform(polygonPoints[i]);
                }

                DrawPolygon();
            }
        }

        private void Scale(double scaleFactor)
        {
            if (currentPolygon != null)
            {
                Point center = CalculatePolygonCenter();
                ScaleTransform scaleTransform = new ScaleTransform(scaleFactor, scaleFactor, center.X, center.Y);

                for (int i = 0; i < polygonPoints.Count; i++)
                {
                    polygonPoints[i] = scaleTransform.Transform(polygonPoints[i]);
                }

                DrawPolygon();
            }
        }

        private Point CalculatePolygonCenter()
        {
            double sumX = 0;
            double sumY = 0;

            foreach (Point point in polygonPoints)
            {
                sumX += point.X;
                sumY += point.Y;
            }

            double centerX = sumX / polygonPoints.Count;
            double centerY = sumY / polygonPoints.Count;

            return new Point(centerX, centerY);
        }

        #endregion 

        private void canvas_MouseLeftButtonDown_1(object sender, MouseButtonEventArgs e)
        {
            Point mousePosition = e.GetPosition(canvas);
            if (bezier)
            {
                

                for (int i = 0; i < controlPoints.Count; i++)
                {
                    if (Math.Abs(mousePosition.X - controlPoints[i].X) < 10 &&
                        Math.Abs(mousePosition.Y - controlPoints[i].Y) < 10)
                    {
                        draggedPointIndex = i;
                        return;
                    }
                }
                controlPoints.Add(mousePosition);
                DrawBezierCurve();
            }else
            {
                if (currentTransform == TransformType.None)
                {
                    // Rysowanie nowego wielokąta
                    polygonPoints.Add(mousePosition);
                    DrawPolygon();
                }
                else
                {
                    // Obsługa przekształceń (przesunięcie, obrót, skalowanie)
                    startPoint = mousePosition;
                }
            }

        }
        private void canvas_MouseMove_1(object sender, MouseEventArgs e)
        {
            bool mouseIsDown = System.Windows.Input.Mouse.LeftButton == MouseButtonState.Pressed;
            if (bezier)
            {
                
                if (mouseIsDown && draggedPointIndex != -1)
                {
                    debug.Content = "pisze bezier";
                    Point mousePosition = e.GetPosition(canvas);
                    controlPoints[draggedPointIndex] = mousePosition;
                    DrawBezierCurve();
                }
            }else
            {
                if (mouseIsDown )
                {
                    debug.Content = "pisze polygon";
                    Point currentPoint = e.GetPosition(canvas);

                    switch (currentTransform)
                    {
                        case TransformType.Translate:
                            Translate(currentPoint.X - startPoint.X, currentPoint.Y - startPoint.Y);
                            startPoint = currentPoint;
                            break;
                        case TransformType.Rotate:
                            double angle = currentPoint.Y - startPoint.Y + currentPoint.X - startPoint.X ;
                            Rotate(angle);
                            startPoint = currentPoint;
                            break;
                        case TransformType.Scale:
                            double scaleFactor = currentPoint.Y - startPoint.Y + currentPoint.X - startPoint.X;
                            if (scaleFactor > 0)
                                scaleFactor = 0.99;
                            else
                                scaleFactor = 1.01;

                            Scale(scaleFactor);
                            startPoint = currentPoint;
                            break;
                    }
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            controlPoints.Add(new Point(50, 200));
            controlPoints.Add(new Point(150, 50));
            controlPoints.Add(new Point(250, 200));

            DrawBezierCurve();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            controlPoints.Clear();
            bezierCurve.Points.Clear();
            DrawControlPoints();

            polygonPoints.Clear();
        }



        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            bezier = !bezier;
        }

        private void typeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            currentTransform = (TransformType)( typeComboBox.SelectedIndex);
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            LoadFiguresFromFile("figures.xml");
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            SaveFiguresToFile("figures.xml");
        }
    }
}

