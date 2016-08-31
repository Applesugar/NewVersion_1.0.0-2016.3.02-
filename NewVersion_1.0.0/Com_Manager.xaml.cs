using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
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
using Microsoft.Win32;
using System.Management;
using System.Globalization;

namespace NewVersion_1._0._0
{
    /// <summary>
    /// Com_Manager.xaml 的交互逻辑
    /// </summary>
    public partial class Com_Manager : Window
    {
        private string iPort;
        private int iRate;
        private byte bSize;
        private int iTimeout;

        private int[] ComInitCheckedChange = new int[50];
        private MainWindow _mainWindow;

        public Com_Manager(MainWindow TheMainWindow)
        {
            InitializeComponent();
            _mainWindow = TheMainWindow;
            for (int i = 0; i < ComInitCheckedChange.Length; i++)
            {
                ComInitCheckedChange[i] = 0;
            }

            iPort = Convert.ToString("com0");
            iRate = Convert.ToInt32(BaundRate.Text);
            bSize = Convert.ToByte(BitsNum.Text, 10);
            iTimeout = 1000;

            //设置窗口显示的位置
            this.WindowStartupLocation = WindowStartupLocation.Manual;
            this.Top = 0;
            this.Left = 60;
        }

        private void ListBox_Initialized(object sender, EventArgs e)
        {

        }

        private void ComList_Loaded(object sender, RoutedEventArgs e)
        {
            ComList.Items.Clear();

            GetComListFromDeviceManager(Getdevice());
        }

        private string Getdevice()
        {
            StringBuilder sbDevHst = new StringBuilder();
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity");
            foreach (ManagementObject mgt in searcher.Get())
            {
                sbDevHst.AppendLine(Convert.ToString(mgt["Name"]));
                sbDevHst.AppendLine("");
            }
            return sbDevHst.ToString();//获取的字符串
        }

        private void bt_Refresh_ComList_Click(object sender, RoutedEventArgs e)
        {
            //刷新串口列表
            for (int i = 0; i < _mainWindow.GetCommList().Length; i++)
            {
                if (i < ComInitCheckedChange.Length)
                {
                    ComInitCheckedChange[i] = 0;
                }
                if (_mainWindow.GetCommList()[i].serialport.IsOpen)
                {
                    _mainWindow.GetCommList()[i].CloseSerialPort();
                }
            }

            ComList.Items.Clear();

            GetComListFromDeviceManager(Getdevice());

        }

        private void GetComListFromDeviceManager(string DeviceData)
        {
            //if (DeviceData.IndexOf("USB-SERIAL CH340 (") == -1)
            //{
            //    if (DeviceData.IndexOf("CH340 (") == -1)
            //    {
            //        DifCondition(DeviceData, "(");
            //    }
            //    else
            //    {
            //        DifCondition(DeviceData, "CH340 (");
            //    }
            //}
            //else
            //{
            //    DifCondition(DeviceData, "USB-SERIAL CH340 (");
            //}
            DifCondition(DeviceData, "(");
        }

        private void DifCondition(string DeviceData, string Findstr)
        {

           int iCount = 0;
            while (DeviceData != "")
            {
                int position = DeviceData.IndexOf(Findstr + "COM");
                if (-1 == position)
                {
                    position = DeviceData.IndexOf(Findstr + "com");
                    if (-1 == position)
                    {
                        position = DeviceData.IndexOf(Findstr + "Com");
                        if (-1 == position)
                        {
                            DeviceData = "";
                        }
                    }
                }
                if (position >= 0)
                {
                    int TestportNumber = -1;
                    int pos = 0;
                    if ((DeviceData[position + Findstr.Length + 3] >= '0' && DeviceData[position + Findstr.Length + 3] <= '9') 
                 && (DeviceData[position + Findstr.Length + 4] >= '0' && DeviceData[position + Findstr.Length + 4] <= '9'))
                    {
                        TestportNumber = (DeviceData[position + Findstr.Length + 3] - '0') * 10 + (DeviceData[position + Findstr.Length + 4] - '0');
                        pos = position + Findstr.Length + 5;
                    }
                    else if (DeviceData[position + Findstr.Length + 3] >= '0' && DeviceData[position + Findstr.Length + 3] <= '9')
                    {
                        TestportNumber = (DeviceData[position + Findstr.Length + 3] - '0');
                        pos = position + Findstr.Length + 4;
                    }
                    else
                    {
                        pos = position + Findstr.Length + 3;
                    }
                    DeviceData = DeviceData.Substring(pos);
                    if (-1 != TestportNumber)
                    {
                        ComInitCheckedChange[iCount++] = TestportNumber;
                    }
                }
            }

            //串口按数值大小进行排序
            for (int i = 0; i < iCount - 1; i++)
            {
                for (int j = i + 1; j < iCount; j++)
                {
                    if (ComInitCheckedChange[i] > ComInitCheckedChange[j])
                    {
                        int temp = ComInitCheckedChange[i];
                        ComInitCheckedChange[i] = ComInitCheckedChange[j];
                        ComInitCheckedChange[j] = temp;
                    }
                }
            }

            //将串口进行显示
            for (int i = 0; i < iCount; i++)
            {
                CheckBox cb = new CheckBox();
                cb.IsChecked = true;
                cb.FontFamily = new System.Windows.Media.FontFamily("宋体");
                cb.FontSize = 15;
                cb.Content = Findstr + "COM" + ComInitCheckedChange[i].ToString() + ")";
                ComList.Items.Add(cb);
            }           
        }

        private void bt_ClearComList_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < _mainWindow.GetCommList().Length; i++)
            {
                if (_mainWindow.GetCommList()[i].serialport.IsOpen)
                {
                    _mainWindow.GetCommList()[i].CloseSerialPort();
                }
            }
            _mainWindow.RefreshComList();
            MessageBox.Show("串口已经全部关闭！\r\n");
            this.Close();
            this.OnClosed(e);
        }

        private void bt_UseComList_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < _mainWindow.GetCommList().Length; i++)
            {
                if (_mainWindow.GetCommList()[i].serialport.IsOpen)
                {
                    _mainWindow.GetCommList()[i].CloseSerialPort();
                }
            }

            int count = 0;

            iRate = Convert.ToInt32(BaundRate.Text);
            bSize = Convert.ToByte(BitsNum.Text, 10);
            iTimeout = 1000;

            for (int i = 0; i < ComList.Items.Count; i++)
            {
                CheckBox cb = (CheckBox)(ComList.Items[i]);
                if (cb.IsChecked == true)
                {
                    //设置并创建所有串口
                    iPort = Convert.ToString("com" + ComInitCheckedChange[i]);
                    _mainWindow.GetCommList()[ComInitCheckedChange[i]].portNumber = ComInitCheckedChange[i];
                    _mainWindow.GetCommList()[ComInitCheckedChange[i]].iPort = iPort;
                    _mainWindow.GetCommList()[ComInitCheckedChange[i]].iRate = iRate;
                    _mainWindow.GetCommList()[ComInitCheckedChange[i]].iTimeout = iTimeout;
                    _mainWindow.GetCommList()[ComInitCheckedChange[i]].bSize = bSize;
                    if (_mainWindow.GetCommList()[ComInitCheckedChange[i]].serialport.IsOpen)
                    {
                        MessageBox.Show("COM " + ComInitCheckedChange[i] + " 之前已经打开！");
                    }
                    else
                    {
                        count++;
                    }
                    _mainWindow.GetCommList()[ComInitCheckedChange[i]].CreateSerialPort();
                }
            }
            _mainWindow.SetComNumber(count);
            _mainWindow.RefreshComList();
            this.Close();
            this.OnClosed(e);
        }

    }
}
