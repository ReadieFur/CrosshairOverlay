using EventHook;
using Microsoft.WindowsAPICodePack.Taskbar;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace CrosshairOverlay
{
    public partial class MainWindow : Window
    {
        //TODO: Add optional delay to the hide crosshair, save user settings and allow custom image crosshairs
        public int delay = 0;
        public NotifyIcon notifyIcon = null;
        public List<string> displays = new List<string>();
        public MouseWatcher mouseWatcher;
        public bool catchingMouse = false;
        public static string coDIR;
        public static int display;
        public static string base64String = string.Empty;

        protected override void OnClosing(CancelEventArgs e)
        {
            notifyIcon.Icon = null;
            notifyIcon.Text = null;
            notifyIcon.Visible = false;
            e.Cancel = false;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            TaskbarManager.Instance.SetApplicationIdForSpecificWindow(new WindowInteropHelper(this).Handle, "CrosshairOverlay");
            Window_Properties.disableActivation(this);
        }

        public MainWindow(string e)
        {
            InitializeComponent();
            getWorkingDIR();
        }

        private void mw_Loaded(object sender, RoutedEventArgs e)
        {
            var eventHookFactory = new EventHookFactory();
            mouseWatcher = eventHookFactory.GetMouseWatcher();

            foreach (var screen in Screen.AllScreens) { displays.Add(screen.Bounds.ToString()); }

            try { loadUserSettings(); }
            catch
            {
                Height = SystemParameters.WorkArea.Height;
                Width = SystemParameters.WorkArea.Width;
                int displayCount = 0;
                foreach (Screen screen in Screen.AllScreens)
                {
                    if (screen.Primary == true)
                    {
                        Top = screen.Bounds.Top;
                        Left = screen.Bounds.Left;
                        Width = screen.Bounds.Width;
                        Height = screen.Bounds.Height;
                        display = displayCount;
                        break;
                    }
                    displayCount += 1;
                }
                circle.Visibility = Visibility.Hidden;
                cImage.Visibility = Visibility.Hidden;
                cSize.Height = 6 * 10 / 3;
                cSize.Width = 6 * 10 / 3;
            }

            mouseWatcher.Start();
            notifyIcon.Visible = true;
        }

        private void loadUserSettings()
        {
            mouseWatcher.OnMouseInput += MouseWatcher_OnMouseInput;

            List<UserSettingsJSON> toLoad = JsonConvert.DeserializeObject<List<UserSettingsJSON>>(File.ReadAllText(coDIR + "userSettings.json"));
            foreach(UserSettingsJSON userSetting in toLoad)
            {
                if (userSetting.crosshairType == "circle") { cross.Visibility = Visibility.Hidden; cImage.Visibility = Visibility.Hidden; circle.Visibility = Visibility.Visible; }
                else if (userSetting.crosshairType == "image")
                {
                    BitmapImage imageToDisplay = new BitmapImage();
                    imageToDisplay.BeginInit();
                    imageToDisplay.StreamSource = new MemoryStream(Convert.FromBase64String(userSetting.crosshairImage));
                    imageToDisplay.EndInit();
                    cImage.Source = imageToDisplay;
                    base64String = userSetting.crosshairImage;

                    cImage.Visibility = Visibility.Visible;
                    cross.Visibility = Visibility.Hidden;
                    circle.Visibility = Visibility.Hidden;
                }
                else { cross.Visibility = Visibility.Visible; cImage.Visibility = Visibility.Hidden; circle.Visibility = Visibility.Hidden; }

                cSize.Width = userSetting.crosshairSize;
                cSize.Height = userSetting.crosshairSize;

                System.Windows.Media.Color colour = System.Windows.Media.Color.FromArgb((byte)userSetting.alpha, (byte)userSetting.red, (byte)userSetting.green, (byte)userSetting.blue);
                TRBL.Stroke = new System.Windows.Media.SolidColorBrush(colour);
                TLBR.Stroke = new System.Windows.Media.SolidColorBrush(colour);
                sCircle.Fill = new System.Windows.Media.SolidColorBrush(colour);

                string[] split = displays[userSetting.display].Split(',');
                Left = Convert.ToInt32(split[0].Substring(3));
                Top = Convert.ToInt32(split[1].Substring(2));
                Width = Convert.ToInt32(split[2].Substring(6));
                Height = Convert.ToInt32(split[3].Remove(split[3].Length - 1, 1).Substring(7));
                display = userSetting.display;

                if (userSetting.hideOnRightClick == true) { catchingMouse = true; }

                delay = userSetting.delay;
            }
        }

        private void getWorkingDIR()
        {
            List<string> paths = new List<string>();
            foreach (string f in Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory))
            {
                paths.Add(f);
            }

            foreach (string d in Directory.GetDirectories(AppDomain.CurrentDomain.BaseDirectory))
            {
                foreach (string f in Directory.GetFiles(d))
                {
                    paths.Add(f);
                }
            }

            string dir = string.Empty;

            foreach (string s in paths)
            {
                if (s.Contains("CrosshairOverlay.dll"))
                {
                    List<string> path = s.Split('\\').ToList();
                    path.RemoveAt(path.Count - 1);
                    foreach (string d in path)
                    {
                        dir = dir + d + "\\";
                    }
                    break;
                }
                else { }
            }

            coDIR = dir;
        }

        public void MouseWatcher_OnMouseInput(object sender, EventHook.MouseEventArgs ev)
        {
            try
            {
                if (catchingMouse)
                {
                    if (ev.Message.ToString() == "WM_RBUTTONDOWN") { Thread.Sleep(delay); Dispatcher.Invoke(() => { cSize.Visibility = Visibility.Hidden; }); }
                    else if (ev.Message.ToString() == "WM_RBUTTONUP") { Dispatcher.Invoke(() => { cSize.Visibility = Visibility.Visible; }); }
                }
                else { Dispatcher.Invoke(() => { cSize.Visibility = Visibility.Visible; }); }
            }
            catch { }
        }

        private void mw_Initialized(object sender, EventArgs e)
        {
            Stream iconStream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/CrosshairOverlay;component/Resources/Icon.ico")).Stream;
            notifyIcon = new NotifyIcon();
            notifyIcon.Click += new EventHandler(notifyIcon_Click);
            notifyIcon.Icon = new Icon(iconStream);
            notifyIcon.Text = "Crosshair Overlay";
            notifyIcon.Visible = true;
        }

        private void notifyIcon_Click(object sender, EventArgs e)
        {
            var sw = new SettingsV2(this);
            sw.Show();
        }
    }
}
