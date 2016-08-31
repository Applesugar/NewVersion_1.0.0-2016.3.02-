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
using Microsoft.Kinect.Tools;
using System.Threading;
using System.Windows.Threading;

namespace NewVersion_1._0._0
{
    /// <summary>
    /// ConfirmTest.xaml 的交互逻辑
    /// </summary>
    public partial class ConfirmTest : Window
    {
        private MainWindow _mainWindow;
        //private string TargetPath = AppDomain.CurrentDomain.BaseDirectory + "//MyContentTest";
        private string TargetPath = App.GetPath() + "//MyContentTest";
        private string DataPath = "";
        private int TestNum;
        
        public ConfirmTest(MainWindow TheMainWindow)
        {
            InitializeComponent();

            _mainWindow = TheMainWindow;
            TestNum = _mainWindow.GetTestNum();
            string Target = "Target" + TestNum.ToString() + "*";
            string[] TargetDirectoryNum = Directory.GetDirectories(TargetPath + "//", Target);
            Label_TestInfo.Content = "任务 " + TestNum.ToString() + " 进行当前的第 " + (TargetDirectoryNum.Length + 1).ToString() +" 次测试";
            Label_KnowTestInfor.Content = "请确定在测试之前你已经熟悉操作？";
            Label_TestNow.Content = "是否测试任务 " + TestNum.ToString() + " ?";

            //设置窗口显示的位置
            this.WindowStartupLocation = WindowStartupLocation.Manual;
            this.Top = 0;
            this.Left = 60;
        }

        private void Test_Yes_Click(object sender, RoutedEventArgs e)
        {
            string Target = "Target" + TestNum.ToString() + "*";
            string[] TargetDirectoryNum = Directory.GetDirectories(TargetPath + "//", Target);
            if (TargetDirectoryNum.Length == 0)
            {
                Directory.CreateDirectory(TargetPath + "//Target" + TestNum.ToString());
                DataPath = TargetPath + "//Target" + TestNum.ToString() + "//";
            }
            else
            {
                Directory.CreateDirectory(TargetPath + "//Target" + TestNum.ToString() + "_ADD_" + TargetDirectoryNum.Length.ToString());
                DataPath = TargetPath + "//Target" + TestNum.ToString() + "_ADD_" + TargetDirectoryNum.Length.ToString() + "//";
            }
            for (int i = 0; i < _mainWindow.GetCommList().Length; i++)
            {
                if (_mainWindow.GetCommList()[i].serialport.IsOpen)
                {
                    _mainWindow.GetCommList()[i].serialport.DiscardOutBuffer();
                    _mainWindow.GetCommList()[i].serialport.DiscardInBuffer();
                    _mainWindow.GetCommList()[i].FilePath = DataPath + i.ToString() + ".txt";
                    _mainWindow.GetCommList()[i].fs = new FileStream(_mainWindow.GetCommList()[i].FilePath, FileMode.OpenOrCreate, FileAccess.Write);
                }
            }
            for (int i = 0; i < _mainWindow.GetCommList().Length; i++)
            {
                if (_mainWindow.GetCommList()[i].serialport.IsOpen)
                {
                    if (_mainWindow.GetCommList()[i].ChartPlotterShow == true)
                    {
                        _mainWindow.GetCommList()[i].FileWrite = true;
                        _mainWindow.GetCommList()[i].DataReceived = c_DataReceived;
                    }
                    else
                    {
                        _mainWindow.GetCommList()[i].portNumber = i;
                        _mainWindow.GetCommList()[i].FileWrite = true;
                        _mainWindow.GetCommList()[i].DataReceived = c_DataReceived;
                        _mainWindow.GetCommList()[i].StartReadData();
                    }
                }
            }

            //设置Kinect文件存放路径
            _mainWindow.SetKinectRecordPath(DataPath);

            //录制Kinect文件和彩色视频文件压缩
            _mainWindow.StartKinectRecord();

            //开启任务计时器
            _mainWindow.getStopWatch().Start();

            this.Close();
            this.OnClosed(e);
        }

        private void c_DataReceived(byte[] readBuffer, int count, int portNumber)
        {
            //往文件写数据
            //this.Dispatcher.Invoke(
            //             DispatcherPriority.SystemIdle,     //在尝试完不同的优先级别后，Important
            //             (System.Windows.Forms.MethodInvoker)delegate()
            //             {
            //                 _mainWindow.GetCommList()[portNumber].fs.Write(readBuffer, 0, count);
            //             });


            //this.Dispatcher.Invoke((System.Windows.Forms.MethodInvoker)delegate
            //{
            //    _mainWindow.GetCommList()[portNumber].fs.Write(readBuffer, 0, count);
            //});
            _mainWindow.GetCommList()[portNumber].fs.Write(readBuffer, 0, count);

        }

        private void Test_No_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.DontTestNow();
            this.Close();
            this.OnClosed(e);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

    }
}
