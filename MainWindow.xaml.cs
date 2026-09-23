using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Microsoft.Win32;
using Microsoft.VisualBasic.FileIO;

namespace ThreeMFExplorer
{
    public partial class MainWindow : Window
    {
        private readonly List<ThreeMfFile> _files = new();
        private readonly List<ThreeMfFile> _filteredFiles = new();

        private GitHubRelease? _availableRelease;
        private readonly UpdateService _updateService = new();

        private Border? _selectedThumbnail;
        private ThreeMfFile? _selectedFile;

        private readonly HashSet<ThreeMfFile> _selectedFiles = new();
        private readonly Dictionary<ThreeMfFile, Border> _thumbnailBorders = new();
        private ThreeMfFile? _selectionAnchor;

        private readonly HashSet<string> _favorites = new(StringComparer.OrdinalIgnoreCase);
        private bool _showFavoritesOnly;
        private static readonly string FavoritesFilePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "3MF-Explorer", "favorites.txt");

        private static readonly string LegacyFavoritesFilePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "3MF-Explorer", "favorites.txt");

        private string _thumbnailView = "Medium";
        private bool _thumbnailViewInitialized;

        // 3D viewer state
        private Point _last3DMousePosition;
        private bool _is3DRotating;
        private bool _is3DPanning;
        private Point _model3DLeftMouseDownPosition;
        private bool _model3DLeftMouseDragged;
        private double _model3DYaw;
        private double _model3DPitch;
        private Point3D _model3DCenter;
        private double _model3DInitialDistance = 200;
        private double _model3DDistance = 200;
        private Vector3D _model3DPan;
        private Model3DGroup? _buildPlateModel;
        private const double BuildPlateThickness = 2.0;
        private const double BuildVolumeTolerance = 0.10;
        private const double BuildPlateSnapTolerance = 2.00;
        private PrinterProfile _activePrinterProfile = PrinterProfile.BambuLabX1C;
        private Rect3D _current3DModelBounds = Rect3D.Empty;
        private Rect3D _current3DViewBounds = Rect3D.Empty;
        private bool _printerProfileInitialized;

        private ThreeMfScene? _current3DScene;
        private string? _loaded3DFilePath;
        private bool _isLoading3D;
        private int? _activePlateId;

        private ThreeMfSceneObject? _selectedSceneObject;
        private readonly HashSet<ThreeMfSceneObject> _hiddenSceneObjects = new();
        private readonly Dictionary<GeometryModel3D, (Material? Material, Material? BackMaterial)> _highlightedObjectMaterials = new();

        private readonly Brush _thumbnailBackground =
            new SolidColorBrush(Color.FromRgb(23, 26, 33));

        private readonly Brush _thumbnailHoverBackground =
            new SolidColorBrush(Color.FromRgb(30, 35, 44));

        private readonly Brush _thumbnailSelectedBackground =
            new SolidColorBrush(Color.FromRgb(27, 42, 65));

        private readonly Brush _thumbnailBorder =
            new SolidColorBrush(Color.FromRgb(42, 48, 59));

        private readonly Brush _thumbnailHoverBorder =
            new SolidColorBrush(Color.FromRgb(70, 82, 100));

        private readonly Brush _thumbnailSelectedBorder =
            new SolidColorBrush(Color.FromRgb(76, 141, 255));

        public MainWindow()
        {
            InitializeComponent();

            InitializePrinterProfiles();

            Localization.SetLanguage(AppSettings.LoadLanguage());
            ApplyLanguage();

            TxtVersion.Text = $"Version {VersionInfo.Version}";
            TxtUpdate.Visibility = Visibility.Collapsed;

            StateChanged += MainWindow_StateChanged;
            PreviewKeyDown += MainWindow_PreviewKeyDown;

            LadeThumbnailAnsicht();
            LadeFavoriten();
            AktualisiereFavoritenFilterButton();
            LadeLaufwerke();

            string? lastFolder = AppSettings.LoadLastFolder();

            if (!string.IsNullOrWhiteSpace(lastFolder) &&
                Directory.Exists(lastFolder))
            {
                TxtOrdner.Text = lastFolder;
                Lade3mfDateien(lastFolder);
            }

            _ = PruefeAufUpdatesAsync();
        }

        // ============================================================
        // TITELLEISTE
        // ============================================================

        private void TitleBar_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ToggleMaximize();
                return;
            }

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                try
                {
                    DragMove();
                }
                catch
                {
                }
            }
        }

        private void BtnMinimize_Click(
            object sender,
            RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void BtnMaximize_Click(
            object sender,
            RoutedEventArgs e)
        {
            ToggleMaximize();
        }

        private void BtnClose_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }

        private void ToggleMaximize()
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
            else
            {
                WindowState = WindowState.Maximized;
            }

            AktualisiereMaximizeSymbol();
        }

        private void MainWindow_StateChanged(
            object? sender,
            EventArgs e)
        {
            AktualisiereMaximizeSymbol();
        }

        private void AktualisiereMaximizeSymbol()
        {
            if (BtnMaximize == null)
                return;

            BtnMaximize.Content =
                WindowState == WindowState.Maximized
                    ? "❐"
                    : "□";

            BtnMaximize.ToolTip =
                WindowState == WindowState.Maximized
                    ? Localization.Get("Restore")
                    : Localization.Get("Maximize");
        }

        // ============================================================
        // SPRACHE / LANGUAGE
        // ============================================================

        private void BtnDE_Click(object sender, RoutedEventArgs e)
        {
            SetLanguage("de");
        }

        private void BtnEN_Click(object sender, RoutedEventArgs e)
        {
            SetLanguage("en");
        }

        private void SetLanguage(string language)
        {
            Localization.SetLanguage(language);
            AppSettings.SaveLanguage(language);
            ApplyLanguage();
            AktualisiereFavoritenFilterButton();
            AktualisiereStatusAuswahl();

            if (_selectedFile is not null)
                ZeigeDatei(_selectedFile);
        }

        private void ApplyLanguage()
        {
            bool de = Localization.IsGerman;

            TxtSubtitle.Text = Localization.Get("Subtitle");
            BtnHelp.Content = Localization.Get("Help");
            BtnHelp.ToolTip = Localization.Get("OpenUserGuide");
            BtnMinimize.ToolTip = Localization.Get("Minimize");
            BtnClose.ToolTip = Localization.Get("Close");

            TxtSuche.ToolTip = Localization.Get("SearchFiles");
            TxtFolderHeading.Text = Localization.Get("FoldersHeading");
            BtnOrdner.Content = Localization.Get("SelectFolder");
            TxtModelsHeading.Text = Localization.Get("Models");
            TxtSortLabel.Text = Localization.Get("SortLabel");
            TxtViewLabel.Text = Localization.Get("ViewLabel");
            TxtPreviewHeading.Text = Localization.Get("PreviewHeading");
            BtnPreviewLarge.ToolTip = Localization.Get("EnlargePreview");
            Txt3DHint.Text = Localization.Get("3DHint");
            LblPrinter3D.Text = Localization.Get("Printer");
            BtnPrinterMenu.ToolTip = Localization.Get("PrinterAndBuildPlate");
            BtnViewMenu.ToolTip = Localization.Get("Select3DView");
            ChkShowBuildPlate.Content = Localization.Get("ShowBuildPlate");
            ChkShowOriginalBuildPlate.Content =
                Localization.Get("ShowOriginalBuildPlate");
            BtnViewIso.Content = Localization.Get("ViewIsometric");
            BtnViewTop.Content = Localization.Get("ViewTop");
            BtnViewFront.Content = Localization.Get("ViewFront");
            BtnViewBack.Content = Localization.Get("ViewBack");
            BtnViewLeft.Content = Localization.Get("ViewLeft");
            BtnViewRight.Content = Localization.Get("ViewRight");
            BtnViewReset.Content = Localization.Get("ResetView");
            BtnObjectsMenu.ToolTip = Localization.Get("ObjectsMenu");
            LblObjects3D.Text = Localization.Get("Objects");
            BtnShowAllObjects.Content = Localization.Get("ShowAllObjects");
            BtnFocusObject.Content = Localization.Get("FocusObject");

            BtnIsolateObject.Content =
                Localization.Get("IsolateObject");
            TxtPrintInfoHeading.Text = Localization.Get("PrintInformationHeading");

            AktualisiereObjektliste3D();
            AktualisiereAusgewaehltesObjektInfo3D();

            CmbSortNameAsc.Content = Localization.Get("NameAZ");
            CmbSortNameDesc.Content = Localization.Get("NameZA");
            CmbSortDateDesc.Content = Localization.Get("NewestFirst");
            CmbSortDateAsc.Content = Localization.Get("OldestFirst");
            CmbSortSizeDesc.Content = Localization.Get("LargestFirst");
            CmbSortSizeAsc.Content = Localization.Get("SmallestFirst");
            CmbViewSmall.Content = Localization.Get("Small");
            CmbViewMedium.Content = Localization.Get("Medium");
            CmbViewLarge.Content = Localization.Get("Large");

            LblPrintTime.Text = Localization.Get("PrintTime");
            LblMaterial.Text = Localization.Get("Material");
            LblFilament.Text = Localization.Get("Filament");
            LblFilamentLength.Text = Localization.Get("FilamentLength");
            LblNozzle.Text = Localization.Get("Nozzle");
            LblPrintProfile.Text = Localization.Get("PrintProfile");
            LblBuildPlate.Text = Localization.Get("BuildPlate");
            LblBambuStudio.Text = "Bambu Studio";
            LblSupport.Text = "Support";

            BtnDE.Background = de ? new SolidColorBrush(Color.FromRgb(45, 63, 92)) : new SolidColorBrush(Color.FromRgb(37, 42, 52));
            BtnEN.Background = !de ? new SolidColorBrush(Color.FromRgb(45, 63, 92)) : new SolidColorBrush(Color.FromRgb(37, 42, 52));

            AktualisiereMaximizeSymbol();
            AktualisiereDateiAnzahl();
        }

        private void AktualisiereDateiAnzahl()
        {
            TxtAnzahl.Text = string.Format(
                Localization.Get("FileCount"),
                _filteredFiles.Count);
        }

        // ============================================================
        // HILFE
        // ============================================================

        private void BtnHelp_Click(
            object sender,
            RoutedEventArgs e)
        {
            HelpWindow helpWindow =
                new HelpWindow
                {
                    Owner = this
                };

            helpWindow.ShowDialog();
        }

        // ============================================================
        // TASTATURBEDIENUNG
        // ============================================================

        private void MainWindow_PreviewKeyDown(
            object sender,
            KeyEventArgs e)
        {
            if (e.Key == Key.D &&
                Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                ToggleFavoritFuerAuswahl();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.A &&
                Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                WaehleAlleGefiltertenDateien();
                e.Handled = true;
                return;
            }

            if (_selectedFile is null)
                return;

            if (e.Key == Key.Enter)
            {
                OeffneDatei(_selectedFile);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.F2)
            {
                if (_selectedFiles.Count == 1)
                {
                    DateiUmbenennen(_selectedFile);
                }

                e.Handled = true;
                return;
            }

            if (e.Key == Key.Delete)
            {
                DateienLoeschen(_selectedFiles.ToList());
                e.Handled = true;
                return;
            }

            if (e.Key == Key.C &&
                Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                if (_selectedFiles.Count <= 1)
                {
                    KopiereDateipfad(_selectedFile);
                }
                else
                {
                    KopiereDateipfade(_selectedFiles);
                }

                e.Handled = true;
            }
        }

        private void AktualisiereStatusAuswahl()
        {
            if (_selectedFiles.Count == 0)
            {
                TxtStatus.Text = "3MF-Explorer";
                return;
            }

            if (_selectedFiles.Count == 1)
            {
                TxtStatus.Text = Localization.Get("OneFileSelectedHints");
                return;
            }

            TxtStatus.Text = string.Format(
                Localization.Get("FilesSelectedHints"),
                _selectedFiles.Count);
        }

        private void WaehleAlleGefiltertenDateien()
        {
            if (_filteredFiles.Count == 0)
                return;

            _selectedFiles.Clear();

            foreach (ThreeMfFile file in _filteredFiles)
            {
                _selectedFiles.Add(file);
            }

            _selectedFile = _filteredFiles[^1];
            _selectionAnchor = _selectedFile;

            AktualisiereAlleThumbnailMarkierungen();
            AktualisiereStatusAuswahl();
            ZeigeDatei(_selectedFile);
        }

        // ============================================================
        // FAVORITEN
        // ============================================================

        private void LadeFavoriten()
        {
            try
            {
                string sourceFile =
                    File.Exists(FavoritesFilePath)
                        ? FavoritesFilePath
                        : LegacyFavoritesFilePath;

                if (!File.Exists(sourceFile))
                    return;

                foreach (string line in File.ReadAllLines(sourceFile))
                {
                    string path = line.Trim();
                    if (!string.IsNullOrWhiteSpace(path))
                        _favorites.Add(path);
                }
            }
            catch
            {
            }
        }

        private void SpeichereFavoriten()
        {
            try
            {
                string? directory = Path.GetDirectoryName(FavoritesFilePath);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllLines(FavoritesFilePath, _favorites.OrderBy(path => path, StringComparer.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{Localization.Get("FavoritesSaveError")}\n\n{ex.Message}",
                    Localization.Get("Favorites"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private bool IstFavorit(ThreeMfFile file) => _favorites.Contains(file.FullPath);

        private void ToggleFavorit(ThreeMfFile file)
        {
            if (!_favorites.Add(file.FullPath))
                _favorites.Remove(file.FullPath);

            SpeichereFavoriten();
            AktualisiereGalerie();
        }

        private void ToggleFavoritFuerAuswahl()
        {
            if (_selectedFiles.Count == 0)
                return;

            bool alleFavoriten = _selectedFiles.All(IstFavorit);

            foreach (ThreeMfFile file in _selectedFiles)
            {
                if (alleFavoriten)
                    _favorites.Remove(file.FullPath);
                else
                    _favorites.Add(file.FullPath);
            }

            SpeichereFavoriten();
            AktualisiereGalerie();
        }

        private void BtnFavoritenFilter_Click(object sender, RoutedEventArgs e)
        {
            _showFavoritesOnly = !_showFavoritesOnly;
            AktualisiereFavoritenFilterButton();
            AktualisiereGalerie();
        }

        private void AktualisiereFavoritenFilterButton()
        {
            if (BtnFavoritenFilter == null)
                return;

            BtnFavoritenFilter.Content = _showFavoritesOnly
                ? Localization.Get("OnlyFavoritesActive")
                : Localization.Get("OnlyFavorites");

            BtnFavoritenFilter.ToolTip = Localization.Get("OnlyFavoritesToolTip");

            BtnFavoritenFilter.Background = _showFavoritesOnly
                ? new SolidColorBrush(Color.FromRgb(45, 63, 92))
                : new SolidColorBrush(Color.FromRgb(37, 42, 52));
        }

        // ============================================================
        // UPDATE
        // ============================================================

        private async Task PruefeAufUpdatesAsync()
        {
            try
            {
                GitHubRelease? release =
                    await _updateService.GetLatestReleaseAsync();

                if (release is null)
                    return;

                if (_updateService.IsNewerVersion(
                    VersionInfo.Version,
                    release.TagName))
                {
                    _availableRelease = release;

                    TxtUpdate.Text = string.Format(
                        Localization.Get("UpdateAvailableClick"),
                        release.TagName);

                    TxtUpdate.Visibility =
                        Visibility.Visible;
                }
            }
            catch
            {
            }
        }

        private async void TxtUpdate_MouseLeftButtonUp(
            object sender,
            MouseButtonEventArgs e)
        {
            if (_availableRelease is null)
                return;

            MessageBoxResult result =
                MessageBox.Show(
                    string.Format(
                        Localization.Get("UpdateInstallQuestion"),
                        _availableRelease.TagName),
                    Localization.Get("UpdateAvailableTitle"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                TxtUpdate.Text = Localization.Get("DownloadingUpdate");

                TxtUpdate.IsEnabled = false;

                await _updateService.InstallAsync(
                    _availableRelease);
            }
            catch (Exception ex)
            {
                TxtUpdate.IsEnabled = true;

                TxtUpdate.Text = string.Format(
                    Localization.Get("UpdateAvailableClick"),
                    _availableRelease.TagName);

                MessageBox.Show(
                    $"{Localization.Get("UpdateInstallError")}\n\n{ex.Message}",
                    Localization.Get("UpdateErrorTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // ============================================================
        // THUMBNAIL-ANSICHT
        // ============================================================

        private void LadeThumbnailAnsicht()
        {
            _thumbnailView =
                AppSettings.LoadThumbnailView();

            int selectedIndex =
                _thumbnailView switch
                {
                    "Small" => 0,
                    "Large" => 2,
                    _ => 1
                };

            CmbAnsicht.SelectedIndex =
                selectedIndex;

            _thumbnailViewInitialized = true;
        }

        private void CmbAnsicht_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (!_thumbnailViewInitialized)
                return;

            if (CmbAnsicht.SelectedItem
                is not ComboBoxItem item ||
                item.Tag is not string tag)
            {
                return;
            }

            _thumbnailView = tag;

            AppSettings.SaveThumbnailView(
                _thumbnailView);

            AktualisiereGalerie();
        }

        private (double Width,
                 double Height,
                 double ImageHeight,
                 double Margin,
                 double FontSize)
            GetThumbnailDimensions()
        {
            return _thumbnailView switch
            {
                "Small" =>
                    (
                        Width: 150,
                        Height: 185,
                        ImageHeight: 128,
                        Margin: 6,
                        FontSize: 11
                    ),

                "Large" =>
                    (
                        Width: 250,
                        Height: 300,
                        ImageHeight: 230,
                        Margin: 10,
                        FontSize: 13
                    ),

                _ =>
                    (
                        Width: 190,
                        Height: 230,
                        ImageHeight: 170,
                        Margin: 9,
                        FontSize: 12
                    )
            };
        }

        // ============================================================
        // ORDNER
        // ============================================================

        private void LadeLaufwerke()
        {
            TreeFolders.Items.Clear();

            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady)
                    continue;

                TreeViewItem item =
                    new TreeViewItem
                    {
                        Header = drive.Name,
                        Tag = drive.RootDirectory.FullName
                    };

                item.Items.Add("Dummy");
                item.Expanded += Folder_Expanded;

                TreeFolders.Items.Add(item);
            }
        }

        private void Folder_Expanded(
            object sender,
            RoutedEventArgs e)
        {
            TreeViewItem item =
                (TreeViewItem)sender;

            if (item.Items.Count == 1 &&
                item.Items[0]?.ToString() == "Dummy")
            {
                item.Items.Clear();

                string path =
                    item.Tag!.ToString()!;

                try
                {
                    foreach (string dir
                             in Directory.GetDirectories(path))
                    {
                        TreeViewItem sub =
                            new TreeViewItem
                            {
                                Header = Path.GetFileName(dir),
                                Tag = dir
                            };

                        sub.Items.Add("Dummy");
                        sub.Expanded += Folder_Expanded;

                        item.Items.Add(sub);
                    }
                }
                catch
                {
                }
            }
        }

        private void TreeFolders_SelectedItemChanged(
            object sender,
            RoutedPropertyChangedEventArgs<object> e)
        {
            if (TreeFolders.SelectedItem
                is not TreeViewItem item)
            {
                return;
            }

            string? path =
                item.Tag?.ToString();

            if (string.IsNullOrEmpty(path))
                return;

            TxtOrdner.Text = path;

            AppSettings.SaveLastFolder(path);

            Lade3mfDateien(path);
        }

        private void BtnOrdner_Click(
            object sender,
            RoutedEventArgs e)
        {
            OpenFolderDialog dialog =
                new OpenFolderDialog();

            if (dialog.ShowDialog() == true)
            {
                TxtOrdner.Text =
                    dialog.FolderName;

                AppSettings.SaveLastFolder(
                    dialog.FolderName);

                Lade3mfDateien(
                    dialog.FolderName);
            }
        }

        // ============================================================
        // DATEIEN LADEN
        // ============================================================

        private void Lade3mfDateien(
            string ordner)
        {
            _files.Clear();

            ThumbnailCache.Clear();

            _selectedThumbnail = null;
            _selectedFile = null;
            _selectionAnchor = null;
            _selectedFiles.Clear();
            _thumbnailBorders.Clear();
            AktualisiereStatusAuswahl();

            LeereDateiAnzeige();

            if (!Directory.Exists(ordner))
            {
                TxtAnzahl.Text = string.Format(Localization.Get("FileCount"), 0);
                ThumbnailPanel.Children.Clear();

                return;
            }

            string[] dateien =
                Directory.GetFiles(
                    ordner,
                    "*.3mf",
                    System.IO.SearchOption.TopDirectoryOnly);

            foreach (string datei in dateien)
            {
                _files.Add(
                    new ThreeMfFile
                    {
                        FileName =
                            Path.GetFileName(datei),

                        FullPath =
                            datei
                    });
            }

            AktualisiereGalerie();
        }

        // ============================================================
        // SUCHE UND SORTIERUNG
        // ============================================================

        private void TxtSuche_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            AktualisiereGalerie();
        }

        private void CmbSortierung_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
                return;

            AktualisiereGalerie();
        }

        private void AktualisiereGalerie()
        {
            ThumbnailPanel.Children.Clear();
            _filteredFiles.Clear();
            _selectedThumbnail = null;
            _selectedFile = null;
            _selectionAnchor = null;
            _selectedFiles.Clear();
            _thumbnailBorders.Clear();
            AktualisiereStatusAuswahl();

            string filter =
                TxtSuche.Text.Trim();

            IEnumerable<ThreeMfFile> result =
                _files.Where(
                    file =>
                        (string.IsNullOrWhiteSpace(filter) ||
                         file.FileName.Contains(
                             filter,
                             StringComparison.OrdinalIgnoreCase)) &&
                        (!_showFavoritesOnly || IstFavorit(file)));

            string sortierung =
                GetAktuelleSortierung();

            result =
                sortierung switch
                {
                    "NameDesc" =>
                        result.OrderByDescending(
                            f => f.FileName,
                            StringComparer.OrdinalIgnoreCase),

                    "DateDesc" =>
                        result.OrderByDescending(
                            f => File.GetLastWriteTime(f.FullPath)),

                    "DateAsc" =>
                        result.OrderBy(
                            f => File.GetLastWriteTime(f.FullPath)),

                    "SizeDesc" =>
                        result.OrderByDescending(
                            f => GetFileSize(f.FullPath)),

                    "SizeAsc" =>
                        result.OrderBy(
                            f => GetFileSize(f.FullPath)),

                    _ =>
                        result.OrderBy(
                            f => f.FileName,
                            StringComparer.OrdinalIgnoreCase)
                };

            foreach (ThreeMfFile file in result)
            {
                _filteredFiles.Add(file);

                ErzeugeThumbnail(file);
            }

            AktualisiereDateiAnzahl();
        }

        private string GetAktuelleSortierung()
        {
            if (CmbSortierung.SelectedItem
                is ComboBoxItem item &&
                item.Tag is string tag)
            {
                return tag;
            }

            return "NameAsc";
        }

        private static long GetFileSize(
            string path)
        {
            try
            {
                return new FileInfo(path).Length;
            }
            catch
            {
                return 0;
            }
        }

        // ============================================================
        // THUMBNAILS
        // ============================================================

        private void ErzeugeThumbnail(
            ThreeMfFile file)
        {
            var dimensions =
                GetThumbnailDimensions();

            Border border =
                new Border
                {
                    Width = dimensions.Width,
                    Height = dimensions.Height,

                    Margin =
                        new Thickness(
                            dimensions.Margin),

                    Padding = new Thickness(8),

                    CornerRadius =
                        new CornerRadius(10),

                    BorderThickness =
                        new Thickness(1),

                    BorderBrush =
                        _thumbnailBorder,

                    Background =
                        _thumbnailBackground,

                    Cursor = Cursors.Hand,

                    SnapsToDevicePixels = true
                };

            Grid cardGrid =
                new Grid();

            Button favoriteButton = new Button
            {
                Content = IstFavorit(file) ? "★" : "☆",
                Width = 32,
                Height = 32,
                Padding = new Thickness(0),
                Margin = new Thickness(6),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Foreground = IstFavorit(file)
                    ? new SolidColorBrush(Color.FromRgb(255, 201, 71))
                    : new SolidColorBrush(Color.FromRgb(190, 197, 208)),
                Background = new SolidColorBrush(Color.FromArgb(210, 24, 28, 35)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(62, 70, 84)),
                BorderThickness = new Thickness(1),
                ToolTip = IstFavorit(file)
                    ? Localization.Get("RemoveFavorite")
                    : Localization.Get("AddFavorite"),
                Cursor = Cursors.Hand
            };

            favoriteButton.Click += (s, e) =>
            {
                ToggleFavorit(file);
                e.Handled = true;
            };

            cardGrid.RowDefinitions.Add(
                new RowDefinition
                {
                    Height =
                        new GridLength(
                            dimensions.ImageHeight)
                });

            cardGrid.RowDefinitions.Add(
                new RowDefinition
                {
                    Height =
                        new GridLength(
                            1,
                            GridUnitType.Star)
                });

            Border imageContainer =
                new Border
                {
                    Background =
                        new SolidColorBrush(
                            Color.FromRgb(
                                12,
                                14,
                                18)),

                    BorderBrush =
                        new SolidColorBrush(
                            Color.FromRgb(
                                43,
                                49,
                                59)),

                    BorderThickness =
                        new Thickness(1),

                    CornerRadius =
                        new CornerRadius(7),

                    Padding =
                        new Thickness(4),

                    Margin =
                        new Thickness(
                            0,
                            0,
                            0,
                            8)
                };

            Image image =
                new Image
                {
                    Stretch = Stretch.Uniform,
                    IsHitTestVisible = false
                };

            string? preview;

            DateTime lastWriteTime =
                File.GetLastWriteTime(
                    file.FullPath);

            if (!ThumbnailCache.TryGet(
                    file.FullPath,
                    lastWriteTime,
                    out string cachedPreview))
            {
                preview =
                    ThumbnailExtractor.ExtractPreview(
                        file.FullPath);

                if (!string.IsNullOrWhiteSpace(preview))
                {
                    ThumbnailCache.Store(
                        file.FullPath,
                        preview,
                        lastWriteTime);
                }
            }
            else
            {
                preview = cachedPreview;
            }

            if (!string.IsNullOrEmpty(preview) &&
                File.Exists(preview))
            {
                try
                {
                    BitmapImage bitmap =
                        new BitmapImage();

                    bitmap.BeginInit();

                    bitmap.UriSource =
                        new Uri(preview);

                    bitmap.CacheOption =
                        BitmapCacheOption.OnLoad;

                    bitmap.EndInit();
                    bitmap.Freeze();

                    image.Source = bitmap;
                }
                catch
                {
                }
            }

            imageContainer.Child = image;

            Grid.SetRow(
                imageContainer,
                0);

            cardGrid.Children.Add(
                imageContainer);

            Grid.SetRow(favoriteButton, 0);
            Panel.SetZIndex(favoriteButton, 10);
            cardGrid.Children.Add(favoriteButton);

            TextBlock text =
                new TextBlock
                {
                    Text =
                        Path.GetFileNameWithoutExtension(
                            file.FileName),

                    TextAlignment =
                        TextAlignment.Center,

                    TextWrapping =
                        TextWrapping.Wrap,

                    TextTrimming =
                        TextTrimming.CharacterEllipsis,

                    Foreground =
                        new SolidColorBrush(
                            Color.FromRgb(
                                226,
                                230,
                                236)),

                    FontSize =
                        dimensions.FontSize,

                    FontWeight =
                        FontWeights.Medium,

                    VerticalAlignment =
                        VerticalAlignment.Center,

                    HorizontalAlignment =
                        HorizontalAlignment.Stretch,

                    Margin =
                        new Thickness(
                            4,
                            0,
                            4,
                            0),

                    IsHitTestVisible = false,

                    ToolTip = file.FileName
                };

            Grid.SetRow(text, 1);

            cardGrid.Children.Add(text);

            border.Child = cardGrid;
            _thumbnailBorders[file] = border;

            border.MouseEnter +=
                (s, e) =>
                {
                    if (!_selectedFiles.Contains(file))
                    {
                        border.Background =
                            _thumbnailHoverBackground;

                        border.BorderBrush =
                            _thumbnailHoverBorder;
                    }
                };

            border.MouseLeave +=
                (s, e) =>
                {
                    if (!_selectedFiles.Contains(file))
                    {
                        border.Background =
                            _thumbnailBackground;

                        border.BorderBrush =
                            _thumbnailBorder;

                        border.BorderThickness =
                            new Thickness(1);
                    }
                };

            border.MouseLeftButtonUp +=
                (s, e) =>
                {
                    ModifierKeys modifiers = Keyboard.Modifiers;

                    if (modifiers.HasFlag(ModifierKeys.Shift))
                    {
                        MarkiereBereich(file,
                            modifiers.HasFlag(ModifierKeys.Control));
                    }
                    else if (modifiers.HasFlag(ModifierKeys.Control))
                    {
                        UmschaltenThumbnail(file);
                    }
                    else
                    {
                        MarkiereNurThumbnail(file);
                    }

                    if (_selectedFiles.Contains(file))
                    {
                        _selectedFile = file;
                        _selectedThumbnail = border;
                        ZeigeDatei(file);
                    }

                    if (e.ClickCount == 2)
                    {
                        OeffneDatei(file);
                    }

                    AktualisiereStatusAuswahl();
                    e.Handled = true;
                };

            border.MouseRightButtonDown +=
                (s, e) =>
                {
                    if (!_selectedFiles.Contains(file))
                    {
                        MarkiereNurThumbnail(file);
                    }

                    _selectedFile = file;
                    _selectedThumbnail = border;
                    ZeigeDatei(file);
                    AktualisiereStatusAuswahl();

                    e.Handled = true;
                };

            border.MouseRightButtonUp +=
                (s, e) =>
                {
                    ContextMenu menu =
                        ErzeugeKontextMenue(file);

                    menu.PlacementTarget = border;
                    border.ContextMenu = menu;
                    menu.IsOpen = true;

                    e.Handled = true;
                };

            ThumbnailPanel.Children.Add(
                border);
        }

        private void MarkiereNurThumbnail(
            ThreeMfFile file)
        {
            _selectedFiles.Clear();
            _selectedFiles.Add(file);
            _selectedFile = file;
            _selectionAnchor = file;

            _thumbnailBorders.TryGetValue(
                file,
                out _selectedThumbnail);

            AktualisiereAlleThumbnailMarkierungen();
        }

        private void UmschaltenThumbnail(
            ThreeMfFile file)
        {
            if (_selectedFiles.Contains(file))
            {
                _selectedFiles.Remove(file);

                if (_selectedFiles.Count == 0)
                {
                    _selectedFile = null;
                    _selectedThumbnail = null;
                    _selectionAnchor = null;
                    LeereDateiAnzeige();
                }
                else if (_selectedFile == file)
                {
                    _selectedFile = _selectedFiles.Last();
                    _thumbnailBorders.TryGetValue(
                        _selectedFile,
                        out _selectedThumbnail);
                    ZeigeDatei(_selectedFile);
                }
            }
            else
            {
                _selectedFiles.Add(file);
                _selectedFile = file;
                _selectionAnchor ??= file;
                _thumbnailBorders.TryGetValue(
                    file,
                    out _selectedThumbnail);
            }

            AktualisiereAlleThumbnailMarkierungen();
        }

        private void MarkiereBereich(
            ThreeMfFile file,
            bool auswahlErweitern)
        {
            ThreeMfFile anchor =
                _selectionAnchor ?? _selectedFile ?? file;

            int anchorIndex = _filteredFiles.IndexOf(anchor);
            int currentIndex = _filteredFiles.IndexOf(file);

            if (anchorIndex < 0 || currentIndex < 0)
            {
                MarkiereNurThumbnail(file);
                return;
            }

            if (!auswahlErweitern)
            {
                _selectedFiles.Clear();
            }

            int start = Math.Min(anchorIndex, currentIndex);
            int end = Math.Max(anchorIndex, currentIndex);

            for (int i = start; i <= end; i++)
            {
                _selectedFiles.Add(_filteredFiles[i]);
            }

            _selectedFile = file;
            _thumbnailBorders.TryGetValue(
                file,
                out _selectedThumbnail);

            AktualisiereAlleThumbnailMarkierungen();
        }

        private void AktualisiereAlleThumbnailMarkierungen()
        {
            foreach ((ThreeMfFile file, Border border) in _thumbnailBorders)
            {
                bool selected = _selectedFiles.Contains(file);

                border.BorderBrush = selected
                    ? _thumbnailSelectedBorder
                    : _thumbnailBorder;

                border.BorderThickness = selected
                    ? new Thickness(2)
                    : new Thickness(1);

                border.Background = selected
                    ? _thumbnailSelectedBackground
                    : _thumbnailBackground;
            }
        }

        // ============================================================
        // KONTEXTMENÜ
        // ============================================================

        private ContextMenu ErzeugeKontextMenue(
            ThreeMfFile file)
        {
            ContextMenu menu =
                new ContextMenu();

            MenuItem openItem =
                new MenuItem
                {
                    Header = Localization.Get("Open")
                };

            openItem.Click +=
                (s, e) => OeffneDatei(file);

            menu.Items.Add(openItem);

            MenuItem explorerItem =
                new MenuItem
                {
                    Header = Localization.Get("ShowInExplorer")
                };

            explorerItem.Click +=
                (s, e) => ZeigeImExplorer(file);

            menu.Items.Add(explorerItem);

            MenuItem copyPathItem =
                new MenuItem
                {
                    Header = Localization.Get("CopyPath")
                };

            copyPathItem.Click +=
                (s, e) => KopiereDateipfad(file);

            menu.Items.Add(copyPathItem);

            MenuItem favoriteItem = new MenuItem
            {
                Header = IstFavorit(file)
                    ? Localization.Get("RemoveFavorite")
                    : Localization.Get("AddFavorite")
            };

            favoriteItem.Click += (s, e) => ToggleFavorit(file);
            menu.Items.Add(favoriteItem);

            menu.Items.Add(new Separator());

            MenuItem renameItem =
                new MenuItem
                {
                    Header = Localization.Get("Rename")
                };

            renameItem.IsEnabled =
                _selectedFiles.Count <= 1;

            renameItem.Click +=
                (s, e) => DateiUmbenennen(file);

            menu.Items.Add(renameItem);

            MenuItem deleteItem =
                new MenuItem
                {
                    Header =
                        Localization.Get("MoveToRecycleBin")
                };

            deleteItem.Header =
                _selectedFiles.Count > 1
                    ? string.Format(Localization.Get("MoveFilesToRecycleBin"), _selectedFiles.Count)
                    : Localization.Get("MoveToRecycleBin");

            deleteItem.Click +=
                (s, e) => DateienLoeschen(_selectedFiles.ToList());

            menu.Items.Add(deleteItem);

            return menu;
        }

        // ============================================================
        // EXPLORER / ZWISCHENABLAGE
        // ============================================================

        private void ZeigeImExplorer(
            ThreeMfFile file)
        {
            try
            {
                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName =
                            "explorer.exe",

                        Arguments =
                            $"/select,\"{file.FullPath}\"",

                        UseShellExecute = true
                    });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{Localization.Get("ExplorerError")}\n\n{ex.Message}",
                    Localization.Get("ExplorerTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void KopiereDateipfad(
            ThreeMfFile file)
        {
            try
            {
                Clipboard.SetText(
                    file.FullPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{Localization.Get("ClipboardError")}\n\n{ex.Message}",
                    Localization.Get("ClipboardTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void KopiereDateipfade(
            IEnumerable<ThreeMfFile> files)
        {
            try
            {
                string text = string.Join(
                    Environment.NewLine,
                    files.Select(file => file.FullPath));

                if (!string.IsNullOrWhiteSpace(text))
                {
                    Clipboard.SetText(text);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{Localization.Get("ClipboardPathsError")}\n\n{ex.Message}",
                    Localization.Get("ClipboardTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // ============================================================
        // UMBENENNEN
        // ============================================================

        private void DateiUmbenennen(
            ThreeMfFile file)
        {
            if (!File.Exists(file.FullPath))
            {
                MessageBox.Show(
                    Localization.Get("FileNoLongerExists"),
                    Localization.Get("Rename"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                AktualisiereAktuellenOrdner();

                return;
            }

            string currentName =
                Path.GetFileNameWithoutExtension(
                    file.FileName);

            RenameWindow dialog =
                new RenameWindow(currentName)
                {
                    Owner = this
                };

            bool? result =
                dialog.ShowDialog();

            if (result != true)
                return;

            string newName =
                dialog.NewFileName.Trim();

            if (string.IsNullOrWhiteSpace(newName))
                return;

            foreach (char invalidChar
                     in Path.GetInvalidFileNameChars())
            {
                if (newName.Contains(invalidChar))
                {
                    MessageBox.Show(
                        Localization.Get("InvalidFileName"),
                        Localization.Get("Rename"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }
            }

            if (!newName.EndsWith(
                    ".3mf",
                    StringComparison.OrdinalIgnoreCase))
            {
                newName += ".3mf";
            }

            string? directory =
                Path.GetDirectoryName(
                    file.FullPath);

            if (string.IsNullOrWhiteSpace(directory))
                return;

            string newPath =
                Path.Combine(
                    directory,
                    newName);

            if (string.Equals(
                    file.FullPath,
                    newPath,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (File.Exists(newPath))
            {
                MessageBox.Show(
                    Localization.Get("FileAlreadyExists"),
                    Localization.Get("Rename"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            try
            {
                File.Move(
                    file.FullPath,
                    newPath);

                AktualisiereAktuellenOrdner();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{Localization.Get("RenameError")}\n\n{ex.Message}",
                    Localization.Get("Rename"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // ============================================================
        // PAPIERKORB
        // ============================================================

        private void DateiLoeschen(
            ThreeMfFile file)
        {
            DateienLoeschen(new[] { file });
        }

        private void DateienLoeschen(
            IReadOnlyCollection<ThreeMfFile> files)
        {
            List<ThreeMfFile> existingFiles =
                files
                    .Where(file => File.Exists(file.FullPath))
                    .Distinct()
                    .ToList();

            if (existingFiles.Count == 0)
            {
                MessageBox.Show(
                    Localization.Get("SelectedFilesNoLongerExist"),
                    Localization.Get("RecycleBin"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                AktualisiereAktuellenOrdner();
                return;
            }

            string message = existingFiles.Count == 1
                ? string.Format(Localization.Get("DeleteSingleQuestionDetailed"), existingFiles[0].FileName)
                : string.Format(Localization.Get("DeleteMultipleQuestionDetailed"), existingFiles.Count);

            MessageBoxResult result =
                MessageBox.Show(
                    message,
                    Localization.Get("MoveToRecycleBin"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            List<string> errors = new();

            foreach (ThreeMfFile file in existingFiles)
            {
                try
                {
                    FileSystem.DeleteFile(
                        file.FullPath,
                        UIOption.OnlyErrorDialogs,
                        RecycleOption.SendToRecycleBin);
                }
                catch (Exception ex)
                {
                    errors.Add($"{file.FileName}: {ex.Message}");
                }
            }

            AktualisiereAktuellenOrdner();

            if (errors.Count > 0)
            {
                MessageBox.Show(
                    Localization.Get("SomeDeleteErrors") + "\n\n" +
                    string.Join(Environment.NewLine, errors),
                    Localization.Get("RecycleBin"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        // ============================================================
        // AKTUELLEN ORDNER NEU LADEN
        // ============================================================

        private void AktualisiereAktuellenOrdner()
        {
            string currentFolder =
                TxtOrdner.Text.Trim();

            if (string.IsNullOrWhiteSpace(
                    currentFolder))
            {
                return;
            }

            if (!Directory.Exists(
                    currentFolder))
            {
                return;
            }

            Lade3mfDateien(
                currentFolder);
        }

        // ============================================================
        // VORSCHAU
        // ============================================================

        private void ZeigeDatei(
            ThreeMfFile file)
        {
            if (!File.Exists(file.FullPath))
                return;

            if (!string.Equals(
                    _loaded3DFilePath,
                    file.FullPath,
                    StringComparison.OrdinalIgnoreCase))
            {
                RestoreObjectHighlight();

                ModelVisual.Content = null;
                BuildPlateVisual.Content = null;
                BuildVolumeWarningVisual.Content = null;

                _current3DScene = null;
                _loaded3DFilePath = null;
                _selectedSceneObject = null;
                _hiddenSceneObjects.Clear();
                _buildPlateModel = null;
                _current3DModelBounds = Rect3D.Empty;
                _current3DViewBounds = Rect3D.Empty;

                ResetObjectControls3D();
            }

            if (ModelViewport.Visibility == Visibility.Visible)
            {
                ImgPreview.Visibility = Visibility.Visible;
                ModelViewport.Visibility = Visibility.Collapsed;
            }

            OrientationGizmoContainer.Visibility = Visibility.Collapsed;

            string? preview;

            DateTime lastWriteTime =
                File.GetLastWriteTime(
                    file.FullPath);

            if (!ThumbnailCache.TryGet(
                    file.FullPath,
                    lastWriteTime,
                    out string cachedPreview))
            {
                preview =
                    ThumbnailExtractor.ExtractPreview(
                        file.FullPath);

                if (!string.IsNullOrWhiteSpace(
                        preview))
                {
                    ThumbnailCache.Store(
                        file.FullPath,
                        preview,
                        lastWriteTime);
                }
            }
            else
            {
                preview = cachedPreview;
            }

            ImgPreview.Source = null;

            if (!string.IsNullOrEmpty(preview) &&
                File.Exists(preview))
            {
                try
                {
                    BitmapImage bitmap =
                        new BitmapImage();

                    bitmap.BeginInit();

                    bitmap.UriSource =
                        new Uri(preview);

                    bitmap.CacheOption =
                        BitmapCacheOption.OnLoad;

                    bitmap.EndInit();
                    bitmap.Freeze();

                    ImgPreview.Source =
                        bitmap;
                }
                catch
                {
                    ImgPreview.Source =
                        null;
                }
            }

            FileInfo info =
                new FileInfo(
                    file.FullPath);

            TxtDateiname.Text =
                file.FileName;

            TxtGroesse.Text =
                string.Format(
                    Localization.Get("SizeValue"),
                    info.Length / 1024);

            TxtDatum.Text =
                string.Format(
                    Localization.Get("ModifiedValue"),
                    info.LastWriteTime);

            BambuPrintMetadata metadata =
                ThreeMfMetadataExtractor.Extract(
                    file.FullPath);

            TxtDruckzeit.Text =
                metadata.PrintTime;

            TxtMaterial.Text =
                metadata.Material;

            TxtFilament.Text =
                metadata.FilamentWeight;

            TxtFilamentLaenge.Text =
                metadata.FilamentLength;

            TxtDuese.Text =
                metadata.NozzleDiameter;

            TxtDruckprofil.Text =
                metadata.PrintProfile;

            TxtDruckplatte.Text =
                metadata.PlateType;

            TxtBambuStudio.Text =
                metadata.BambuStudioVersion;

            TxtSupport.Text =
                metadata.Support;
        }

        private void LeereDateiAnzeige()
        {
            ImgPreview.Source = null;

            TxtDateiname.Text = "";
            TxtGroesse.Text = "";
            TxtDatum.Text = "";

            TxtDruckzeit.Text = "–";
            TxtMaterial.Text = "–";
            TxtFilament.Text = "–";
            TxtFilamentLaenge.Text = "–";
            TxtDuese.Text = "–";
            TxtDruckprofil.Text = "–";
            TxtDruckplatte.Text = "–";
            TxtBambuStudio.Text = "–";
            TxtSupport.Text = "–";

            ResetObjectControls3D();
        }

        // ============================================================
        // 2D / 3D VORSCHAU
        // ============================================================

        private void BtnPrinterMenu_Click(object sender, RoutedEventArgs e)
        {
            PrinterMenuPopup.IsOpen = !PrinterMenuPopup.IsOpen;
        }

        private void BtnViewMenu_Click(object sender, RoutedEventArgs e)
        {
            if (!BtnViewMenu.IsEnabled)
                return;

            ViewMenuPopup.IsOpen = !ViewMenuPopup.IsOpen;
        }


        private IReadOnlyList<ThreeMfSceneObject>
            GetActivePlateObjects()
        {
            if (_current3DScene is null)
                return Array.Empty<ThreeMfSceneObject>();

            if (_activePlateId is null)
                return _current3DScene.Objects;

            return _current3DScene.Objects
                .Where(
                    item =>
                        item.PlateId == _activePlateId.Value)
                .ToList();
        }

        private void BtnPlateMenu_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!BtnPlateMenu.IsEnabled)
                return;

            PlateMenuPopup.IsOpen =
                !PlateMenuPopup.IsOpen;
        }

        private void AktualisierePlattenliste3D()
        {
            if (PlatesPanel3D == null ||
                BtnPlateMenu == null)
                return;

            PlatesPanel3D.Children.Clear();

            IReadOnlyList<ThreeMfPlate> plates =
                _current3DScene?.Plates
                ?? Array.Empty<ThreeMfPlate>();

            BtnPlateMenu.IsEnabled =
                plates.Count > 1;

            BtnPlateMenu.ToolTip =
                Localization.IsGerman
                    ? (_activePlateId is null
                        ? "Druckplatten · Alle"
                        : $"Druckplatten · Platte {_activePlateId}")
                    : (_activePlateId is null
                        ? "Plates · All"
                        : $"Plates · Plate {_activePlateId}");

            if (plates.Count <= 1)
                return;

            AddPlateButton(
                Localization.IsGerman
                    ? "Alle Platten"
                    : "All plates",
                null,
                null);

            foreach (ThreeMfPlate plate in plates)
            {
                string label =
                    Localization.IsGerman
                        ? $"Platte {plate.PlateId}"
                        : $"Plate {plate.PlateId}";

                AddPlateButton(
                    label,
                    plate.PlateId,
                    plate);
            }
        }

        private void AddPlateButton(
            string text,
            int? plateId,
            ThreeMfPlate? plate)
        {
            bool isActive =
                _activePlateId == plateId;

            Button button =
                new Button
                {
                    Tag = plateId,
                    Margin =
                        new Thickness(
                            0,
                            2,
                            0,
                            2),
                    Padding =
                        new Thickness(
                            6),
                    HorizontalContentAlignment =
                        HorizontalAlignment.Stretch,
                    Background =
                        new SolidColorBrush(
                            isActive
                                ? Color.FromRgb(
                                    45,
                                    54,
                                    68)
                                : Color.FromRgb(
                                    31,
                                    35,
                                    44)),
                    BorderBrush =
                        new SolidColorBrush(
                            isActive
                                ? Color.FromRgb(
                                    255,
                                    196,
                                    92)
                                : Color.FromRgb(
                                    59,
                                    67,
                                    80)),
                    BorderThickness =
                        new Thickness(
                            isActive
                                ? 2
                                : 1)
                };

            if (plate is null)
            {
                button.Content =
                    new TextBlock
                    {
                        Text =
                            (isActive
                                ? "●  "
                                : string.Empty)
                            + text,
                        Foreground =
                            new SolidColorBrush(
                                Color.FromRgb(
                                    235,
                                    239,
                                    245)),
                        FontWeight =
                            isActive
                                ? FontWeights.SemiBold
                                : FontWeights.Normal,
                        VerticalAlignment =
                            VerticalAlignment.Center
                    };
            }
            else
            {
                Grid contentGrid =
                    new Grid();

                contentGrid.ColumnDefinitions.Add(
                    new ColumnDefinition
                    {
                        Width =
                            new GridLength(
                                96)
                    });

                contentGrid.ColumnDefinitions.Add(
                    new ColumnDefinition
                    {
                        Width =
                            new GridLength(
                                1,
                                GridUnitType.Star)
                    });

                Border previewBorder =
                    new Border
                    {
                        Width = 88,
                        Height = 66,
                        Margin =
                            new Thickness(
                                0,
                                0,
                                10,
                                0),
                        Background =
                            new SolidColorBrush(
                                Color.FromRgb(
                                    13,
                                    16,
                                    21)),
                        BorderBrush =
                            new SolidColorBrush(
                                Color.FromRgb(
                                    54,
                                    62,
                                    74)),
                        BorderThickness =
                            new Thickness(
                                1),
                        CornerRadius =
                            new CornerRadius(
                                5),
                        ClipToBounds = true
                    };

                if (plate.PreviewImage is not null)
                {
                    previewBorder.Child =
                        new Image
                        {
                            Source =
                                plate.PreviewImage,
                            Stretch =
                                Stretch.Uniform,
                            Margin =
                                new Thickness(
                                    2)
                        };
                }
                else
                {
                    previewBorder.Child =
                        new TextBlock
                        {
                            Text = "3MF",
                            Foreground =
                                new SolidColorBrush(
                                    Color.FromRgb(
                                        126,
                                        136,
                                        151)),
                            FontSize = 11,
                            HorizontalAlignment =
                                HorizontalAlignment.Center,
                            VerticalAlignment =
                                VerticalAlignment.Center
                        };
                }

                Grid.SetColumn(
                    previewBorder,
                    0);

                contentGrid.Children.Add(
                    previewBorder);

                StackPanel textPanel =
                    new StackPanel
                    {
                        VerticalAlignment =
                            VerticalAlignment.Center
                    };

                textPanel.Children.Add(
                    new TextBlock
                    {
                        Text =
                            (isActive
                                ? "●  "
                                : string.Empty)
                            + text,
                        Foreground =
                            new SolidColorBrush(
                                isActive
                                    ? Color.FromRgb(
                                        255,
                                        211,
                                        122)
                                    : Color.FromRgb(
                                        240,
                                        243,
                                        248)),
                        FontWeight =
                            FontWeights.SemiBold,
                        FontSize = 12
                    });

                textPanel.Children.Add(
                    new TextBlock
                    {
                        Text =
                            Localization.IsGerman
                                ? $"{plate.ObjectIds.Count} Objekte"
                                : $"{plate.ObjectIds.Count} objects",
                        Foreground =
                            new SolidColorBrush(
                                Color.FromRgb(
                                    158,
                                    167,
                                    181)),
                        FontSize = 10,
                        Margin =
                            new Thickness(
                                0,
                                3,
                                0,
                                0)
                    });

                string defaultName =
                    $"Plate {plate.PlateId}";

                if (!string.IsNullOrWhiteSpace(
                        plate.Name) &&
                    !string.Equals(
                        plate.Name,
                        defaultName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    textPanel.Children.Add(
                        new TextBlock
                        {
                            Text =
                                plate.Name,
                            Foreground =
                                new SolidColorBrush(
                                    Color.FromRgb(
                                        126,
                                        136,
                                        151)),
                            FontSize = 9,
                            TextTrimming =
                                TextTrimming.CharacterEllipsis,
                            Margin =
                                new Thickness(
                                    0,
                                    2,
                                    0,
                                    0)
                        });
                }

                Grid.SetColumn(
                    textPanel,
                    1);

                contentGrid.Children.Add(
                    textPanel);

                button.Content =
                    contentGrid;
            }

            button.Click +=
                (_, _) =>
                {
                    SelectPlate(
                        plateId);
                };

            PlatesPanel3D.Children.Add(
                button);
        }

        private void ApplySourcePlateLayoutToActivePrinter()
        {
            if (_current3DScene is null)
                return;

            bool showOriginalPlate =
                ChkShowOriginalBuildPlate?.IsChecked == true;

            double offsetX =
                showOriginalPlate
                    ? 0.0
                    : (_activePrinterProfile.Width -
                       _current3DScene.SourceBedWidth)
                      / 2.0;

            double offsetY =
                showOriginalPlate
                    ? 0.0
                    : (_activePrinterProfile.Depth -
                       _current3DScene.SourceBedDepth)
                      / 2.0;

            foreach (ThreeMfSceneObject sceneObject
                     in _current3DScene.Objects)
            {
                sceneObject.Model.Transform =
                    new TranslateTransform3D(
                        offsetX,
                        offsetY,
                        0);
            }
        }

        private Rect3D GetActivePlateBounds()
        {
            Rect3D bounds = Rect3D.Empty;

            foreach (ThreeMfSceneObject item
                     in GetActivePlateObjects())
            {
                if (_hiddenSceneObjects.Contains(item))
                    continue;

                if (bounds.IsEmpty)
                    bounds = item.Bounds;
                else
                    bounds.Union(item.Bounds);
            }

            return bounds;
        }

        private void RefreshActivePlatePresentation()
        {
            ApplySourcePlateLayoutToActivePrinter();

            Rect3D activeBounds =
                GetActivePlateBounds();

            if (activeBounds.IsEmpty)
                return;

            _current3DModelBounds =
                activeBounds;

            _buildPlateModel =
                CreateBuildPlate(activeBounds);

            BuildPlateVisual.Content =
                ChkShowBuildPlate.IsChecked == true
                    ? _buildPlateModel
                    : null;

            BuildVolumeWarningVisual.Content =
                CreateBuildVolumeWarning(
                    activeBounds);

            Update3DModelInfo(
                activeBounds);

            Rect3D viewBounds =
                CombineBounds(
                    activeBounds,
                    _buildPlateModel.Bounds);

            _current3DViewBounds =
                viewBounds;

            Passe3DKameraAn(
                viewBounds);

            _model3DPan =
                new Vector3D();

            Aktualisiere3DAnsicht();
        }

        private void SelectPlate(
            int? plateId)
        {
            if (_current3DScene is null)
                return;

            RestoreObjectHighlight();
            _selectedSceneObject = null;
            _activePlateId = plateId;

            foreach (ThreeMfSceneObject sceneObject
                     in _current3DScene.Objects)
            {
                bool visible =
                    plateId is null ||
                    sceneObject.PlateId == plateId.Value;

                SetSceneObjectVisibility(
                    sceneObject,
                    visible,
                    false);
            }

            _hiddenSceneObjects.RemoveWhere(
                item =>
                    plateId is null);

            AktualisiereObjektliste3D();
            AktualisiereAusgewaehltesObjektInfo3D();
            AktualisierePlattenliste3D();
            PlateMenuPopup.IsOpen = false;

            RefreshActivePlatePresentation();
        }

        private void BtnObjectsMenu_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!BtnObjectsMenu.IsEnabled)
                return;

            ObjectsMenuPopup.IsOpen =
                !ObjectsMenuPopup.IsOpen;
        }

        private void BtnShowAllObjects_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_current3DScene is null)
                return;

            foreach (ThreeMfSceneObject sceneObject
                     in _hiddenSceneObjects.ToList())
            {
                if (!_current3DScene.Model.Children.Contains(
                        sceneObject.Model))
                {
                    _current3DScene.Model.Children.Add(
                        sceneObject.Model);
                }
            }

            _hiddenSceneObjects.Clear();

            AktualisiereObjektliste3D();
            AktualisiereAusgewaehltesObjektInfo3D();
        }

        private void BtnIsolateObject_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_current3DScene is null ||
                _selectedSceneObject is null)
            {
                return;
            }

            IReadOnlyList<ThreeMfSceneObject> visiblePlateObjects =
                GetActivePlateObjects();

            foreach (ThreeMfSceneObject sceneObject
                     in visiblePlateObjects)
            {
                SetSceneObjectVisibility(
                    sceneObject,
                    ReferenceEquals(
                        sceneObject,
                        _selectedSceneObject),
                    false);
            }

            AktualisiereObjektliste3D();
            AktualisiereAusgewaehltesObjektInfo3D();

            ObjectsMenuPopup.IsOpen = false;
        }

        private void BtnFocusObject_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_selectedSceneObject is null)
                return;

            SetSceneObjectVisibility(
                _selectedSceneObject,
                true,
                false);

            Rect3D bounds =
                _selectedSceneObject.Bounds;

            if (bounds.IsEmpty)
                return;

            Passe3DKameraAn(bounds);
            _model3DPan = new Vector3D();

            Aktualisiere3DAnsicht();

            AktualisiereObjektliste3D();
            AktualisiereAusgewaehltesObjektInfo3D();

            ObjectsMenuPopup.IsOpen = false;
        }

        private void ResetObjectControls3D()
        {
            RestoreObjectHighlight();

            _current3DScene = null;
            _activePlateId = null;
            _selectedSceneObject = null;
            _hiddenSceneObjects.Clear();

            if (PlatesPanel3D != null)
                PlatesPanel3D.Children.Clear();

            if (PlateMenuPopup != null)
                PlateMenuPopup.IsOpen = false;

            if (BtnPlateMenu != null)
                BtnPlateMenu.IsEnabled = false;

            if (ObjectsPanel3D != null)
                ObjectsPanel3D.Children.Clear();

            if (TxtObjectsSummary3D != null)
                TxtObjectsSummary3D.Text = string.Empty;

            if (TxtSelectedObjectInfo3D != null)
                TxtSelectedObjectInfo3D.Text = string.Empty;

            if (ObjectsMenuPopup != null)
                ObjectsMenuPopup.IsOpen = false;

            if (BtnObjectsMenu != null)
                BtnObjectsMenu.IsEnabled = false;

            if (BtnFocusObject != null)
                BtnFocusObject.IsEnabled = false;

            if (BtnIsolateObject != null)
                BtnIsolateObject.IsEnabled = false;

            if (BtnShowAllObjects != null)
                BtnShowAllObjects.IsEnabled = false;
        }

        private void AktualisiereObjektliste3D()
        {
            AktualisierePlattenliste3D();
            if (ObjectsPanel3D == null ||
                TxtObjectsSummary3D == null)
            {
                return;
            }

            ObjectsPanel3D.Children.Clear();

            IReadOnlyList<ThreeMfSceneObject> visiblePlateObjects =
                GetActivePlateObjects();

            int count =
                visiblePlateObjects.Count;

            TxtObjectsSummary3D.Text =
                count switch
                {
                    0 => string.Empty,
                    1 => Localization.Get("OneObject"),
                    _ => string.Format(
                        Localization.Get("ObjectCount"),
                        count)
                };

            BtnObjectsMenu.IsEnabled =
                count > 0;

            BtnFocusObject.IsEnabled =
                _selectedSceneObject is not null;

            BtnIsolateObject.IsEnabled =
                _selectedSceneObject is not null &&
                count > 1;

            BtnShowAllObjects.IsEnabled =
                _hiddenSceneObjects.Count > 0;

            if (_current3DScene is null)
                return;

            foreach (ThreeMfSceneObject sceneObject
                     in _current3DScene.Objects)
            {
                bool isVisible =
                    !_hiddenSceneObjects.Contains(
                        sceneObject);

                bool isSelected =
                    ReferenceEquals(
                        _selectedSceneObject,
                        sceneObject);

                Grid row = new Grid
                {
                    Margin =
                        new Thickness(
                            1,
                            1,
                            1,
                            2)
                };

                row.ColumnDefinitions.Add(
                    new ColumnDefinition
                    {
                        Width =
                            new GridLength(28)
                    });

                row.ColumnDefinitions.Add(
                    new ColumnDefinition
                    {
                        Width =
                            new GridLength(
                                1,
                                GridUnitType.Star)
                    });

                CheckBox visibilityCheckBox =
                    new CheckBox
                    {
                        IsChecked = isVisible,
                        VerticalAlignment =
                            VerticalAlignment.Center,
                        HorizontalAlignment =
                            HorizontalAlignment.Center,
                        ToolTip =
                            isVisible
                                ? Localization.Get("HideObject")
                                : Localization.Get("ShowObject"),
                        Tag = sceneObject
                    };

                visibilityCheckBox.Checked +=
                    (s, e) =>
                    {
                        if (s is CheckBox check &&
                            check.Tag
                                is ThreeMfSceneObject item)
                        {
                            SetSceneObjectVisibility(
                                item,
                                true);
                        }
                    };

                visibilityCheckBox.Unchecked +=
                    (s, e) =>
                    {
                        if (s is CheckBox check &&
                            check.Tag
                                is ThreeMfSceneObject item)
                        {
                            SetSceneObjectVisibility(
                                item,
                                false);
                        }
                    };

                Button selectButton =
                    new Button
                    {
                        Content =
                            GetSceneObjectDisplayText(
                                sceneObject,
                                isVisible,
                                isSelected),
                        Tag = sceneObject,
                        Padding =
                            new Thickness(
                                7,
                                5,
                                7,
                                5),
                        HorizontalContentAlignment =
                            HorizontalAlignment.Left,
                        FontSize = 10,
                        ToolTip =
                            Localization.Get(
                                "SelectObject")
                    };

                List<string> exceededAxes =
                    GetObjectExceededAxes(
                        sceneObject.Bounds);

                if (exceededAxes.Count > 0)
                {
                    selectButton.Foreground =
                        new SolidColorBrush(
                            Color.FromRgb(
                                255,
                                190,
                                120));

                    selectButton.ToolTip =
                        string.Format(
                            Localization.Get(
                                "ObjectExceeds"),
                            string.Join(
                                ", ",
                                exceededAxes));
                }

                if (isSelected)
                {
                    selectButton.Background =
                        new SolidColorBrush(
                            Color.FromRgb(
                                44,
                                64,
                                94));

                    selectButton.BorderBrush =
                        new SolidColorBrush(
                            exceededAxes.Count > 0
                                ? Color.FromRgb(
                                    255,
                                    166,
                                    77)
                                : Color.FromRgb(
                                    76,
                                    141,
                                    255));
                }
                else if (exceededAxes.Count > 0)
                {
                    selectButton.Background =
                        new SolidColorBrush(
                            Color.FromRgb(
                                55,
                                40,
                                27));

                    selectButton.BorderBrush =
                        new SolidColorBrush(
                            Color.FromRgb(
                                150,
                                92,
                                38));
                }

                selectButton.Click +=
                    (s, e) =>
                    {
                        if (s is Button button &&
                            button.Tag
                                is ThreeMfSceneObject item)
                        {
                            SelectSceneObject(
                                item);
                        }
                    };

                Grid.SetColumn(
                    visibilityCheckBox,
                    0);

                Grid.SetColumn(
                    selectButton,
                    1);

                row.Children.Add(
                    visibilityCheckBox);

                row.Children.Add(
                    selectButton);

                ObjectsPanel3D.Children.Add(
                    row);
            }
        }

        private string GetSceneObjectDisplayText(
            ThreeMfSceneObject sceneObject,
            bool isVisible,
            bool isSelected)
        {
            string name =
                GetSceneObjectName(
                    sceneObject);

            string prefix =
                isSelected
                    ? "● "
                    : string.Empty;

            string suffix =
                isVisible
                    ? string.Empty
                    : $"  ({Localization.Get("Hidden")})";

            List<string> exceededAxes =
                GetObjectExceededAxes(
                    sceneObject.Bounds);

            string buildVolumeStatus =
                exceededAxes.Count == 0
                    ? string.Empty
                    : $"  ⚠ {string.Join("/", exceededAxes)}";

            return
                $"{prefix}{sceneObject.DisplayIndex}. {name}" +
                $"{buildVolumeStatus}{suffix}";
        }

        private string GetSceneObjectName(
            ThreeMfSceneObject sceneObject)
        {
            if (!string.IsNullOrWhiteSpace(
                    sceneObject.Name))
            {
                return sceneObject.Name;
            }

            return
                Localization.Get("Object");
        }

        private void SelectSceneObject(
            ThreeMfSceneObject sceneObject)
        {
            RestoreObjectHighlight();

            _selectedSceneObject =
                sceneObject;

            ApplyObjectHighlight(
                sceneObject);

            AktualisiereObjektliste3D();
            AktualisiereAusgewaehltesObjektInfo3D();
        }

        private void SetSceneObjectVisibility(
            ThreeMfSceneObject sceneObject,
            bool visible,
            bool refreshUi = true)
        {
            if (_current3DScene is null)
                return;

            if (visible)
            {
                _hiddenSceneObjects.Remove(
                    sceneObject);

                if (!_current3DScene.Model.Children.Contains(
                        sceneObject.Model))
                {
                    _current3DScene.Model.Children.Add(
                        sceneObject.Model);
                }
            }
            else
            {
                _hiddenSceneObjects.Add(
                    sceneObject);

                if (_current3DScene.Model.Children.Contains(
                        sceneObject.Model))
                {
                    _current3DScene.Model.Children.Remove(
                        sceneObject.Model);
                }
            }

            if (refreshUi)
            {
                AktualisiereObjektliste3D();
                AktualisiereAusgewaehltesObjektInfo3D();
            }
        }

        private void ApplyObjectHighlight(
            ThreeMfSceneObject sceneObject)
        {
            MaterialGroup selectedMaterial =
                new MaterialGroup();

            selectedMaterial.Children.Add(
                new DiffuseMaterial(
                    new SolidColorBrush(
                        Color.FromRgb(
                            255,
                            151,
                            70))));

            selectedMaterial.Children.Add(
                new EmissiveMaterial(
                    new SolidColorBrush(
                        Color.FromArgb(
                            70,
                            255,
                            120,
                            35))));

            selectedMaterial.Children.Add(
                new SpecularMaterial(
                    new SolidColorBrush(
                        Color.FromRgb(
                            255,
                            220,
                            180)),
                    45));

            foreach (GeometryModel3D geometry
                     in EnumerateGeometryModels(
                         sceneObject.Model))
            {
                if (!_highlightedObjectMaterials.ContainsKey(
                        geometry))
                {
                    _highlightedObjectMaterials[geometry] =
                        (
                            geometry.Material,
                            geometry.BackMaterial
                        );
                }

                geometry.Material =
                    selectedMaterial;

                geometry.BackMaterial =
                    selectedMaterial;
            }
        }

        private void RestoreObjectHighlight()
        {
            foreach (KeyValuePair<
                         GeometryModel3D,
                         (Material? Material, Material? BackMaterial)>
                     pair
                     in _highlightedObjectMaterials)
            {
                pair.Key.Material =
                    pair.Value.Material;

                pair.Key.BackMaterial =
                    pair.Value.BackMaterial;
            }

            _highlightedObjectMaterials.Clear();
        }

        private static IEnumerable<GeometryModel3D>
            EnumerateGeometryModels(
                Model3D model)
        {
            if (model is GeometryModel3D geometry)
            {
                yield return geometry;
                yield break;
            }

            if (model is not Model3DGroup group)
                yield break;

            foreach (Model3D child
                     in group.Children)
            {
                foreach (GeometryModel3D nested
                         in EnumerateGeometryModels(
                             child))
                {
                    yield return nested;
                }
            }
        }

        private void AktualisiereAusgewaehltesObjektInfo3D()
        {
            if (TxtSelectedObjectInfo3D == null)
                return;

            if (_selectedSceneObject is null)
            {
                TxtSelectedObjectInfo3D.Text =
                    string.Empty;

                return;
            }

            Rect3D bounds =
                _selectedSceneObject.Bounds;

            string name =
                $"{_selectedSceneObject.DisplayIndex}. " +
                GetSceneObjectName(
                    _selectedSceneObject);

            string size =
                $"{Math.Max(0, bounds.SizeX):0.0} × " +
                $"{Math.Max(0, bounds.SizeY):0.0} × " +
                $"{Math.Max(0, bounds.SizeZ):0.0} mm";

            var effectiveZ =
                GetEffectiveZRange(
                    bounds);

            string position =
                $"X {bounds.X:0.0}–{bounds.X + bounds.SizeX:0.0}  ·  " +
                $"Y {bounds.Y:0.0}–{bounds.Y + bounds.SizeY:0.0}  ·  " +
                $"Z {effectiveZ.Bottom:0.0}–{effectiveZ.Top:0.0} mm";

            List<string> exceededAxes =
                GetObjectExceededAxes(
                    bounds);

            string fitText =
                exceededAxes.Count == 0
                    ? Localization.Get("ObjectFits")
                    : string.Format(
                        Localization.Get(
                            "ObjectExceeds"),
                        string.Join(
                            ", ",
                            exceededAxes));

            TxtSelectedObjectInfo3D.Text =
                $"{name}\n" +
                $"{string.Format(Localization.Get("ObjectSize"), size)}\n" +
                $"{string.Format(Localization.Get("ObjectPosition"), position)}\n" +
                $"{string.Format(Localization.Get("ObjectTriangles"), _selectedSceneObject.TriangleCount)}\n" +
                fitText;

            TxtSelectedObjectInfo3D.Foreground =
                new SolidColorBrush(
                    exceededAxes.Count == 0
                        ? Color.FromRgb(
                            185,
                            230,
                            196)
                        : Color.FromRgb(
                            255,
                            190,
                            120));
        }

        private (
            double Bottom,
            double Top,
            double Height)
            GetEffectiveZRange(
                Rect3D bounds)
        {
            double height =
                Math.Max(
                    0,
                    bounds.SizeZ);

            double bottom =
                bounds.Z;

            // Bambu 3MFs can contain small negative/positive base
            // offsets from nested component transforms. If the model
            // is effectively sitting on the plate, treat its base as Z=0.
            if (Math.Abs(bottom) <= BuildPlateSnapTolerance)
                bottom = 0.0;

            double top =
                bottom + height;

            return (
                bottom,
                top,
                height);
        }

        private bool FitsActivePrinterZ(
            Rect3D bounds)
        {
            var z =
                GetEffectiveZRange(
                    bounds);

            return
                z.Bottom >= -BuildVolumeTolerance &&
                z.Top <= _activePrinterProfile.Height
                         + BuildVolumeTolerance;
        }

        private List<string> GetObjectExceededAxes(
            Rect3D bounds)
        {
            List<string> result = new();

            bool fitsX =
                bounds.X >= -0.001 &&
                bounds.X + bounds.SizeX
                    <= _activePrinterProfile.Width
                       + 0.001;

            bool fitsY =
                bounds.Y >= -0.001 &&
                bounds.Y + bounds.SizeY
                    <= _activePrinterProfile.Depth
                       + 0.001;

            bool fitsZ =
                FitsActivePrinterZ(
                    bounds);

            if (!fitsX)
                result.Add("X");

            if (!fitsY)
                result.Add("Y");

            if (!fitsZ)
                result.Add("Z");

            return result;
        }

        private void Btn3DView_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not string view)
                return;

            ViewMenuPopup.IsOpen = false;
            Set3DView(view);
        }

        private void Set3DView(string view)
        {
            if (ModelVisual.Content is null)
                return;

            if (view == "Reset")
            {
                Reset3DView();
                return;
            }

            _model3DPan = new Vector3D();
            _model3DDistance = _model3DInitialDistance;

            switch (view)
            {
                case "Top":
                    _model3DYaw = 0;
                    _model3DPitch = 0;
                    break;
                case "Front":
                    _model3DYaw = 0;
                    _model3DPitch = 90;
                    break;
                case "Back":
                    _model3DYaw = 0;
                    _model3DPitch = -90;
                    break;
                case "Left":
                    _model3DYaw = -90;
                    _model3DPitch = 0;
                    break;
                case "Right":
                    _model3DYaw = 90;
                    _model3DPitch = 0;
                    break;
                case "Iso":
                default:
                    _model3DYaw = -35;
                    _model3DPitch = 25;
                    break;
            }

            Aktualisiere3DAnsicht();
        }

        private void BtnPreview2D_Click(object sender, RoutedEventArgs e)
        {
            ImgPreview.Visibility = Visibility.Visible;
            ModelViewport.Visibility = Visibility.Collapsed;
            Txt3DHintContainer.Visibility = Visibility.Collapsed;
            OrientationGizmoContainer.Visibility = Visibility.Collapsed;
            PrinterControls3D.Visibility = Visibility.Visible;
            PrinterMenuPopup.IsOpen = false;
            ViewMenuPopup.IsOpen = false;
            ObjectsMenuPopup.IsOpen = false;
            BtnViewMenu.IsEnabled = false;
            ResetObjectControls3D();
            ModelInfo3DContainer.Visibility = Visibility.Collapsed;
            BuildVolumeWarningVisual.Content = null;
        }

        private void BtnPreview3D_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedFile is null ||
                !File.Exists(_selectedFile.FullPath) ||
                _isLoading3D)
            {
                return;
            }

            _isLoading3D = true;

            BtnPreview3D.IsEnabled = false;
            BtnPreviewLarge.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;

            try
            {
                ResetObjectControls3D();

                ThreeMfScene scene;

                bool canReuseLoadedScene =
                    _current3DScene is not null &&
                    string.Equals(
                        _loaded3DFilePath,
                        _selectedFile.FullPath,
                        StringComparison.OrdinalIgnoreCase);

                if (canReuseLoadedScene)
                {
                    scene = _current3DScene!;
                }
                else
                {
                    // Release an older scene before constructing a new one.
                    RestoreObjectHighlight();

                    ModelVisual.Content = null;
                    BuildPlateVisual.Content = null;
                    BuildVolumeWarningVisual.Content = null;

                    _current3DScene = null;
                    _loaded3DFilePath = null;
                    _selectedSceneObject = null;
                    _hiddenSceneObjects.Clear();
                    _buildPlateModel = null;
                    _current3DModelBounds = Rect3D.Empty;
                    _current3DViewBounds = Rect3D.Empty;

                    GC.Collect(
                        1,
                        GCCollectionMode.Forced,
                        blocking: true,
                        compacting: false);

                    scene =
                        ThreeMfModelLoader.LoadScene(
                            _selectedFile.FullPath);

                    _current3DScene = scene;
                    _loaded3DFilePath =
                        _selectedFile.FullPath;

                    AddExtraLighting(
                        scene.Model);
                }

                ApplySourcePlateLayoutToActivePrinter();

                Model3DGroup model =
                    scene.Model;

                Rect3D modelBounds =
                    model.Bounds;

                _current3DModelBounds =
                    modelBounds;

                _buildPlateModel =
                    CreateBuildPlate(
                        modelBounds);

                ModelVisual.Content = model;

                BuildPlateVisual.Content =
                    ChkShowBuildPlate.IsChecked == true
                        ? _buildPlateModel
                        : null;

                BuildVolumeWarningVisual.Content =
                    CreateBuildVolumeWarning(
                        modelBounds);

                Rect3D viewBounds =
                    CombineBounds(
                        modelBounds,
                        _buildPlateModel.Bounds);

                _current3DViewBounds =
                    viewBounds;

                Passe3DKameraAn(
                    viewBounds);

                Reset3DView();

                AktualisiereObjektliste3D();
                AktualisiereAusgewaehltesObjektInfo3D();
                AktualisierePlattenliste3D();

                ImgPreview.Visibility =
                    Visibility.Collapsed;

                ModelViewport.Visibility =
                    Visibility.Visible;

                Txt3DHintContainer.Visibility =
                    Visibility.Visible;

                OrientationGizmoContainer.Visibility =
                    Visibility.Visible;

                PrinterControls3D.Visibility =
                    Visibility.Visible;

                BtnViewMenu.IsEnabled = true;

                Update3DModelInfo(
                    modelBounds);

                ModelInfo3DContainer.Visibility =
                    Visibility.Visible;

                ModelViewport.Focus();
            }
            catch (Exception ex)
            {
                ModelVisual.Content = null;
                BuildPlateVisual.Content = null;
                BuildVolumeWarningVisual.Content = null;

                _current3DScene = null;
                _loaded3DFilePath = null;
                _selectedSceneObject = null;
                _hiddenSceneObjects.Clear();
                _buildPlateModel = null;
                _current3DModelBounds = Rect3D.Empty;
                _current3DViewBounds = Rect3D.Empty;

                ImgPreview.Visibility =
                    Visibility.Visible;

                ModelViewport.Visibility =
                    Visibility.Collapsed;

                Txt3DHintContainer.Visibility =
                    Visibility.Collapsed;

                OrientationGizmoContainer.Visibility =
                    Visibility.Collapsed;

                PrinterControls3D.Visibility =
                    Visibility.Visible;

                ViewMenuPopup.IsOpen = false;
                BtnViewMenu.IsEnabled = false;

                ResetObjectControls3D();

                ModelInfo3DContainer.Visibility =
                    Visibility.Collapsed;

                MessageBox.Show(
                    Localization.IsGerman
                        ? $"3D-Modell konnte nicht geladen werden.\n\n{ex}"
                        : $"The 3D model could not be loaded.\n\n{ex}",
                    "3MF-Explorer",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            finally
            {
                Mouse.OverrideCursor = null;
                BtnPreview3D.IsEnabled = true;
                BtnPreviewLarge.IsEnabled = true;
                _isLoading3D = false;
            }
        }

        private void Update3DModelInfo(Rect3D bounds)
        {
            double x = Math.Max(0, bounds.SizeX);
            double y = Math.Max(0, bounds.SizeY);
            double z = Math.Max(0, bounds.SizeZ);

            bool fitsX =
                bounds.X >= -BuildVolumeTolerance &&
                bounds.X + bounds.SizeX
                    <= _activePrinterProfile.Width
                       + BuildVolumeTolerance;

            bool fitsY =
                bounds.Y >= -BuildVolumeTolerance &&
                bounds.Y + bounds.SizeY
                    <= _activePrinterProfile.Depth
                       + BuildVolumeTolerance;

            bool fitsZ =
                FitsActivePrinterZ(
                    bounds);
            bool fits = fitsX && fitsY && fitsZ;

            string size = $"{x:0.0} × {y:0.0} × {z:0.0} mm";
            string volume = $"{_activePrinterProfile.Width:0} × {_activePrinterProfile.Depth:0} × {_activePrinterProfile.Height:0} mm";

            string sourcePrinterName =
                string.IsNullOrWhiteSpace(
                    _current3DScene?.SourcePrinterName)
                    ? (Localization.IsGerman
                        ? "3MF-Projekt"
                        : "3MF project")
                    : _current3DScene!.SourcePrinterName;

            string sourceVolume =
                _current3DScene is null
                    ? "–"
                    : $"{_current3DScene.SourceBedWidth:0} × " +
                      $"{_current3DScene.SourceBedDepth:0} × " +
                      $"{_current3DScene.SourceBedHeight:0} mm";

            TxtPrinterProfile3D.Text =
                Localization.IsGerman
                    ? $"Projekt: {sourcePrinterName}\n" +
                      $"Prüfung: {_activePrinterProfile.Name}"
                    : $"Project: {sourcePrinterName}\n" +
                      $"Check: {_activePrinterProfile.Name}";

            TxtModelSize3D.Text =
                Localization.IsGerman
                    ? $"Belegt: {size}"
                    : $"Occupied: {size}";

            var effectiveZ =
                GetEffectiveZRange(
                    bounds);

            string position =
                $"X {bounds.X:0.0}–{bounds.X + bounds.SizeX:0.0}  ·  " +
                $"Y {bounds.Y:0.0}–{bounds.Y + bounds.SizeY:0.0}  ·  " +
                $"Z {effectiveZ.Bottom:0.0}–{effectiveZ.Top:0.0} mm";

            TxtBuildVolume3D.Text =
                Localization.IsGerman
                    ? $"Projektplatte: {sourceVolume}\n" +
                      $"Ziel-Bauraum: {volume}\n" +
                      $"Position: {position}"
                    : $"Project plate: {sourceVolume}\n" +
                      $"Target build volume: {volume}\n" +
                      $"Position: {position}";

            if (fits)
            {
                TxtFitStatus3D.Text = Localization.IsGerman
                    ? "✓ Modell passt in den Bauraum"
                    : "✓ Model fits within the build volume";
                TxtFitStatus3D.Foreground = new SolidColorBrush(Color.FromRgb(185, 230, 196));
                return;
            }

            List<string> exceeded = new();
            if (!fitsX)
            {
                double over = Math.Max(Math.Max(-bounds.X, 0), Math.Max(bounds.X + bounds.SizeX - _activePrinterProfile.Width, 0));
                exceeded.Add(Localization.IsGerman ? $"X +{over:0.0} mm außerhalb" : $"X +{over:0.0} mm outside");
            }
            if (!fitsY)
            {
                double over = Math.Max(Math.Max(-bounds.Y, 0), Math.Max(bounds.Y + bounds.SizeY - _activePrinterProfile.Depth, 0));
                exceeded.Add(Localization.IsGerman ? $"Y +{over:0.0} mm außerhalb" : $"Y +{over:0.0} mm outside");
            }
            if (!fitsZ)
            {
                var effectiveZRange =
                    GetEffectiveZRange(
                        bounds);

                double over =
                    Math.Max(
                        Math.Max(
                            -effectiveZRange.Bottom,
                            0),
                        Math.Max(
                            effectiveZRange.Top -
                            _activePrinterProfile.Height,
                            0));
                exceeded.Add(Localization.IsGerman ? $"Z +{over:0.0} mm außerhalb" : $"Z +{over:0.0} mm outside");
            }

            TxtFitStatus3D.Text = (Localization.IsGerman ? "⚠ Überschreitung: " : "⚠ Exceeds: ") + string.Join(" · ", exceeded);
            TxtFitStatus3D.Foreground = new SolidColorBrush(Color.FromRgb(255, 190, 120));
        }

        private void ImgPreview_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount != 2)
                return;

            if (_selectedFile is null || !File.Exists(_selectedFile.FullPath))
                return;

            BtnPreviewLarge_Click(BtnPreviewLarge, new RoutedEventArgs());
            e.Handled = true;
        }

        private void BtnPreviewLarge_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedFile is null ||
                !File.Exists(_selectedFile.FullPath))
            {
                return;
            }

            bool show3D =
                ModelViewport.Visibility ==
                Visibility.Visible;

            bool reloadMain3DAfterClose =
                show3D;

            ThreeMfScene? sceneForLargePreview =
                show3D
                    ? _current3DScene
                    : null;

            int? plateForLargePreview =
                show3D
                    ? _activePlateId
                    : null;

            try
            {
                if (show3D)
                {
                    // Die bereits geladene 3D-Szene wird an das große
                    // Vorschaufenster übergeben. Dadurch wird dasselbe
                    // komplexe Modell nicht ein zweites Mal aus der 3MF
                    // aufgebaut, was bei großen Modellen zu sehr hohem
                    // Speicherverbrauch und Abstürzen führen konnte.
                    RestoreObjectHighlight();

                    ModelVisual.Content = null;
                    BuildPlateVisual.Content = null;
                    BuildVolumeWarningVisual.Content = null;

                    _current3DScene = null;
                    _loaded3DFilePath = null;
                    _selectedSceneObject = null;
                    _hiddenSceneObjects.Clear();
                    _buildPlateModel = null;
                    _current3DModelBounds = Rect3D.Empty;
                    _current3DViewBounds = Rect3D.Empty;

                    if (PlatesPanel3D != null)
                        PlatesPanel3D.Children.Clear();

                    if (ObjectsPanel3D != null)
                        ObjectsPanel3D.Children.Clear();

                    if (PlateMenuPopup != null)
                        PlateMenuPopup.IsOpen = false;

                    if (ObjectsMenuPopup != null)
                        ObjectsMenuPopup.IsOpen = false;

                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }

                PreviewWindow previewWindow =
                    new PreviewWindow(
                        _selectedFile.FullPath,
                        show3D,
                        _activePrinterProfile.Id,
                        sceneForLargePreview,
                        plateForLargePreview)
                    {
                        Owner = this
                    };

                previewWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    Localization.IsGerman
                        ? $"Große Vorschau konnte nicht geöffnet werden.\n\n{ex}"
                        : $"Large preview could not be opened.\n\n{ex}",
                    "3MF-Explorer",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            finally
            {
                // Referenz auf die an das Vorschaufenster übergebene
                // Szene freigeben, bevor die normale 3D-Ansicht
                // wieder aufgebaut wird.
                sceneForLargePreview = null;

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                PrinterProfile savedProfile =
                    PrinterProfile.LoadSelected();

                if (!string.Equals(
                        savedProfile.Id,
                        _activePrinterProfile.Id,
                        StringComparison.OrdinalIgnoreCase))
                {
                    _printerProfileInitialized = false;
                    _activePrinterProfile = savedProfile;
                    CmbPrinterProfile.SelectedItem =
                        PrinterProfile.All.FirstOrDefault(
                            profile =>
                                profile.Id ==
                                savedProfile.Id)
                        ?? PrinterProfile.BambuLabX1C;
                    _printerProfileInitialized = true;
                }

                if (reloadMain3DAfterClose &&
                    _selectedFile is not null &&
                    File.Exists(_selectedFile.FullPath))
                {
                    BtnPreview3D_Click(
                        BtnPreview3D,
                        new RoutedEventArgs());
                }
            }
        }

        private Model3DGroup? CreateBuildVolumeWarning(Rect3D bounds)
        {
            var effectiveZ =
                GetEffectiveZRange(
                    bounds);

            bool left =
                bounds.X < -BuildVolumeTolerance;

            bool right =
                bounds.X + bounds.SizeX >
                _activePrinterProfile.Width
                + BuildVolumeTolerance;

            bool front =
                bounds.Y < -BuildVolumeTolerance;

            bool back =
                bounds.Y + bounds.SizeY >
                _activePrinterProfile.Depth
                + BuildVolumeTolerance;

            bool below =
                effectiveZ.Bottom <
                -BuildVolumeTolerance;

            bool above =
                effectiveZ.Top >
                _activePrinterProfile.Height
                + BuildVolumeTolerance;

            if (!left && !right && !front && !back && !below && !above)
                return null;

            Model3DGroup group = new Model3DGroup();
            MaterialGroup warningMaterial = new MaterialGroup();
            warningMaterial.Children.Add(new DiffuseMaterial(
                new SolidColorBrush(Color.FromArgb(230, 220, 55, 55))));
            warningMaterial.Children.Add(new EmissiveMaterial(
                new SolidColorBrush(Color.FromArgb(110, 255, 45, 45))));

            double w = _activePrinterProfile.Width;
            double d = _activePrinterProfile.Depth;
            double edge = Math.Max(1.5, Math.Min(w, d) * 0.008);
            double z0 = 0.35;
            double z1 = 2.0;

            void AddBox(double minX, double maxX, double minY, double maxY, double minZ, double maxZ)
            {
                GeometryModel3D item = new GeometryModel3D
                {
                    Geometry = CreateBoxMesh(minX, maxX, minY, maxY, minZ, maxZ),
                    Material = warningMaterial,
                    BackMaterial = warningMaterial
                };
                group.Children.Add(item);
            }

            if (left) AddBox(-edge / 2, edge / 2, 0, d, z0, z1);
            if (right) AddBox(w - edge / 2, w + edge / 2, 0, d, z0, z1);
            if (front) AddBox(0, w, -edge / 2, edge / 2, z0, z1);
            if (back) AddBox(0, w, d - edge / 2, d + edge / 2, z0, z1);

            if (below)
            {
                AddBox(0, w, 0, d, -0.9, -0.25);
            }

            if (above)
            {
                MeshGeometry3D plane = new MeshGeometry3D();
                double z = _activePrinterProfile.Height;
                plane.Positions.Add(new Point3D(0, 0, z));
                plane.Positions.Add(new Point3D(w, 0, z));
                plane.Positions.Add(new Point3D(w, d, z));
                plane.Positions.Add(new Point3D(0, d, z));
                plane.TriangleIndices.Add(0);
                plane.TriangleIndices.Add(1);
                plane.TriangleIndices.Add(2);
                plane.TriangleIndices.Add(0);
                plane.TriangleIndices.Add(2);
                plane.TriangleIndices.Add(3);

                DiffuseMaterial heightMaterial = new DiffuseMaterial(
                    new SolidColorBrush(Color.FromArgb(48, 255, 60, 60)));
                GeometryModel3D heightPlane = new GeometryModel3D
                {
                    Geometry = plane,
                    Material = heightMaterial,
                    BackMaterial = heightMaterial
                };
                group.Children.Add(heightPlane);

                double line = Math.Max(1.2, edge * 0.75);
                AddBox(0, w, -line / 2, line / 2, z - line / 2, z + line / 2);
                AddBox(0, w, d - line / 2, d + line / 2, z - line / 2, z + line / 2);
                AddBox(-line / 2, line / 2, 0, d, z - line / 2, z + line / 2);
                AddBox(w - line / 2, w + line / 2, 0, d, z - line / 2, z + line / 2);
            }

            return group;
        }

        private Model3DGroup CreateBuildPlate(Rect3D modelBounds)
        {
            double topZ = 0.0;
            double bottomZ = -BuildPlateThickness;

            // Bambu/3MF build coordinates use the plate origin at X=0 / Y=0.
            // Keep the plate fixed so saved build-item transforms are visible at their real positions.
            bool showOriginalPlate =
                ChkShowOriginalBuildPlate?.IsChecked == true &&
                _current3DScene is not null;

            double plateWidth =
                showOriginalPlate
                    ? _current3DScene!.SourceBedWidth
                    : _activePrinterProfile.Width;

            double plateDepth =
                showOriginalPlate
                    ? _current3DScene!.SourceBedDepth
                    : _activePrinterProfile.Depth;

            string plateName =
                showOriginalPlate
                    ? (string.IsNullOrWhiteSpace(
                           _current3DScene!.SourcePrinterName)
                        ? "3MF"
                        : _current3DScene.SourcePrinterName)
                    : _activePrinterProfile.Name;

            double minX = 0.0;
            double maxX = plateWidth;
            double minY = 0.0;
            double maxY = plateDepth;

            Model3DGroup plateGroup = new Model3DGroup();

            MeshGeometry3D topMesh = new MeshGeometry3D();
            topMesh.Positions.Add(new Point3D(minX, minY, topZ));
            topMesh.Positions.Add(new Point3D(maxX, minY, topZ));
            topMesh.Positions.Add(new Point3D(maxX, maxY, topZ));
            topMesh.Positions.Add(new Point3D(minX, maxY, topZ));
            topMesh.TextureCoordinates.Add(new Point(0, 1));
            topMesh.TextureCoordinates.Add(new Point(1, 1));
            topMesh.TextureCoordinates.Add(new Point(1, 0));
            topMesh.TextureCoordinates.Add(new Point(0, 0));
            topMesh.TriangleIndices.Add(0);
            topMesh.TriangleIndices.Add(1);
            topMesh.TriangleIndices.Add(2);
            topMesh.TriangleIndices.Add(0);
            topMesh.TriangleIndices.Add(2);
            topMesh.TriangleIndices.Add(3);

            DiffuseMaterial texturedTop =
                new DiffuseMaterial(
                    CreateRoughPlateBrush(
                        plateWidth,
                        plateDepth,
                        plateName));
            GeometryModel3D top = new GeometryModel3D
            {
                Geometry = topMesh,
                Material = texturedTop,
                BackMaterial = texturedTop
            };
            plateGroup.Children.Add(top);

            MeshGeometry3D bodyMesh = CreateBoxMesh(minX, maxX, minY, maxY, bottomZ, topZ);
            MaterialGroup bodyMaterial = new MaterialGroup();
            bodyMaterial.Children.Add(new DiffuseMaterial(
                new SolidColorBrush(Color.FromRgb(35, 39, 46))));
            bodyMaterial.Children.Add(new SpecularMaterial(
                new SolidColorBrush(Color.FromRgb(70, 75, 84)), 12));

            GeometryModel3D body = new GeometryModel3D
            {
                Geometry = bodyMesh,
                Material = bodyMaterial,
                BackMaterial = bodyMaterial
            };
            plateGroup.Children.Add(body);

            return plateGroup;
        }

        private static Brush CreateRoughPlateBrush(
            double plateWidth,
            double plateDepth,
            string plateName)
        {
            DrawingGroup drawing = new DrawingGroup();

            // Dunkle, leicht raue Grundfläche.
            drawing.Children.Add(new GeometryDrawing(
                new SolidColorBrush(Color.FromRgb(50, 54, 61)),
                null,
                new RectangleGeometry(new Rect(0, 0, plateWidth, plateDepth))));

            // Technisches Raster: 10 mm, alle 50 mm etwas kräftiger.
            Pen minorGridPen = new Pen(
                new SolidColorBrush(Color.FromArgb(58, 205, 210, 216)), 0.32);
            Pen majorGridPen = new Pen(
                new SolidColorBrush(Color.FromArgb(115, 220, 224, 230)), 0.62);

            for (double p = 10; p < plateWidth; p += 10)
            {
                bool major = Math.Abs(p % 50) < 0.001;
                Pen pen = major ? majorGridPen : minorGridPen;
                drawing.Children.Add(new GeometryDrawing(
                    null, pen,
                    new LineGeometry(new Point(p, 0), new Point(p, plateDepth))));
            }

            for (double p = 10; p < plateDepth; p += 10)
            {
                bool major = Math.Abs(p % 50) < 0.001;
                Pen pen = major ? majorGridPen : minorGridPen;
                drawing.Children.Add(new GeometryDrawing(
                    null, pen,
                    new LineGeometry(new Point(0, p), new Point(plateWidth, p))));
            }

            // Außenkante der nutzbaren 256 x 256 mm Fläche.
            drawing.Children.Add(new GeometryDrawing(
                null,
                new Pen(new SolidColorBrush(Color.FromArgb(170, 230, 233, 238)), 0.85),
                new RectangleGeometry(new Rect(0.45, 0.45, plateWidth - 0.9, plateDepth - 0.9))));

            // Mittelpunkt bei X=128 / Y=128 mm.
            double centerX = plateWidth / 2.0;
            double centerY = plateDepth / 2.0;
            Pen centerPen = new Pen(
                new SolidColorBrush(Color.FromArgb(190, 235, 238, 242)), 0.75);
            drawing.Children.Add(new GeometryDrawing(
                null, centerPen,
                new LineGeometry(new Point(centerX - 6, centerY), new Point(centerX + 6, centerY))));
            drawing.Children.Add(new GeometryDrawing(
                null, centerPen,
                new LineGeometry(new Point(centerX, centerY - 6), new Point(centerX, centerY + 6))));
            drawing.Children.Add(new GeometryDrawing(
                null, centerPen,
                new EllipseGeometry(new Point(centerX, centerY), 2.2, 2.2)));

            // Feine, deterministische Sprenkel sorgen für eine raue PEI-artige Optik.
            Brush speckleLight = new SolidColorBrush(Color.FromArgb(62, 220, 223, 228));
            Brush speckleDark = new SolidColorBrush(Color.FromArgb(55, 9, 11, 14));
            for (int i = 0; i < 260; i++)
            {
                double x = ((i * 73 + 19) % Math.Max(1.0, plateWidth - 5)) + 2;
                double y = ((i * 137 + 41) % Math.Max(1.0, plateDepth - 5)) + 2;
                double r = 0.25 + (i % 5) * 0.13;
                drawing.Children.Add(new GeometryDrawing(
                    i % 3 == 0 ? speckleDark : speckleLight,
                    null,
                    new EllipseGeometry(new Point(x, y), r, r)));
            }

            // Orientierung und Plattenname direkt auf der Platte.
            AddPlateText(drawing, "X →", new Point(Math.Max(8, plateWidth - 30), 9), 5.0, FontWeights.Bold);
            AddPlateText(drawing, "Y →", new Point(8, Math.Max(8, plateDepth - 30)), 5.0, FontWeights.Bold, -90.0);
            AddPlateText(drawing, $"{centerX:0} / {centerY:0}", new Point(centerX + 5, centerY - 2), 3.2, FontWeights.Normal);
            AddPlateText(drawing, plateName, new Point(8, Math.Max(12, plateDepth - 12)), 4.6, FontWeights.SemiBold);
            AddPlateText(drawing, $"{plateWidth:0} × {plateDepth:0} mm", new Point(8, Math.Max(6, plateDepth - 6)), 3.2, FontWeights.Normal);

            DrawingBrush brush = new DrawingBrush(drawing)
            {
                Viewbox = new Rect(0, 0, plateWidth, plateDepth),
                ViewboxUnits = BrushMappingMode.Absolute,
                Stretch = Stretch.Fill
            };
            return brush;
        }

        private static void AddPlateText(
            DrawingGroup drawing,
            string text,
            Point origin,
            double fontSize,
            FontWeight fontWeight,
            double rotation = 0.0)
        {
            FormattedText formatted = new FormattedText(
                text,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, fontWeight, FontStretches.Normal),
                fontSize,
                new SolidColorBrush(Color.FromArgb(190, 232, 235, 240)),
                1.0);

            Geometry geometry = formatted.BuildGeometry(origin);
            if (Math.Abs(rotation) > 0.001)
                geometry.Transform = new RotateTransform(rotation, origin.X, origin.Y);

            drawing.Children.Add(new GeometryDrawing(
                new SolidColorBrush(Color.FromArgb(190, 232, 235, 240)),
                null,
                geometry));
        }

        private static MeshGeometry3D CreateBoxMesh(
            double minX, double maxX,
            double minY, double maxY,
            double minZ, double maxZ)
        {
            MeshGeometry3D mesh = new MeshGeometry3D();
            Point3D[] p =
            {
                new(minX, minY, minZ), new(maxX, minY, minZ),
                new(maxX, maxY, minZ), new(minX, maxY, minZ),
                new(minX, minY, maxZ), new(maxX, minY, maxZ),
                new(maxX, maxY, maxZ), new(minX, maxY, maxZ)
            };
            foreach (Point3D point in p)
                mesh.Positions.Add(point);

            int[] indices =
            {
                0,2,1, 0,3,2,
                0,1,5, 0,5,4,
                1,2,6, 1,6,5,
                2,3,7, 2,7,6,
                3,0,4, 3,4,7
            };
            foreach (int index in indices)
                mesh.TriangleIndices.Add(index);
            return mesh;
        }

        private static void AddExtraLighting(Model3DGroup model)
        {
            model.Children.Add(new DirectionalLight(
                Color.FromRgb(170, 185, 210),
                new Vector3D(0.7, -0.5, -1.0)));
            model.Children.Add(new DirectionalLight(
                Color.FromRgb(105, 115, 135),
                new Vector3D(-0.5, 0.8, -0.4)));
        }

        private static Rect3D CombineBounds(Rect3D a, Rect3D b)
        {
            if (a.IsEmpty) return b;
            if (b.IsEmpty) return a;
            Rect3D result = a;
            result.Union(b);
            return result;
        }

        private void InitializePrinterProfiles()
        {
            _printerProfileInitialized = false;
            CmbPrinterProfile.ItemsSource = PrinterProfile.All;
            _activePrinterProfile = PrinterProfile.LoadSelected();
            CmbPrinterProfile.SelectedItem = PrinterProfile.All.FirstOrDefault(p => p.Id == _activePrinterProfile.Id)
                ?? PrinterProfile.BambuLabX1C;
            _printerProfileInitialized = true;
        }

        private void CmbPrinterProfile_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_printerProfileInitialized || CmbPrinterProfile.SelectedItem is not PrinterProfile profile)
                return;

            _activePrinterProfile = profile;
            PrinterProfile.SaveSelected(profile);

            if (ModelViewport.Visibility == Visibility.Visible && !_current3DModelBounds.IsEmpty)
                RefreshPrinterProfile3D();
        }

        private void RefreshPrinterProfile3D()
        {
            RefreshActivePlatePresentation();
            AktualisiereObjektliste3D();
            AktualisiereAusgewaehltesObjektInfo3D();
        }

        private void ChkShowBuildPlate_Changed(object sender, RoutedEventArgs e)
        {
            if (BuildPlateVisual == null)
                return;

            BuildPlateVisual.Content = ChkShowBuildPlate.IsChecked == true
                ? _buildPlateModel
                : null;
        }

        private void ChkShowOriginalBuildPlate_Changed(
            object sender,
            RoutedEventArgs e)
        {
            if (_current3DScene is null ||
                ModelViewport == null ||
                ModelViewport.Visibility != Visibility.Visible)
            {
                return;
            }

            RefreshActivePlatePresentation();
            AktualisiereObjektliste3D();
            AktualisiereAusgewaehltesObjektInfo3D();
        }

        private void Passe3DKameraAn(Rect3D bounds)
        {
            if (bounds.IsEmpty)
                return;

            _model3DCenter = new Point3D(
                bounds.X + bounds.SizeX / 2.0,
                bounds.Y + bounds.SizeY / 2.0,
                bounds.Z + bounds.SizeZ / 2.0);

            double maxSize = Math.Max(bounds.SizeX, Math.Max(bounds.SizeY, bounds.SizeZ));
            if (maxSize <= 0)
                maxSize = 1;

            _model3DInitialDistance = maxSize * 2.5;
            _model3DDistance = _model3DInitialDistance;
        }

        private void Reset3DView()
        {
            if (!_current3DViewBounds.IsEmpty)
            {
                Passe3DKameraAn(
                    _current3DViewBounds);
            }

            _model3DYaw = -35;
            _model3DPitch = 25;
            _model3DPan = new Vector3D();
            _model3DDistance = _model3DInitialDistance;
            Aktualisiere3DAnsicht();
        }

        private void Aktualisiere3DAnsicht()
        {
            if (ModelVisual.Content is null)
                return;

            Transform3DGroup transforms = new Transform3DGroup();
            transforms.Children.Add(new TranslateTransform3D(-_model3DCenter.X, -_model3DCenter.Y, -_model3DCenter.Z));
            transforms.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), _model3DPitch)));
            transforms.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), _model3DYaw)));
            transforms.Children.Add(new TranslateTransform3D(_model3DCenter.X, _model3DCenter.Y, _model3DCenter.Z));
            ModelVisual.Transform = transforms;
            BuildPlateVisual.Transform = transforms;
            BuildVolumeWarningVisual.Transform = transforms;

            Point3D target = _model3DCenter + _model3DPan;
            ModelCamera.Position = new Point3D(target.X, target.Y, target.Z + _model3DDistance);
            ModelCamera.LookDirection = new Vector3D(0, 0, -_model3DDistance);
            ModelCamera.UpDirection = new Vector3D(0, 1, 0);

            AktualisiereOrientierungsanzeige();
        }


        private void AktualisiereOrientierungsanzeige()
        {
            const double center = 40.0;
            const double length = 26.0;

            Transform3DGroup orientation = new Transform3DGroup();
            orientation.Children.Add(
                new RotateTransform3D(
                    new AxisAngleRotation3D(
                        new Vector3D(1, 0, 0),
                        _model3DPitch)));
            orientation.Children.Add(
                new RotateTransform3D(
                    new AxisAngleRotation3D(
                        new Vector3D(0, 1, 0),
                        _model3DYaw)));

            AktualisiereAchse(
                AxisXLine,
                AxisXLabel,
                orientation.Transform(new Vector3D(1, 0, 0)),
                center,
                length);

            AktualisiereAchse(
                AxisYLine,
                AxisYLabel,
                orientation.Transform(new Vector3D(0, 1, 0)),
                center,
                length);

            AktualisiereAchse(
                AxisZLine,
                AxisZLabel,
                orientation.Transform(new Vector3D(0, 0, 1)),
                center,
                length);
        }

        private static void AktualisiereAchse(
            System.Windows.Shapes.Line line,
            TextBlock label,
            Vector3D axis,
            double center,
            double length)
        {
            double endX = center + axis.X * length;
            double endY = center - axis.Y * length;

            line.X1 = center;
            line.Y1 = center;
            line.X2 = endX;
            line.Y2 = endY;

            double depth = Math.Max(-1.0, Math.Min(1.0, axis.Z));
            double opacity = 0.62 + ((depth + 1.0) * 0.19);

            line.Opacity = opacity;
            label.Opacity = opacity;

            Canvas.SetLeft(
                label,
                endX + (axis.X >= 0 ? 4.0 : -13.0));

            Canvas.SetTop(
                label,
                endY + (axis.Y >= 0 ? -15.0 : 2.0));
        }

        private void ModelViewport_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                Reset3DView();
                e.Handled = true;
                return;
            }

            _model3DLeftMouseDownPosition =
                e.GetPosition(ModelViewport);

            _model3DLeftMouseDragged = false;
            _is3DRotating = true;
            _last3DMousePosition =
                _model3DLeftMouseDownPosition;

            ModelViewport.CaptureMouse();
            e.Handled = true;
        }

        private void ModelViewport_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _is3DPanning = true;
            _last3DMousePosition = e.GetPosition(ModelViewport);
            ModelViewport.CaptureMouse();
            e.Handled = true;
        }

        private void ModelViewport_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_is3DRotating && !_is3DPanning)
                return;

            Point current = e.GetPosition(ModelViewport);
            Vector delta = current - _last3DMousePosition;

            if (_is3DRotating &&
                !_model3DLeftMouseDragged)
            {
                Vector fromMouseDown =
                    current - _model3DLeftMouseDownPosition;

                if (Math.Abs(fromMouseDown.X) >= 4.0 ||
                    Math.Abs(fromMouseDown.Y) >= 4.0)
                {
                    _model3DLeftMouseDragged = true;
                }
            }

            _last3DMousePosition = current;

            if (_is3DRotating && e.LeftButton == MouseButtonState.Pressed)
            {
                _model3DYaw += delta.X * 0.6;
                _model3DPitch -= delta.Y * 0.6;
                _model3DPitch = Math.Max(-89, Math.Min(89, _model3DPitch));
                Aktualisiere3DAnsicht();
            }
            else if (_is3DPanning && e.RightButton == MouseButtonState.Pressed)
            {
                double factor = _model3DDistance / 450.0;
                _model3DPan.X -= delta.X * factor;
                _model3DPan.Y += delta.Y * factor;
                Aktualisiere3DAnsicht();
            }
        }

        private void ModelViewport_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Point clickPosition =
                e.GetPosition(ModelViewport);

            bool wasClick =
                !_model3DLeftMouseDragged;

            _is3DRotating = false;

            if (!_is3DPanning)
                ModelViewport.ReleaseMouseCapture();

            if (wasClick)
            {
                SelectSceneObjectFromViewport(
                    clickPosition);
            }

            e.Handled = true;
        }

        private void SelectSceneObjectFromViewport(
            Point position)
        {
            if (_current3DScene is null ||
                ModelViewport.Visibility
                    != Visibility.Visible)
            {
                return;
            }

            HitTestResult? hit =
                VisualTreeHelper.HitTest(
                    ModelViewport,
                    position);

            if (hit is not RayMeshGeometry3DHitTestResult
                    meshHit ||
                meshHit.ModelHit is not GeometryModel3D
                    geometry)
            {
                return;
            }

            ThreeMfSceneObject? sceneObject =
                _current3DScene.Objects.FirstOrDefault(
                    item =>
                        !_hiddenSceneObjects.Contains(item) &&
                        EnumerateGeometryModels(item.Model)
                            .Any(model =>
                                ReferenceEquals(
                                    model,
                                    geometry)));

            if (sceneObject is null)
                return;

            SelectSceneObject(
                sceneObject);
        }

        private void ModelViewport_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            _is3DPanning = false;
            if (!_is3DRotating)
                ModelViewport.ReleaseMouseCapture();
            e.Handled = true;
        }

        private void ModelViewport_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double zoomFactor = e.Delta > 0 ? 0.88 : 1.14;
            _model3DDistance *= zoomFactor;

            double minDistance = Math.Max(_model3DInitialDistance * 0.12, 0.1);
            double maxDistance = Math.Max(_model3DInitialDistance * 12.0, minDistance);
            _model3DDistance = Math.Max(minDistance, Math.Min(maxDistance, _model3DDistance));

            Aktualisiere3DAnsicht();
            e.Handled = true;
        }

        // ============================================================
        // DATEI ÖFFNEN
        // ============================================================

        private void OeffneDatei(
            ThreeMfFile file)
        {
            try
            {
                if (!File.Exists(
                        file.FullPath))
                {
                    MessageBox.Show(
                        Localization.Get("FileNoLongerExists"),
                        Localization.Get("OpenFileTitle"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    AktualisiereAktuellenOrdner();

                    return;
                }

                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName =
                            file.FullPath,

                        UseShellExecute =
                            true
                    });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{Localization.Get("OpenError")}\n\n{ex.Message}",
                    Localization.Get("OpenFileTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}