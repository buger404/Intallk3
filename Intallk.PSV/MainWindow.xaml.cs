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

namespace Intallk.PSV
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        double bPicListY;
        public MainWindow()
        {
            InitializeComponent();
            bPicListY = this.Height - picList.Height;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            picList.Height = this.Height - bPicListY;
        }
    }
}
