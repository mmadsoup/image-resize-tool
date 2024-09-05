using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace image_resize_tool
{
    public partial class MainWindow : Window
    {
        private BitmapImage? droppedImage = null;
        private Dictionary<BitmapImage, Vector2> droppedImages = new();
        
        private string fileNamePath = "";
        private Vector2 updatedSize = Vector2.Zero;
        private bool convertToPOT = false;

        System.Windows.Controls.Label? folderDroppedLabel = null;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void FileDropStackPanel_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Get dropped file
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                fileNamePath = Path.GetFileName(files[0]);
                fileNameLabel.Content = fileNamePath;

                // Clear folder images dictionary
                if (root.Children.Contains(folderDroppedLabel))
                {
                    root.Children.Remove(folderDroppedLabel);
                    folderDroppedLabel = null;
                }

                if (droppedImages.Count > 0)
                { 
                    droppedImages.Clear();
                }
                
                // If dropped item is a single image
                if (IsImage(fileNamePath))
                {
                    Uri filePath = new(files[0]);

                    droppedImage = new BitmapImage(filePath);
                    imagePreview.Source = droppedImage;

                    fileDropStackPanel.Background = Brushes.Transparent;
                    originalSizeLabel.Content = $"Original Dimensions: {droppedImage.PixelWidth} x {droppedImage.PixelHeight}";

                    if (convertToPOT)
                    {
                        updatedSize = RoundToPowerOfTwo(new Vector2(droppedImage.PixelWidth, droppedImage.PixelHeight));
                    }
                    else
                    {
                        updatedSize = RoundToMultipleOfFour(new Vector2(droppedImage.PixelWidth, droppedImage.PixelHeight));
                    }

                    updatedSizeLabel.Content = $"Updated Dimensions: {updatedSize.X} x {updatedSize.Y}";
                }
                else
                { // If dropped item is a folder
                    originalSizeLabel.Content = "";
                    updatedSizeLabel.Content = "";

                    imagePreview.Source = new BitmapImage();
                    fileDropStackPanel.Background = Brushes.LightGray;

                    string[] folder = (string[])e.Data.GetData(DataFormats.FileDrop);
                    fileNamePath = folder[0];
                    string[] fileArray = Directory.GetFiles(fileNamePath);
                    fileNameLabel.Content = fileNamePath;

                    foreach (string file in fileArray)
                    {
                        if (IsImage(file))
                        {
                            Uri filePath = new(file);
                            BitmapImage bitmapImage = new(filePath);

                            if (convertToPOT)
                            {
                                droppedImages[bitmapImage] = RoundToPowerOfTwo(new Vector2(bitmapImage.PixelWidth, bitmapImage.PixelHeight));
                            }
                            else
                            {
                                droppedImages[bitmapImage] = RoundToMultipleOfFour(new Vector2(bitmapImage.PixelWidth, bitmapImage.PixelHeight));
                            }
                        }
                    }

                    // If no images were found in the folder, show error
                    if (droppedImages.Count == 0)
                    {
                        fileNameLabel.Content = "";
                        MessageBox.Show("Please drop in a folder with images in it.\nTool does not scan subfolders.", "Failure");
                    }
                    else
                    { // Show that the folder is loaded
                        fileNameLabel.Content = fileNamePath;
                        folderDroppedLabel = new()
                        {
                            VerticalAlignment = VerticalAlignment.Center,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            FontSize = 20,
                            Height = double.NaN,
                            Width = double.NaN,
                            Content = "Folder Loaded"
                        };

                        Grid.SetRow(folderDroppedLabel, 1);
                        root.Children.Add(folderDroppedLabel);
                    }
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {

            SaveFileDialog saveFileDialog = new();
            if (IsImage(fileNamePath))
            {
                if (droppedImage == null)
                {
                    return;
                }

                saveFileDialog.Title = "Save Updated Image...";
                saveFileDialog.Filter = "png(*.png)|*.png";
                saveFileDialog.FileName = $"{fileNamePath.Split(".")[0]}_Resized";

                if (saveFileDialog.ShowDialog() == true)
                {
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    var resizedImage = ResizeImageCanvas(droppedImage, (int)updatedSize.X, (int)updatedSize.Y);
                    encoder.Frames.Add(BitmapFrame.Create(resizedImage));

                    using (Stream stm = File.Create(saveFileDialog.FileName))
                    {
                        encoder.Save(stm);
                    }
                    MessageBox.Show("Image resized successfully!", "Success");
                }
            }
            else
            {
                int counter = 0;
                saveFileDialog.Title = "Save Updated Images...";
                saveFileDialog.Filter = "png(*.png)|*.png";
                saveFileDialog.FileName = "image";

                if (saveFileDialog.ShowDialog() == true)
                {
                    foreach (KeyValuePair<BitmapImage, Vector2> pair in droppedImages)
                    {
                        BitmapImage image = pair.Key;
                        Vector2 size = pair.Value;
                        PngBitmapEncoder encoder = new();
                        var resizedImage = ResizeImageCanvas(image, (int)size.X, (int)size.Y);
                        encoder.Frames.Add(BitmapFrame.Create(resizedImage));

                        using (Stream stm = File.Create($"{saveFileDialog.FileName.Split(".")[0]}_{counter}.png"))
                        {
                            encoder.Save(stm);
                        }
                        counter++;
                    }
                    MessageBox.Show("Images resized successfully!", "Success");
                    droppedImages.Clear();
                    fileNameLabel.Content = "";
                    root.Children.Remove(folderDroppedLabel);
                    folderDroppedLabel = null;
                }
            }
        }
        private void PowerOfTwoCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            convertToPOT = true;

            if (IsImage(fileNamePath))
            {
                updatedSize = RoundToPowerOfTwo(new Vector2(droppedImage.PixelWidth, droppedImage.PixelHeight));
                updatedSizeLabel.Content = $"Updated Dimensions: {updatedSize.X} x {updatedSize.Y}";
            }
            else
            {
                foreach (KeyValuePair<BitmapImage, Vector2> pair in droppedImages)
                {
                    droppedImages[pair.Key] = RoundToPowerOfTwo(new Vector2(pair.Key.PixelWidth, pair.Key.PixelHeight));
                }
            }
        }

        private void PowerOfTwoCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            convertToPOT = false;

            if (IsImage(fileNamePath))
            {
                updatedSize = RoundToMultipleOfFour(new Vector2(droppedImage.PixelWidth, droppedImage.PixelHeight));
                updatedSizeLabel.Content = $"Updated Dimensions: {updatedSize.X} x {updatedSize.Y}";
            }
            else
            {
                foreach (KeyValuePair<BitmapImage, Vector2> pair in droppedImages)
                {
                    droppedImages[pair.Key] = RoundToMultipleOfFour(new Vector2(pair.Key.PixelWidth, pair.Key.PixelHeight));
                }
            }
        }

        #region Image Functions
        private static bool IsImage(string filePath)
        {
            string extension = Path.GetExtension(filePath);
            string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".webp" };
            return imageExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
        }

        private static BitmapSource ResizeCanvas(BitmapSource image, int newWidth, int newHeight)
        {
            DrawingVisual drawingVisual = new();

            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                drawingContext.DrawImage(image, new Rect(0, 0, image.Width, image.Height));
            }

            RenderTargetBitmap resizedBitmap = new(newWidth, newHeight, image.DpiX, image.DpiY, PixelFormats.Default);
            resizedBitmap.Render(drawingVisual);

            return resizedBitmap;
        }
        
        private BitmapSource ResizeImageCanvas(BitmapImage image, int width, int height)
        {
            BitmapSource resizedImage = ResizeCanvas(image, width, height);

            return resizedImage;
        }
        #endregion

        #region Math Functions
        private static Vector2 RoundToMultipleOfFour(Vector2 size)
        {
            return new Vector2(size.X % 4 == 0 ?
                                size.X :
                                size.X + (4 - (size.X % 4)),
                                size.Y % 4 == 0 ?
                                size.Y :
                                size.Y + (4 - (size.Y % 4))
                                );
        }

        private static Vector2 RoundToPowerOfTwo(Vector2 size)
        {
            int newWidth = (int)Math.Pow(2, Math.Ceiling(Math.Log(size.X, 2)));
            int newHeight = (int)Math.Pow(2, Math.Ceiling(Math.Log(size.Y, 2)));

            return new Vector2(newWidth, newHeight);
        }
        #endregion
    }
}