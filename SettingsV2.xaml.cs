using Microsoft.WindowsAPICodePack.Taskbar;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CrosshairOverlay
{
    public partial class SettingsV2 : Window
    {
        private MainWindow mw = null;
        private List<displayControls> displayButtons = new List<displayControls>();
        private bool skip = true;
        private string cType = "cross";

        //Minor modifications applied
        #region TEMPLATE
        bool allowClose = true;
        Timer winAero = new Timer();
        Timer checkForChange = new Timer();
        double previousWidth = 0;
        double previousHeight = 0;
        double previousTop = 0;
        double previousLeft = 0;

        public SettingsV2(MainWindow mainWindow)
        {
            mw = mainWindow;
            InitializeComponent();
            string[] FVI = FileVersionInfo.GetVersionInfo(MainWindow.coDIR + "CrosshairOverlay.dll").FileVersion.Split('.');
            release.Content = $"Release: {FVI[0]}.{FVI[1]}.{FVI[2]}";
            if (taskbarGroup != string.Empty)
            { SourceInitialized += (s, ev) => { TaskbarManager.Instance.SetApplicationIdForSpecificWindow(new WindowInteropHelper(this).Handle, taskbarGroup); }; }
            windowBorder.Visibility = Visibility.Visible;
            appTitle.Content = windowTitle;
            if (!allowResize) { resizebtn.Visibility = Visibility.Collapsed; ResizeMode = ResizeMode.NoResize; }
            previousWidth = defaultWidth;
            previousHeight = defaultHeight;
            Width = defaultWidth;
            Height = defaultHeight;
            previousTop = Top;
            previousLeft = Left;
            if (minWidth > 0) { MinWidth = minWidth; }
            if (minHeight > 0) { MinHeight = minHeight; }
            if (maxWidth > 0) { MaxWidth = maxWidth; }
            if (maxHeight > 0) { MaxHeight = maxHeight; }
            WindowStartup();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            winAero.Interval = 10;
            winAero.Elapsed += checkForAeroFC;
            winAero.Start();

            DataContext = new XAMLStyles { };
            checkForChange.Interval = 1000;
            checkForChange.Elapsed += (se, ea) => { try { if (Styles.themeChanged) { Dispatcher.Invoke(() => { DataContext = new XAMLStyles { }; }); } } catch { } };
            checkForChange.Start();

            windowLoaded();
        }

        #region Window functions
        private void topBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (Width == SystemParameters.WorkArea.Width && Height == SystemParameters.WorkArea.Height && allowResize == true)
                {
                    windowBorder.Visibility = Visibility.Visible;
                    Top = System.Windows.Forms.Control.MousePosition.Y - 15;
                    Left = System.Windows.Forms.Control.MousePosition.X - 400;
                    Width = previousWidth;
                    Height = previousHeight;
                    resizebtn.Content = "\uE922";
                    DragMove();
                }
                else if (e.ClickCount == 2 && allowResize == true)
                {
                    Top = 0;
                    Left = 0;
                    Width = SystemParameters.WorkArea.Width;
                    Height = SystemParameters.WorkArea.Height;
                    resizebtn.Content = "\uE923";
                    windowBorder.Visibility = Visibility.Hidden;
                }
                else
                {
                    DragMove();
                    previousWidth = Width;
                    previousHeight = Height;
                    previousTop = Top;
                    previousLeft = Left;
                }
            }
        }

        private void closebtn_Click(object sender, RoutedEventArgs e) { allowClose = true; Close(); }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (allowClose == false)
            {
                disallowClosing();
                e.Cancel = false;
            }
            else
            {
                winAero.Stop();
                allowClosing();
                e.Cancel = false;
            }
        }

        private void resizebtn_Click(object sender, RoutedEventArgs e)
        {
            if (Height != SystemParameters.WorkArea.Height && Width != SystemParameters.WorkArea.Width)
            {
                previousWidth = Width;
                previousHeight = Height;
                Top = 0;
                Left = 0;
                Height = SystemParameters.WorkArea.Height;
                Width = SystemParameters.WorkArea.Width;
                windowBorder.Visibility = Visibility.Hidden;
                resizebtn.Content = "\uE923";
            }
            else
            {
                WindowState = WindowState.Normal;
                Width = previousWidth;
                Height = previousHeight;
                Top = previousTop;
                Left = previousLeft;
                windowBorder.Visibility = Visibility.Visible;
                resizebtn.Content = "\uE922";
            }
        }

        private void minimisebtn_Click(object sender, RoutedEventArgs e) { saveSettings(); allowClose = false; Close(); }

        private void checkForAeroFC(object sender, ElapsedEventArgs e)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (allowResize)
                    {
                        if (WindowState == WindowState.Maximized)
                        {
                            WindowState = WindowState.Normal;
                            Top = 0;
                            Left = 0;
                            Width = SystemParameters.WorkArea.Width;
                            Height = SystemParameters.WorkArea.Height;
                            resizebtn.Content = "\uE923";
                            windowBorder.Visibility = Visibility.Hidden;
                        }
                        else if (Width != SystemParameters.WorkArea.Width && Height != SystemParameters.WorkArea.Height)
                        {
                            resizebtn.Content = "\uE922";
                            windowBorder.Visibility = Visibility.Visible;
                        }

                        if (Height > SystemParameters.WorkArea.Height) { Height = SystemParameters.WorkArea.Height; }
                    }
                    else
                    {
                        if (WindowState == WindowState.Maximized)
                        {
                            WindowState = WindowState.Normal;
                            Top = previousTop;
                            Left = previousLeft;
                            Width = defaultWidth;
                            Height = defaultHeight;
                            resizebtn.Content = "\uE923";
                            windowBorder.Visibility = Visibility.Visible;
                        }
                    }
                });
            }
            catch { }
        }
        #endregion
        #endregion

        #region TEMPLATE MODIFIERS
        string windowTitle = "Crosshair Overlay Settings";
        string taskbarGroup = "CrosshairOverlay";
        bool allowResize = false;
        double defaultWidth = 600;
        double defaultHeight = 310;
        double minWidth = 600; //0 = No minimum
        double minHeight = 310; //0 = No minimum
        double maxWidth = SystemParameters.WorkArea.Width; //0 = No maximum
        double maxHeight = SystemParameters.WorkArea.Height; //0 = No maximum

        private void WindowStartup()
        {
            mw.notifyIcon.Visible = false;
        }

        private void windowLoaded()
        {
            getType();
            getColours();
            getSize();
            displays();
            activateCurrentDisplayButton();
            getExtras();
            skip = false;
            Activate();
        }

        #region Window closing
        private void allowClosing()
        {
            saveSettings();
            mw.Close();
        }

        private void disallowClosing()
        {
            mw.notifyIcon.Visible = true;
        }
        #endregion
        #endregion

        #region Type
        private void getType()
        {
            if (mw.cross.IsVisible) { crossRDO.IsChecked = true; }
            else if (mw.circle.IsVisible) { circleRDO.IsChecked = true; }
        }

        private void cTypeRDO_Checked(object sender, RoutedEventArgs e)
        {
            if (!skip)
            {
                RadioButton radioButton = sender as RadioButton;
                if (radioButton.Name == "crossRDO")
                {
                    cType = "cross";
                    mw.cross.Visibility = Visibility.Visible;
                    mw.circle.Visibility = Visibility.Hidden;
                    mw.cImage.Visibility = Visibility.Hidden;
                    cControlsVisibility(true);
                }
                else if (radioButton.Name == "circleRDO")
                {
                    cType = "circle";
                    mw.cross.Visibility = Visibility.Hidden;
                    mw.circle.Visibility = Visibility.Visible;
                    mw.cImage.Visibility = Visibility.Hidden;
                    cControlsVisibility(true);
                }
            }
        }

        private void imageBTN_Click(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.OpenFileDialog o = new System.Windows.Forms.OpenFileDialog())
            {
                o.Title = "Select An Image;";
                o.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                o.Filter = "Images (*.BMP;*.JPG;*.PNG,*.TIFF)|*.BMP;*.JPG;*.PNG;*.TIFF|All Files (*.*)|*.*";
                if (o.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    try
                    {
                        using (System.Drawing.Image image = System.Drawing.Image.FromFile(o.FileName))
                        {
                            using (MemoryStream m = new MemoryStream())
                            {
                                image.Save(m, image.RawFormat);
                                byte[] imageBytes = m.ToArray();

                                BitmapImage imageToDisplay = new BitmapImage();
                                imageToDisplay.BeginInit();
                                imageToDisplay.StreamSource = new MemoryStream(Convert.FromBase64String(Convert.ToBase64String(imageBytes)));
                                imageToDisplay.EndInit();
                                mw.cImage.Source = imageToDisplay;

                                circleRDO.IsChecked = false;
                                crossRDO.IsChecked = false;
                                cControlsVisibility(false);
                                cType = "image";
                                MainWindow.base64String = Convert.ToBase64String(imageBytes);
                                mw.circle.Visibility = Visibility.Hidden;
                                mw.cross.Visibility = Visibility.Hidden;
                                mw.cImage.Visibility = Visibility.Visible;
                            }
                        }
                    }
                    catch { MessageBox.Show($"Failed to process image", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
                }
            }
        }
        #endregion

        #region Colour
        private void cControlsVisibility(bool visibility)
        {
            redSlider.IsEnabled = visibility;
            greenSlider.IsEnabled = visibility;
            blueSlider.IsEnabled = visibility;
            alphaSlider.IsEnabled = visibility;
            redInt.IsEnabled = visibility;
            greenInt.IsEnabled = visibility;
            blueInt.IsEnabled = visibility;
            alphaInt.IsEnabled = visibility;

            if (visibility)
            {
                colourT.Foreground = Styles.bc(Styles.foreground);
                redT.Foreground = Styles.bc(Styles.foreground);
                greenT.Foreground = Styles.bc(Styles.foreground);
                blueT.Foreground = Styles.bc(Styles.foreground);
                alphaT.Foreground = Styles.bc(Styles.foreground);
            }
            else
            {
                colourT.Foreground = Styles.bc(Styles.border);
                redT.Foreground = Styles.bc(Styles.border);
                greenT.Foreground = Styles.bc(Styles.border);
                blueT.Foreground = Styles.bc(Styles.border);
                alphaT.Foreground = Styles.bc(Styles.border);
            }
        }

        private void getColours()
        {
            System.Drawing.Color colour = System.Drawing.ColorTranslator.FromHtml(mw.TRBL.Stroke.ToString());
            alphaInt.Text = colour.A.ToString();
            alphaSlider.Value = colour.A;
            redInt.Text = colour.R.ToString();
            redSlider.Value = colour.R;
            greenInt.Text = colour.G.ToString();
            greenSlider.Value = colour.G;
            blueInt.Text = colour.B.ToString();
            blueSlider.Value = colour.B;
        }

        private void colourSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!skip)
            {
                skip = true;
                Slider slider = sender as Slider;
                if (slider.Name == "redSlider") { redInt.Text = Math.Truncate(redSlider.Value).ToString(); }
                else if (slider.Name == "greenSlider") { greenInt.Text = Math.Truncate(greenSlider.Value).ToString(); }
                else if (slider.Name == "blueSlider") { blueInt.Text = Math.Truncate(blueSlider.Value).ToString(); }
                else if (slider.Name == "alphaSlider") { alphaInt.Text = Math.Truncate(alphaSlider.Value).ToString(); }
                updateRGB();
                skip = false;
            }
        }

        private void colourInt_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!skip)
            {
                skip = true;
                TextBox textBox = sender as TextBox;
                if (textBox.Name == "redInt") { isRGBValid(textBox); }
                else if (textBox.Name == "greenInt") { isRGBValid(textBox); }
                else if (textBox.Name == "blueInt") { isRGBValid(textBox); }
                else if (textBox.Name == "alphaInt") { isRGBValid(textBox); }
                updateRGB();
                skip = false;

                void isRGBValid(TextBox tb)
                {
                    int input;
                    int output;
                    bool number;
                    try { input = Convert.ToInt32(tb.Text); number = true; }
                    catch { number = false; input = 0; }

                    if (number == true)
                    {
                        if (input > 255) { output = 255; }
                        else if (input < 0) { output = 0; }
                        else { output = input; }
                    }
                    else { output = input; }

                    if (tb.Name == "redInt") { redInt.Text = output.ToString(); redSlider.Value = output; }
                    else if (tb.Name == "greenInt") { greenInt.Text = output.ToString(); greenSlider.Value = output; }
                    else if (tb.Name == "blueInt") { blueInt.Text = output.ToString(); blueSlider.Value = output; }
                    else if (tb.Name == "alphaInt") { alphaInt.Text = output.ToString(); alphaSlider.Value = output; }
                }
            }
        }

        private void updateRGB()
        {
            Color colour = Color.FromArgb((byte)alphaSlider.Value, (byte)redSlider.Value, (byte)greenSlider.Value, (byte)blueSlider.Value);
            mw.TRBL.Stroke = new SolidColorBrush(colour);
            mw.TLBR.Stroke = new SolidColorBrush(colour);
            mw.sCircle.Fill = new SolidColorBrush(colour);
        }
        #endregion

        #region Size
        private void getSize() { cSizeSlider.Value = mw.cSize.Height / 10 * 3; }

        private void cSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!skip)
            {
                mw.cSize.Height = cSizeSlider.Value * 10 / 3;
                mw.cSize.Width = cSizeSlider.Value * 10 / 3;
            }
        }
        #endregion

        #region Display
        private void displays()
        {
            foreach (string disp in mw.displays)
            {
                Border border = new Border();
                border.BorderBrush = Styles.bc(Styles.foreground);
                border.BorderThickness = new Thickness(1, 1, 1, 1);

                Label label = new Label();
                label.Content = mw.displays.FindIndex(x => x.Equals(disp)) + 1;
                label.Foreground = Styles.bc(Styles.foreground);
                label.HorizontalAlignment = HorizontalAlignment.Center;
                label.VerticalAlignment = VerticalAlignment.Center;

                Grid grid = new Grid();
                grid.MouseDown += (ob, se) =>
                {
                    MainWindow.display = mw.displays.FindIndex(x => x.Equals(disp));

                    foreach (displayControls DisplayControls in displayButtons)
                    {
                        DisplayControls.label.Foreground = Styles.bc(Styles.foreground);
                        DisplayControls.grid.Background = Styles.bc(Styles.background);
                    }

                    grid.Background = Styles.bc(Styles.foreground);
                    label.Foreground = Styles.bc(Styles.background);

                    string[] split = disp.Split(',');
                    mw.Left = Convert.ToInt32(split[0].Substring(3));
                    mw.Top = Convert.ToInt32(split[1].Substring(2));
                    mw.Width = Convert.ToInt32(split[2].Substring(6));
                    mw.Height = Convert.ToInt32(split[3].Remove(split[3].Length - 1, 1).Substring(7));
                };
                grid.MouseEnter += (ob, se) => { if (MainWindow.display != mw.displays.FindIndex(x => x.Equals(disp))) { grid.Background = Styles.bc(Styles.accent); } };
                grid.MouseLeave += (ob, se) => { if (MainWindow.display != mw.displays.FindIndex(x => x.Equals(disp))) { grid.Background = Styles.bc(Styles.background); } };
                grid.Background = Styles.bc(Styles.background);
                grid.Margin = new Thickness(0, 0, 5, 0);
                grid.Width = 40;
                grid.Height = 40;

                grid.Children.Add(label);
                grid.Children.Add(border);
                displaysPanel.Children.Add(grid);
                displayButtons.Add(new displayControls { display = mw.displays.FindIndex(x => x.Equals(disp)), label = label, grid = grid });
            }
        }

        private void activateCurrentDisplayButton()
        {
            foreach (displayControls dp in displayButtons)
            {
                if (MainWindow.display == dp.display)
                {
                    dp.grid.Background = Styles.bc(Styles.foreground);
                    dp.label.Foreground = Styles.bc(Styles.background);
                    break;
                }
            }
        }

        class displayControls
        {
            public int display { get; set; }
            public Label label { get; set; }
            public Grid grid { get; set; }
        }
        #endregion

        #region Extras
        private void getExtras()
        {
            getCatchingMouse();
        }

        #region Hide On Mouse Capture
        private void getCatchingMouse()
        {
            if (mw.catchingMouse) { catchingMouseVisibility(true); delayInt.Text = mw.delay.ToString(); }
            else { catchingMouseVisibility(false); delayInt.Text = mw.delay.ToString(); }
        }

        private void catchingMouseVisibility(bool visibility)
        {
            hideOnRightClickCHKBX.IsChecked = visibility;
            delayT.IsEnabled = visibility;
            delayInt.IsEnabled = visibility;
            mw.catchingMouse = visibility;
        }

        private void hideOnRightClickCHKBX_Checked(object sender, RoutedEventArgs e) { catchingMouseVisibility(true); }
        private void hideOnRightClickCHKBX_Unchecked(object sender, RoutedEventArgs e) { catchingMouseVisibility(false); }
        #endregion
        #endregion

        private void saveSettings()
        {
            System.Drawing.Color colour = System.Drawing.ColorTranslator.FromHtml(mw.TRBL.Stroke.ToString());
            List<UserSettingsJSON> toSave = new List<UserSettingsJSON>();
            toSave.Add(new UserSettingsJSON()
            {
                crosshairType = cType,
                crosshairSize = cSizeSlider.Value * 10 / 3,
                crosshairImage = MainWindow.base64String,
                red = colour.R,
                green = colour.G,
                blue = colour.B,
                alpha = colour.A,
                display = MainWindow.display,
                hideOnRightClick = mw.catchingMouse,
                delay = mw.delay
            });

            using (StreamWriter file = File.CreateText(MainWindow.coDIR + "userSettings.json"))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, toSave);
            }
        }
    }
}
