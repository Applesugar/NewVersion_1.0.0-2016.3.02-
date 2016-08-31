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
using System.Windows.Shapes;
using System.IO;

namespace NewVersion_1._0._0
{
    /// <summary>
    /// SelectTarget.xaml 的交互逻辑
    /// </summary>
    public partial class SelectTarget : Window
    {

        private MainWindow _mainWindow;
        public SelectTarget(MainWindow TheMainWindow)
        {
            InitializeComponent();

            _mainWindow = TheMainWindow;

            //设置窗口显示的位置
            this.WindowStartupLocation = WindowStartupLocation.Manual;
            this.Top = 0;
            this.Left = 500;
        }

        private void CheckPicture1_Loaded(object sender, RoutedEventArgs e)
        {
            string str = AppDomain.CurrentDomain.BaseDirectory;
            BitmapImage Init_image = new BitmapImage(new Uri(str + "//MyPicture//CheckPicture_1.png", UriKind.RelativeOrAbsolute));
            CheckPicture1.Source = Init_image;
        }

        private void CheckPicture2_Loaded(object sender, RoutedEventArgs e)
        {
            string str = AppDomain.CurrentDomain.BaseDirectory;
            BitmapImage Init_image = new BitmapImage(new Uri(str + "//MyPicture//CheckPicture_2.png", UriKind.RelativeOrAbsolute));
            CheckPicture2.Source = Init_image;
        }

        private void CheckPicture3_Loaded(object sender, RoutedEventArgs e)
        {
            string str = AppDomain.CurrentDomain.BaseDirectory;
            BitmapImage Init_image = new BitmapImage(new Uri(str + "//MyPicture//CheckPicture_3.png", UriKind.RelativeOrAbsolute));
            CheckPicture3.Source = Init_image;
        }

        private void CheckPicture4_Loaded(object sender, RoutedEventArgs e)
        {
            string str = AppDomain.CurrentDomain.BaseDirectory;
            BitmapImage Init_image = new BitmapImage(new Uri(str + "//MyPicture//CheckPicture_4.png", UriKind.RelativeOrAbsolute));
            CheckPicture4.Source = Init_image;
        }

        private void CheckPicture5_Loaded(object sender, RoutedEventArgs e)
        {
            string str = AppDomain.CurrentDomain.BaseDirectory;
            BitmapImage Init_image = new BitmapImage(new Uri(str + "//MyPicture//CheckPicture_5.png", UriKind.RelativeOrAbsolute));
            CheckPicture5.Source = Init_image;
        }

        private void CheckPicture6_Loaded(object sender, RoutedEventArgs e)
        {
            string str = AppDomain.CurrentDomain.BaseDirectory;
            BitmapImage Init_image = new BitmapImage(new Uri(str + "//MyPicture//CheckPicture_6.png", UriKind.RelativeOrAbsolute));
            CheckPicture6.Source = Init_image;
        }

        private void CheckPicture7_Loaded(object sender, RoutedEventArgs e)
        {
            string str = AppDomain.CurrentDomain.BaseDirectory;
            BitmapImage Init_image = new BitmapImage(new Uri(str + "//MyPicture//CheckPicture_7.png", UriKind.RelativeOrAbsolute));
            CheckPicture7.Source = Init_image;
        }

        private void CheckPicture8_Loaded(object sender, RoutedEventArgs e)
        {
            string str = AppDomain.CurrentDomain.BaseDirectory;
            BitmapImage Init_image = new BitmapImage(new Uri(str + "//MyPicture//CheckPicture_8.png", UriKind.RelativeOrAbsolute));
            CheckPicture8.Source = Init_image;
        }

        private void CheckPicture9_Loaded(object sender, RoutedEventArgs e)
        {
            string str = AppDomain.CurrentDomain.BaseDirectory;
            BitmapImage Init_image = new BitmapImage(new Uri(str + "//MyPicture//CheckPicture_9.png", UriKind.RelativeOrAbsolute));
            CheckPicture9.Source = Init_image;
        }

        private void CheckPicture10_Loaded(object sender, RoutedEventArgs e)
        {
            string str = AppDomain.CurrentDomain.BaseDirectory;
            BitmapImage Init_image = new BitmapImage(new Uri(str + "//MyPicture//CheckPicture_10.png", UriKind.RelativeOrAbsolute));
            CheckPicture10.Source = Init_image;
        }

        private void CheckPicture11_Loaded(object sender, RoutedEventArgs e)
        {
            string str = AppDomain.CurrentDomain.BaseDirectory;
            BitmapImage Init_image = new BitmapImage(new Uri(str + "//MyPicture//CheckPicture_11.png", UriKind.RelativeOrAbsolute));
            CheckPicture11.Source = Init_image;
        }

        private void CheckPicture_1_Checked(object sender, RoutedEventArgs e)
        {
            _mainWindow.SetTestNum(1);
            MessageBox.Show("您已经选择了第 1 项任务！");

            string str = AppDomain.CurrentDomain.BaseDirectory;
            BitmapImage Init_image = new BitmapImage(new Uri(str + "//MyPicture//1.png", UriKind.RelativeOrAbsolute));
            _mainWindow.GetImageShow().Source = Init_image;
            _mainWindow.GetPreTargetButton().IsEnabled = true;
            _mainWindow.GetNextTargetButton().IsEnabled = true;
            _mainWindow.GetBeginButton().IsEnabled = true;
            this.Close();
            this.OnClosed(e);
        }

        private void CheckPicture_2_Checked(object sender, RoutedEventArgs e)
        {
            _mainWindow.SetTestNum(2); 
            MessageBox.Show("您已经选择了第 2 项任务！");

            string str = AppDomain.CurrentDomain.BaseDirectory;
            BitmapImage Init_image = new BitmapImage(new Uri(str + "//MyPicture//2.png", UriKind.RelativeOrAbsolute));
            _mainWindow.GetImageShow().Source = Init_image;
            _mainWindow.GetPreTargetButton().IsEnabled = true;
            _mainWindow.GetNextTargetButton().IsEnabled = true;
            _mainWindow.GetBeginButton().IsEnabled = true;
            this.Close();
            this.OnClosed(e);
        }

        private void CheckPicture_3_Checked(object sender, RoutedEventArgs e)
        {
            _mainWindow.SetTestNum(3); 
            MessageBox.Show("您已经选择了第 3 项任务！");

            string str = AppDomain.CurrentDomain.BaseDirectory;
            BitmapImage Init_image = new BitmapImage(new Uri(str + "//MyPicture//3.png", UriKind.RelativeOrAbsolute));
            _mainWindow.GetImageShow().Source = Init_image;
            _mainWindow.GetPreTargetButton().IsEnabled = true;
            _mainWindow.GetNextTargetButton().IsEnabled = true;
            _mainWindow.GetBeginButton().IsEnabled = true;
            this.Close();
            this.OnClosed(e);
        }

        private void CheckPicture_4_Checked(object sender, RoutedEventArgs e)
        {
            _mainWindow.SetTestNum(4); ;
            MessageBox.Show("您已经选择了第 4 项任务！");

            string str = AppDomain.CurrentDomain.BaseDirectory;
            BitmapImage Init_image = new BitmapImage(new Uri(str + "//MyPicture//4.png", UriKind.RelativeOrAbsolute));
            _mainWindow.GetImageShow().Source = Init_image;
            _mainWindow.GetPreTargetButton().IsEnabled = true;
            _mainWindow.GetNextTargetButton().IsEnabled = true;
            _mainWindow.GetBeginButton().IsEnabled = true;
            this.Close();
            this.OnClosed(e);
        }

        private void CheckPicture_5_Checked(object sender, RoutedEventArgs e)
        {
            _mainWindow.SetTestNum(5);
            MessageBox.Show("您已经选择了第 5 项任务！");

            string str = AppDomain.CurrentDomain.BaseDirectory;
            BitmapImage Init_image = new BitmapImage(new Uri(str + "//MyPicture//5.png", UriKind.RelativeOrAbsolute));
            _mainWindow.GetImageShow().Source = Init_image;
            _mainWindow.GetPreTargetButton().IsEnabled = true;
            _mainWindow.GetNextTargetButton().IsEnabled = true;
            _mainWindow.GetBeginButton().IsEnabled = true;
            this.Close();
            this.OnClosed(e);
        }

        private void CheckPicture_6_Checked(object sender, RoutedEventArgs e)
        {
            _mainWindow.SetTestNum(6);
            MessageBox.Show("您已经选择了第 6 项任务！");

            string str = AppDomain.CurrentDomain.BaseDirectory;
            BitmapImage Init_image = new BitmapImage(new Uri(str + "//MyPicture//6.png", UriKind.RelativeOrAbsolute));
            _mainWindow.GetImageShow().Source = Init_image;
            _mainWindow.GetPreTargetButton().IsEnabled = true;
            _mainWindow.GetNextTargetButton().IsEnabled = true;
            _mainWindow.GetBeginButton().IsEnabled = true;
            this.Close();
            this.OnClosed(e);
        }

        private void CheckPicture_7_Checked(object sender, RoutedEventArgs e)
        {
            _mainWindow.SetTestNum(7);
            MessageBox.Show("您已经选择了第 7 项任务！");

            string str = AppDomain.CurrentDomain.BaseDirectory;
            BitmapImage Init_image = new BitmapImage(new Uri(str + "//MyPicture//7.png", UriKind.RelativeOrAbsolute));
            _mainWindow.GetImageShow().Source = Init_image;
            _mainWindow.GetPreTargetButton().IsEnabled = true;
            _mainWindow.GetNextTargetButton().IsEnabled = true;
            _mainWindow.GetBeginButton().IsEnabled = true;
            this.Close();
            this.OnClosed(e);
        }

        private void CheckPicture_8_Checked(object sender, RoutedEventArgs e)
        {
            _mainWindow.SetTestNum(8);
            MessageBox.Show("您已经选择了第 8 项任务！");

            string str = AppDomain.CurrentDomain.BaseDirectory;
            BitmapImage Init_image = new BitmapImage(new Uri(str + "//MyPicture//8.png", UriKind.RelativeOrAbsolute));
            _mainWindow.GetImageShow().Source = Init_image;
            _mainWindow.GetPreTargetButton().IsEnabled = true;
            _mainWindow.GetNextTargetButton().IsEnabled = true;
            _mainWindow.GetBeginButton().IsEnabled = true;
            this.Close();
            this.OnClosed(e);
        }

        private void CheckPicture_9_Checked(object sender, RoutedEventArgs e)
        {
            _mainWindow.SetTestNum(9);
            MessageBox.Show("您已经选择了第 9 项任务！");

            string str = AppDomain.CurrentDomain.BaseDirectory;
            BitmapImage Init_image = new BitmapImage(new Uri(str + "//MyPicture//9.png", UriKind.RelativeOrAbsolute));
            _mainWindow.GetImageShow().Source = Init_image;
            _mainWindow.GetPreTargetButton().IsEnabled = true;
            _mainWindow.GetNextTargetButton().IsEnabled = true;
            _mainWindow.GetBeginButton().IsEnabled = true;
            this.Close();
            this.OnClosed(e);
        }

        private void CheckPicture_10_Checked(object sender, RoutedEventArgs e)
        {
            _mainWindow.SetTestNum(10);
            MessageBox.Show("您已经选择了第 10 项任务！");

            string str = AppDomain.CurrentDomain.BaseDirectory;
            BitmapImage Init_image = new BitmapImage(new Uri(str + "//MyPicture//10.png", UriKind.RelativeOrAbsolute));
            _mainWindow.GetImageShow().Source = Init_image;
            _mainWindow.GetPreTargetButton().IsEnabled = true;
            _mainWindow.GetNextTargetButton().IsEnabled = true;
            _mainWindow.GetBeginButton().IsEnabled = true;
            this.Close();
            this.OnClosed(e);
        }

        private void CheckPicture_11_Checked(object sender, RoutedEventArgs e)
        {
            _mainWindow.SetTestNum(11);
            MessageBox.Show("您已经选择了第 11 项任务！");

            string str = AppDomain.CurrentDomain.BaseDirectory;
            BitmapImage Init_image = new BitmapImage(new Uri(str + "//MyPicture//11.png", UriKind.RelativeOrAbsolute));
            _mainWindow.GetImageShow().Source = Init_image;
            _mainWindow.GetPreTargetButton().IsEnabled = true;
            _mainWindow.GetNextTargetButton().IsEnabled = false;
            _mainWindow.GetBeginButton().IsEnabled = true;
            this.Close();
            this.OnClosed(e);
        }

    }
}
