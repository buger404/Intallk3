using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using System.Threading;
using Intallk.Models;
using Intallk.Modules;
using System.Windows.Threading;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using System.Windows.Interop;
using Microsoft.Win32;
using System.Net.Cache;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Reflection;

namespace Intallk.PSV
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")]
        // GetCursorPos() makes everything possible
        static extern bool GetCursorPos(ref System.Drawing.Point lpPoint);
        DispatcherTimer timer = new DispatcherTimer();
        OpenFileDialog openFileDialog = new OpenFileDialog();
        Dictionary<string, string> args = new Dictionary<string, string>();
        double bPicListY, bArgListY, bArgBtnY, bOKBtnX, bCancelBtnX;
        bool initialized = false;
        static PaintingCompiler compiler = new PaintingCompiler();
        PaintFile paintFile = null!;
        int argIndex;
        double sDx = -1, sDy, sx = -1, sy;

        public MainWindow()
        {
            InitializeComponent();
            bPicListY = this.Height - picList.Height + 17;
            bArgListY = this.Height - argList.Height;
            bArgBtnY = argOK.Margin.Top - argInputs.Margin.Top;
            bOKBtnX = argOK.Margin.Left - argInputs.Margin.Left;
            bCancelBtnX = argCancel.Margin.Left - argOK.Margin.Left;
            SetArgBoxVisible(Visibility.Hidden);
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += Timer_Tick;
            timer.Start();
            openFileDialog.Title = "导入图片...";
            openFileDialog.Filter = "图片文件|*.jpg;*.png;*.bmp;*.gif;*.jpeg";
            if (!Directory.Exists("Assets")) Directory.CreateDirectory("Assets");
            initialized = true;
        }
        public void SetArgBoxVisible(Visibility Visible)
        {
            argName.Visibility = Visible;
            argInputs.Visibility = Visible;
            argOK.Visibility = Visible;
            argCancel.Visibility = Visible;
        }
        private void Timer_Tick(object? sender, EventArgs e)
        {
            PaintFile p = null!;
            List<string> piclist = new List<string>();
            try
            {
                p = compiler.CompilePaintScript(script.Text, out piclist);
                paintFile = p;
                //picList.Items.Clear();
                foreach (string pic in piclist)
                    if(!picList.Items.Contains(pic))
                        picList.Items.Add(pic);
                foreach(string pic in picList.Items)
                {
                    if (!piclist.Contains(pic))
                        picList.Items.Remove(pic);
                }
                bool changed = false;
                foreach (string s in p.Parameters!)
                {
                    if (!args.ContainsKey(s))
                    {
                        changed = true;
                        args.Add(s, "{" + s + "}");
                    }
                }
                if (changed)
                {
                    argList.Items.Clear();
                    SetArgBoxVisible(Visibility.Hidden);
                    foreach(string s in p.Parameters!)
                    {
                        argList.Items.Add(s + "： " + args[s]);
                    }
                }
                List<object> parg = new List<object>();
                parg.Add(".draw"); parg.Add("");
                foreach (string s in p.Parameters!)
                {
                    parg.Add(args[s]);
                }
                Bitmap b = new PaintingProcessing(p).Paint("", null!, null!, parg.ToArray(), "errImg.jpg", "Assets\\").Result;
                preview.Width = b.Width * 1.0 * (scaler.Value / 10);
                preview.Height = b.Height * 1.0 * (scaler.Value / 10);
                preview.Source = Imaging.CreateBitmapSourceFromHBitmap
                                            (
                                                b.GetHbitmap(),
                                                IntPtr.Zero,
                                                Int32Rect.Empty,
                                                BitmapSizeOptions.FromEmptyOptions()
                                            );
                b.Dispose();
                status.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 255, 255));
                status.Content = "没有发现任何问题。";
            }
            catch (Exception err)
            {
                status.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 83, 57));
                status.Content = err.Message.Replace("\n", " ");
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            picList.Height = Math.Max(0, this.Height - bPicListY);
            argList.Height = Math.Max(0, this.Height - bArgListY);
            previewScrollViewer.Width = previewPanel.Width;
            previewScrollViewer.Height = previewPanel.Height;
        }


        private void picList_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (picList.SelectedItem == null) return;
            string imgPath = "";
            if (File.Exists("Assets\\" + picList.SelectedItem!.ToString()))
            {
                imgPath = "Assets/" + picList.SelectedItem!.ToString();
            }
            else
            {
                imgPath = "errImg.jpg";
            }
            Bitmap b = new Bitmap(imgPath);
            assetsPreview.Source = Imaging.CreateBitmapSourceFromHBitmap
                                            (
                                                b.GetHbitmap(),
                                                IntPtr.Zero,
                                                Int32Rect.Empty,
                                                BitmapSizeOptions.FromEmptyOptions()
                                            );
            b.Dispose();
        }

        private void pasteBtn_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (picList.SelectedItem == null) return;
            if (!Clipboard.ContainsImage()) return;
            using (MemoryStream stream = new MemoryStream())
            {
                BitmapSource image = null!;
                try
                {
                    image = Clipboard.GetImage();
                }
                catch
                {
                    try
                    {
                        if(Clipboard.GetFileDropList().Count == 0)
                        {
                            return;
                        }
                        image = new BitmapImage(new Uri(Clipboard.GetFileDropList()[0]!, UriKind.Absolute));
                    }
                    catch
                    {
                        return;
                    }
                }
                BitmapEncoder be = new JpegBitmapEncoder();
                BitmapFrame bf = BitmapFrame.Create(image);
                be.Frames.Add(bf);
                be.Save(stream);
                File.WriteAllBytes("Assets\\" + picList.SelectedItem!.ToString(), stream.ToArray());
                picList_MouseLeftButtonUp(sender, e);
            }
        }

        private void uploadBtn_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (picList.SelectedItem == null) return;
            if ((bool)openFileDialog.ShowDialog()!)
            {
                if (File.Exists("Assets\\" + picList.SelectedItem!.ToString())) File.Delete("Assets\\" + picList.SelectedItem!.ToString());
                File.Copy(openFileDialog.FileName, "Assets\\" + picList.SelectedItem!.ToString());
                picList_MouseLeftButtonUp(sender, e);
            }
            return;
        }

        private void picList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Image_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Process p = new Process();
            p.StartInfo.FileName = "explorer.exe";
            if (picList.SelectedItem == null)
                p.StartInfo.Arguments = @"Assets\";
            else
                p.StartInfo.Arguments = @" /select, Assets\" + picList.SelectedItem!.ToString();
            p.Start();
        }

        private void preview_MouseDown(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Point dp = e.GetPosition(grid);
            System.Windows.Point p = e.GetPosition(preview);
            sDx = dp.X; sDy = dp.Y; sx = p.X; sy = p.Y;
            selRecct.Margin = new Thickness(sDx, sDy, 0, 0);
            selRecct.Width = 0; selRecct.Height = 0;
            preview.CaptureMouse();
        }

        private void preview_MouseUp(object sender, MouseButtonEventArgs e)
        {
            sDx = double.MinValue;
            preview.ReleaseMouseCapture();
        }

        private void selRecct_MouseDown(object sender, MouseButtonEventArgs e)
        {
            preview_MouseDown(sender, e);
        }

        private void argList_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (argList.SelectedItem == null) return;
            if (argInputs.Visibility == Visibility.Visible)
            {
                Console.Beep();
                return;
            }
            System.Windows.Point p = e.GetPosition(grid);
            argName.Content = paintFile.Parameters![argList.SelectedIndex];
            argInputs.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 96, 100, 96));
            argInputs.Text = args[(string)argName.Content];
            SetArgBoxVisible(Visibility.Visible);
            Thickness t = argInputs.Margin;
            t.Left = this.Width - argList.Width - 13; t.Top = p.Y;
            argInputs.Margin = t;
            t.Top += bArgBtnY;
            argName.Margin = t;
            t.Left += bOKBtnX;
            argOK.Margin = t;
            t.Left += bCancelBtnX;
            argCancel.Margin = t;
            argIndex = argList.SelectedIndex;
        }

        private void argInputs_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!initialized) return;
            string[] t = ((string)argName.Content).Split('/');
            if (t.Length != 2) return;
            if (!t[1].EndsWith('字')) return;
            string num = t[1].Substring(1, t[1].Length - 2);
            int n = 0;
            if(int.TryParse(num,out n))
            {
                if (t[1][0] == '限')
                {
                    if (argInputs.Text.Length > n)
                        argInputs.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 83, 57));
                    else
                        argInputs.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 96, 100, 96));
                }
                if (t[1][0] == '需')
                {
                    if (argInputs.Text.Length != n)
                        argInputs.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 83, 57));
                    else
                        argInputs.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 96, 100, 96));
                }
            }
        }

        private void colorPad_MouseUp(object sender, MouseButtonEventArgs e)
        {
            

        }

        private void colorPicker_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Cross;
            colorPicker.CaptureMouse();
        }

        private void colorPicker_MouseUp(object sender, MouseButtonEventArgs e)
        {
            System.Drawing.Point p = new System.Drawing.Point(0,0);
            GetCursorPos(ref p);
            const BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Static;
            var dpiXProperty = typeof(SystemParameters).GetProperty("DpiX", bindingFlags);
            var dpiYProperty = typeof(SystemParameters).GetProperty("DpiY", bindingFlags);
            Bitmap b = new Bitmap((int)(SystemParameters.FullPrimaryScreenWidth * Convert.ToDouble(dpiXProperty!.GetValue(null, null)!) / 96)
                                , (int)((SystemParameters.MaximumWindowTrackHeight - SystemParameters.MenuBarHeight + SystemParameters.ResizeFrameHorizontalBorderHeight) 
                                         * Convert.ToDouble(dpiXProperty!.GetValue(null, null)!) / 96));
            Graphics g = Graphics.FromImage(b);
            g.CopyFromScreen(new System.Drawing.Point(0, 0), 
                             new System.Drawing.Point(0, 0), 
                             new System.Drawing.Size(b.Width, b.Height));
            g.Dispose();
            Mouse.OverrideCursor = Cursors.Arrow;
            System.Drawing.Color c = b.GetPixel(p.X, p.Y);
            MessageBox.Show(c.R + "," + c.G + "," + c.B + "\n已复制。", "取色器", MessageBoxButton.OK, MessageBoxImage.Information);
            Clipboard.SetText(c.R + "," + c.G + "," + c.B);
            b.Dispose();
        }

        private void argCancel_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            SetArgBoxVisible(Visibility.Hidden);
        }

        private void argOK_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            args[(string)argName.Content] = argInputs.Text;
            argList.Items[argIndex] = argName.Content + "： " + argInputs.Text.Replace("\n", "\\n").Replace("\r", "");
            SetArgBoxVisible(Visibility.Hidden);
        }

        private void selRecct_MouseMove(object sender, MouseEventArgs e)
        {
            preview_MouseMove(sender, e);
        }

        private void selRecct_MouseUp(object sender, MouseButtonEventArgs e)
        {
            preview_MouseUp(sender, e);
        }

        private void previewPanel_MouseWheel(object sender, MouseWheelEventArgs e)
        {

        }

        private void scaler_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!initialized) return;
            preview.Width = preview.Source.Width * (scaler.Value / 10);
            preview.Height = preview.Source.Height * (scaler.Value / 10);
            scaleDisplay.Content = Math.Floor(scaler.Value * 10) + "%";
        }

        private void preview_MouseMove(object sender, MouseEventArgs e)
        {
            System.Windows.Point p = e.GetPosition(preview);
            posDisplay.Content = Math.Round(p.X / scaler.Value * 10) + "," + Math.Round(p.Y / scaler.Value * 10);
            if(e.LeftButton == MouseButtonState.Pressed && sDx > 0)
            {
                System.Windows.Point dp = e.GetPosition(grid);
                selRecct.Width = Math.Max(0, dp.X - sDx);
                selRecct.Height = Math.Max(0, dp.Y - sDy);
                sizeDisplay.Content = Math.Round(sx / scaler.Value * 10) + "," + Math.Round(sy / scaler.Value * 10) +
                                      "   " +
                                      Math.Max(0, Math.Round((p.X - sx) / scaler.Value * 10))
                                      + "x" +
                                      Math.Max(0, Math.Round((p.Y - sy) / scaler.Value * 10));
            }
        }
    }
}
