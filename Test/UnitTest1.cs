using Microsoft.VisualStudio.TestTools.UnitTesting;
using grafzad3; // Dodaj odpowiednie using zależnie od organizacji projektu
using System.Threading;

[TestClass]
public class ImageProcessingTests
{
    [TestMethod]
    public void TestAddRGB()
    {
        byte[] pixels = new byte[] { 100, 100, 100, 100, 0, 0, 0, 0, 255, 255, 255, 255 };
        int initialRedValue = MainWindow.redValue;

        // Act
        MainWindow.redValue = 10;
        MainWindow.greenValue = 0;
        MainWindow.blueValue = 10;

        byte[] resultPixels = MainWindow.AddRGB(pixels);

        // Assert
        Assert.AreEqual(110, pixels[0]);
        Assert.AreEqual(100, pixels[1]);
        Assert.AreEqual(110, pixels[2]);

        Assert.AreEqual(10, pixels[4]);
        Assert.AreEqual(0, pixels[5]);
        Assert.AreEqual(10, pixels[6]);

        Assert.AreEqual(255, pixels[8]);
        Assert.AreEqual(255, pixels[9]);
        Assert.AreEqual(255, pixels[10]);

    }

    [TestMethod]
    public void TestSubRGB()
    {
        byte[] pixels = new byte[] { 100, 100, 100, 100 ,0,0,0,0,255,255,255,255 };
        int initialRedValue = MainWindow.redValue;

        // Act
        MainWindow.redValue = 10;
        MainWindow.greenValue = 10;
        MainWindow.blueValue = 0;

        pixels = MainWindow.SubRGB(pixels);

        Assert.AreEqual(100, pixels[0]);
        Assert.AreEqual(90, pixels[1]);
        Assert.AreEqual(90, pixels[2]);

        Assert.AreEqual(0, pixels[4]);
        Assert.AreEqual(0, pixels[5]);
        Assert.AreEqual(0, pixels[6]);

        Assert.AreEqual(255, pixels[8]);
        Assert.AreEqual(245, pixels[9]);
        Assert.AreEqual(245, pixels[10]);

    }    [TestMethod]
    public void TestMedianTransformation()
    {
        byte[] pixels = new byte[] { 100, 100, 100, 100     ,0,0,0,0,       255,255,255,255 ,
                                     100, 100, 100, 100     ,0,0,0,0,       255,255,255,255 ,
                                     100, 100, 100, 100     ,0,0,0,0,       255,255,255,255 ,
                                                                                                };
        int initialRedValue = MainWindow.redValue;



        pixels = MainWindow.MedianTransformation(pixels,3,3);

        
        
        Assert.AreEqual(pixels[0],50);//Ul
        Assert.AreEqual(100, pixels[4]);//U
        Assert.AreEqual(127, pixels[8]);//UR
        Assert.AreEqual(50, pixels[12]);//L
        Assert.AreEqual(100, pixels[16]);//M
        Assert.AreEqual(127, pixels[20]);//R
        Assert.AreEqual(50, pixels[24]);//DL
        Assert.AreEqual(100, pixels[28]);//D
        Assert.AreEqual(127, pixels[32]);//DR

    }


    // Podobne testy dla innych operacji
}