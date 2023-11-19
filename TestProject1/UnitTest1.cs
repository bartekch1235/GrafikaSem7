using Microsoft.VisualStudio.TestTools.UnitTesting;
using grafzad3; // Dodaj odpowiednie using zależnie od organizacji projektu

[TestClass]
public class ImageProcessingTests
{
    [TestMethod]
    public void TestAddRGB()
    {
        // Arrange
        MainWindow mainWindow = new MainWindow();
        byte[] pixels = new byte[] { 100, 100, 100, 200, 200, 200, 50, 50, 50, 0 };
        int initialRedValue = mainWindow.redValue;

        // Act
        mainWindow.redValue = 10;
        byte[] resultPixels = mainWindow.AddRGB(pixels);

        // Assert
        Assert.AreEqual(110, resultPixels[0]);
        Assert.AreEqual(110, resultPixels[1]);
        Assert.AreEqual(110, resultPixels[2]);
        Assert.AreEqual(210, resultPixels[4]);
        Assert.AreEqual(210, resultPixels[5]);
        Assert.AreEqual(210, resultPixels[6]);
        Assert.AreEqual(60, resultPixels[8]);
        Assert.AreEqual(60, resultPixels[9]);
        Assert.AreEqual(60, resultPixels[10]);

        // Cleanup
        mainWindow.redValue = initialRedValue;
    }

    [TestMethod]
    public void TestSubRGB()
    {
        // Arrange
        MainWindow mainWindow = new MainWindow();
        byte[] pixels = new byte[] { 100, 100, 100, 200, 200, 200, 50, 50, 50, 0 };
        int initialRedValue = mainWindow.redValue;

        // Act
        mainWindow.redValue = 10;
        byte[] resultPixels = mainWindow.SubRGB(pixels);

        // Assert
        Assert.AreEqual(90, resultPixels[0]);
        Assert.AreEqual(90, resultPixels[1]);
        Assert.AreEqual(90, resultPixels[2]);
        Assert.AreEqual(190, resultPixels[4]);
        Assert.AreEqual(190, resultPixels[5]);
        Assert.AreEqual(190, resultPixels[6]);
        Assert.AreEqual(40, resultPixels[8]);
        Assert.AreEqual(40, resultPixels[9]);
        Assert.AreEqual(40, resultPixels[10]);

        // Cleanup
        mainWindow.redValue = initialRedValue;
    }

    // Podobne testy dla innych operacji
}