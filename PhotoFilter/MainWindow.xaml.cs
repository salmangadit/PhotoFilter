using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Cache;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PhotoFilter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<string> _images = null;
        List<string> _selection;
        List<string> _deleted;
        List<BitmapImage> _cache;

        int currentPointer = 0;
        const int PAGE_BY = 50;

        bool currentSelectionState = false;

        private const string SEL = "Images selected: {0}";
        private const string LEFT = "Image number: {0}/{1}";
        private const string SELECTED_FOLDER = "_selected";
        private const string DELETED_FOLDER = "_deleted";
        private const string SELECTED_FILES = "_selectedImages.txt";

        private string SELECTED_FOLDER_PATH;
        private string DELETED_FOLDER_PATH;
        private string SELECTED_FILE_PATH;

        public MainWindow()
        {
            SELECTED_FOLDER_PATH = Environment.CurrentDirectory + "\\" + SELECTED_FOLDER;
            DELETED_FOLDER_PATH = Environment.CurrentDirectory + "\\" + DELETED_FOLDER;
            SELECTED_FILE_PATH = SELECTED_FOLDER_PATH + "\\" + SELECTED_FILES;

            ExtractImages();
            CreateSubDirectoryAndFile();
            currentSelectionState = _selection.Any(a => a == _images[currentPointer]);
            InitializeComponent();
            RefreshAll();
        }

        private void ExtractImages()
        {
            _images = Directory
                .EnumerateFiles(Environment.CurrentDirectory, "*.*", SearchOption.TopDirectoryOnly)
                .Where(s => s.ToLower().EndsWith(".png") || s.ToLower().EndsWith(".jpg"))
                .ToList();

            int maxlen = _images.Max(x => System.IO.Path.GetFileNameWithoutExtension(x).Length);
            _images = _images.OrderBy(x => System.IO.Path.GetFileNameWithoutExtension(x).PadLeft(maxlen, '0')).ToList();

            foreach (var )

            _selection = new List<string>();
            _deleted = new List<string>();
        }

        private void CreateSubDirectoryAndFile()
        {
            if (!Directory.Exists(SELECTED_FOLDER_PATH))
            {
                Directory.CreateDirectory(SELECTED_FOLDER_PATH);
            }

            if (!Directory.Exists(DELETED_FOLDER_PATH))
            {
                Directory.CreateDirectory(DELETED_FOLDER_PATH);
            }

            if (!File.Exists(SELECTED_FILE_PATH))
            {
                File.Create(SELECTED_FILE_PATH);
            }
            else
            {
                string[] allSelectedImages = File.ReadAllLines(SELECTED_FILE_PATH);
                foreach (string image in allSelectedImages)
                {
                    _selection.Add(Environment.CurrentDirectory + "\\" + image);
                }
            }
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            Previous();
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            Next();
        }

        private void btnSelect_Click(object sender, RoutedEventArgs e)
        {
            ToggleSelectImage();
        }

        private void ToggleSelectImage()
        {
            if (_selection.Any(a => a == _images[currentPointer]))
            {
                // Has been selected, unselect it
                _selection.Remove(_images[currentPointer]);
            }
            else
            {
                // Has not been selected, select it
                _selection.Add(_images[currentPointer]);
            }

            RefreshMetaData();
        }

        private void Next()
        {
            WriteIfStateChanged();
            CopyIfStateChanged();
            currentPointer = currentPointer == _images.Count - 1 ? _images.Count - 1 : currentPointer + 1;
            RefreshAll();
            currentSelectionState = _selection.Any(a => a == _images[currentPointer]);
        }

        private void Previous()
        {
            WriteIfStateChanged();
            CopyIfStateChanged();
            currentPointer = currentPointer == 0 ? 0 : currentPointer - 1;
            RefreshAll();
            currentSelectionState = _selection.Any(a => a == _images[currentPointer]);
        }

        private void PageUp()
        {
            WriteIfStateChanged();
            CopyIfStateChanged();
            currentPointer = currentPointer + PAGE_BY > _images.Count - 1 ? _images.Count - 1 : currentPointer + PAGE_BY;
            RefreshAll();
            currentSelectionState = _selection.Any(a => a == _images[currentPointer]);
        }

        private void PageDown()
        {
            WriteIfStateChanged();
            CopyIfStateChanged();
            currentPointer = currentPointer - PAGE_BY <= 0 ? 0 : currentPointer - PAGE_BY;
            RefreshAll();
            currentSelectionState = _selection.Any(a => a == _images[currentPointer]);
        }

        private void Delete()
        {
            _deleted.Add(_images[currentPointer]);
            _images.RemoveAt(currentPointer);
            RefreshAll();
            File.Move(_deleted.LastOrDefault(), DELETED_FOLDER_PATH + "\\" + System.IO.Path.GetFileName(_deleted.LastOrDefault()));
            currentSelectionState = _selection.Any(a => a == _images[currentPointer]);
        }
        
        private void WriteIfStateChanged()
        {
            // Check if selection state changed
            if (_selection.Any(a => a == _images[currentPointer]) != currentSelectionState)
            {
                // State has changed, write to file
                File.WriteAllLines(SELECTED_FILE_PATH, _selection.Select(a => System.IO.Path.GetFileName(a)));
            }
        }

        private void CopyIfStateChanged()
        {
            // Check if selection state changed
            if (_selection.Any(a => a == _images[currentPointer]) != currentSelectionState)
            {
                if (_selection.Any(a => a == _images[currentPointer]))
                {
                    // A new image has been selected, copy it
                    File.Copy(_images[currentPointer], string.Format("{0}/{1}", SELECTED_FOLDER_PATH, System.IO.Path.GetFileName(_images[currentPointer])));
                }
                else
                {
                    // An image should be removed from selected
                    File.Delete(string.Format("{0}/{1}", SELECTED_FOLDER_PATH, System.IO.Path.GetFileName(_images[currentPointer])));
                }
            }
        }

        private void RefreshAll()
        {
            LoadImageAtPointer(currentPointer);
            RefreshMetaData();
        }

        private void RefreshMetaData()
        {
            UpdateSelectedBackground();
            UpdateSelectedText();
        }

        private void LoadImageAtPointer(int index)
        {
            if (index > _images.Count - 1 || index < 0)
            {
                return;
            }

            BitmapImage b = new BitmapImage();
            b.BeginInit();
            b.CacheOption = BitmapCacheOption.None;
            b.UriCachePolicy = new RequestCachePolicy(RequestCacheLevel.BypassCache);
            b.CacheOption = BitmapCacheOption.OnLoad;
            b.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            b.UriSource = new Uri(_images[index], UriKind.Absolute);
            b.EndInit();

            imgImage.Source = b;
            imgImage.Stretch = Stretch.Uniform;
        }

        private void UpdateSelectedBackground()
        {
            btnSelect.Background = _selection.Any(a => a == _images[currentPointer]) ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.White);
        }

        private void UpdateSelectedText()
        {
            lblImagesSelected.Content = string.Format(SEL, _selection.Count);
            lblImagesLeft.Content = string.Format(LEFT, currentPointer + 1, _images.Count);
        }

        private void mainWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Right)
            {
                Next();
            }
            else if (e.Key == Key.Left)
            {
                Previous();
            }
            else if (e.Key == Key.Space)
            {
                ToggleSelectImage();
            }
            else if (e.Key == Key.Delete)
            {
                Delete();
            }
            else if (e.Key == Key.PageUp)
            {
                PageUp();
            }
            else if (e.Key == Key.PageDown)
            {
                PageDown();
            }
        }
    }
}
