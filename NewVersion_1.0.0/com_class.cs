using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Win32;
using System.IO.Ports;
using System.Threading;
using System.Windows;

namespace NewVersion_1._0._0
{
    public class com_class
    {
        //串口参数
        public String iPort;
        public int iRate;
        public byte bSize;
        public int iTimeout;
        public SerialPort serialport;

        //文件操作参数
        public bool FileWrite = false;
        public FileStream fs = null;
        public string FilePath = ""; 
        
        //多线程参数
        public Thread _readThread;
        public bool _keepReading;
        
        //数据通信参数
        public long receivedCount = 0;      //串口读取的字节数量
        public int portNumber = 0;            //当前操作的串口名数字，如4（COM4）

        public delegate void EventHandle_FileDataReceived(byte[] param, int count, int portNum);
        public EventHandle_FileDataReceived DataReceived;

        public delegate void EventHandle_ChartPlotterDataShow(byte[] param, int count);
        public EventHandle_ChartPlotterDataShow ChartPlotterDataShow;

        //数据长度
        private const int PACKAGE_LENGTH = 37;
        private const int DATA_LENGTH = 34;
        private const int DATA_USED_LENGTH = 18;
        private const int DATA_TIME_LENGTH = 4;
        private const byte START_FLAG = (byte)255;
        private const byte END_FLAG = (byte)238;

        //决定处理效率和ChartPlotter的刷新效率
        private const int READ_LENGTH = 100;

        //ChartPlotter记录游标
        public int iCount = 0;
        public bool ChartPlotterShow = false;

        //初始化串口类
        public com_class()
        {
            iPort = "com1";
            iRate = 115200;
            bSize = 8;
            iTimeout = 50;
            serialport = new SerialPort();
        }

        //设置串口参数，创建串口
        public void CreateSerialPort()
        {
            if (!serialport.IsOpen)
            {
                Parity myParity = Parity.None;
                StopBits myStopBits = StopBits.One;

                serialport.PortName = iPort;
                serialport.BaudRate = iRate;
                serialport.DataBits = bSize;
                serialport.Parity = myParity;
                serialport.StopBits = myStopBits;
                serialport.ReadTimeout = iTimeout;
                serialport.ReadBufferSize = 10000;

                serialport.Open();
            }
        }

        //开始读取数据，_keepReading置为真，创建读取线程
        public void StartReadData()
        {
            //使用线程方法
            _keepReading = true;
            _readThread = new Thread(ReadPort);
            _readThread.IsBackground = true;
            _readThread.Start();

            //使用串口DataRecieved
            //_keepReading = true;
            //serialport.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

        }

        //暂停数据读取，_keepReading置为假，线程结束
        public void EndReadData()
        {
            if (_keepReading)
            {
                _keepReading = false;
                iCount = 0;
                //结束线程
                //_readThread.Join();
            }
        }

        //关闭串口，同时清空文件路径
        public void CloseSerialPort()
        {
            if (serialport.IsOpen)
            {
                _keepReading = false;
                ChartPlotterShow = false;
                FileWrite = false;
                receivedCount = 0;
                serialport.Close();
                if (fs != null)
                {
                    fs.Flush();
                    fs.Close();
                    fs = null;
                    FilePath = "";
                }
            }
        }

        //读串口线程
        private void ReadPort()
        {
            //大致分为两种情况，
            //1.需要图形显示的数据被中间隔断了；
            //2.需要图形显示的数据没有被隔断.

            //图形显示数据接口，当接满DATA_USED_LENGTH个字节后立马处理
            int ChartPlotterIndex = 0;

            //需要图形显示的数据是否被中间隔断了
            bool PreProcessNotEnd = false;
            //如果被中间隔断，那么记录上一次数据包处理了多少个字节
            int PrePorcessLeavedBytes = 0;
            //如果被中间隔断，那么上一次遇到头的时候剩下的字节数的校验结果
            byte PreProcessOddEvenCheck = (byte)0;

            int count = 0;
            byte[] readBuffer = new byte[serialport.ReadBufferSize + 1];
            byte[] ChartPlotterData = new byte[DATA_USED_LENGTH];

            while(_keepReading)
            { 
                if (serialport.IsOpen)
                {
                    //先将文件按照原始数据进行记录，最后再将文件进行统一的处理
                    // [1byte][4-2-2-2-2-2-2-2-2-2-2-2-2-2-2-2][1byte][1byte]
                    try
                    {
                        // If there are bytes available on the serial port,  
                        // Read returns up to "count" bytes, but will not block (wait)  
                        // for the remaining bytes. If there are no bytes available  
                        // on the serial port, Read will block until at least one byte  
                        // is available on the port, up until the ReadTimeout milliseconds  
                        // have elapsed, at which time a TimeoutException will be thrown.
                        count += serialport.Read(readBuffer, count, READ_LENGTH);

                        if (count >= READ_LENGTH)
                        {

                            if (FileWrite == true)
                            {
                                //往文件里边写数据
                                DataReceived(readBuffer, count, portNumber);
                            }

                            //数据处理，先处理上一次残留的数据
                            int countBeginPosition = 0;
                            if (PreProcessNotEnd)
                            {
                                if (count < PACKAGE_LENGTH - PrePorcessLeavedBytes)
                                {
                                    //如果上下两次接收的数据总和还是很少，改进以后就不会出现这种情况了
                                    while (countBeginPosition < count)
                                    {
                                        //两种情况，1.上一次已经处理了12个字节；2.上一次处理的字节数少于12个字节
                                        if (ChartPlotterIndex < DATA_USED_LENGTH)
                                        {
                                            ChartPlotterData[ChartPlotterIndex++] = readBuffer[countBeginPosition];
                                        }
                                        PreProcessOddEvenCheck ^= readBuffer[countBeginPosition];
                                        PrePorcessLeavedBytes++;
                                        countBeginPosition++;
                                    }
                                    PreProcessNotEnd = true;
                                }
                                else if (DATA_LENGTH - PrePorcessLeavedBytes + 1 >= 0 && readBuffer[DATA_LENGTH - PrePorcessLeavedBytes + 1] == END_FLAG)
                                {
                                    //已经处理的+此次接收的包的长度大于PACKAGE_LENGTH && 找到了结尾
                                    for (int i = 0; i <= DATA_LENGTH - PrePorcessLeavedBytes + 1; i++)
                                    {
                                        if (ChartPlotterIndex < DATA_USED_LENGTH)
                                        {
                                            ChartPlotterData[ChartPlotterIndex++] = readBuffer[i];
                                        }
                                        PreProcessOddEvenCheck ^= readBuffer[i];
                                    }
                                    if (ChartPlotterIndex == DATA_USED_LENGTH && PreProcessOddEvenCheck == readBuffer[DATA_LENGTH - PrePorcessLeavedBytes + 2])
                                    {
                                        //校验码相同&&接收满了12个数据，可以用于图形显示了
                                        countBeginPosition = PACKAGE_LENGTH - PrePorcessLeavedBytes;   //新一次数据起始位置
                                        if (ChartPlotterShow == true)
                                        {
                                            //图形化显示当前串口数据
                                            ChartPlotterDataShow(ChartPlotterData, DATA_USED_LENGTH);
                                            iCount++;
                                        }
                                    }
                                    else
                                    {
                                        countBeginPosition = 0;   //新一次数据起始位置
                                    }
                                    ChartPlotterIndex = 0;      //清空数据
                                    //置零
                                    PreProcessNotEnd = false;
                                    PrePorcessLeavedBytes = 0;
                                    PreProcessOddEvenCheck = (byte)0;
                                }
                                else
                                {
                                    ChartPlotterIndex = 0;      //清空数据
                                    countBeginPosition = 0;   //新一次数据起始位置
                                    //置零
                                    PreProcessNotEnd = false;
                                    PrePorcessLeavedBytes = 0;
                                    PreProcessOddEvenCheck = (byte)0;
                                }
                            }
                            else
                            {
                                ChartPlotterIndex = 0;      //清空数据
                                countBeginPosition = 0;   //新一次数据起始位置
                            }
                            for (int i = countBeginPosition; i < count; i++)
                            {
                                byte uc = readBuffer[i];
                                byte oddevencheck = (byte)0;

                                if (uc == START_FLAG)
                                {
                                    if (i + PACKAGE_LENGTH >= count)
                                    {
                                        //找到了头部的START_FLAG，1.此时的数据被隔断
                                        PrePorcessLeavedBytes = 0;
                                        while (i < count)
                                        {
                                            if (ChartPlotterIndex < DATA_USED_LENGTH && PrePorcessLeavedBytes > DATA_TIME_LENGTH)
                                            {
                                                ChartPlotterData[ChartPlotterIndex++] = readBuffer[i + DATA_TIME_LENGTH + 1];
                                            }
                                            PreProcessOddEvenCheck ^= readBuffer[i];
                                            i++;
                                            PrePorcessLeavedBytes++;
                                        }
                                        PreProcessNotEnd = true;
                                    }
                                    else
                                    {
                                        //找到了头部的START_FLAG，2.此时的数据没有被隔断
                                        PreProcessNotEnd = false;
                                        PrePorcessLeavedBytes = 0;
                                        PreProcessOddEvenCheck = (byte)0;

                                        if (readBuffer[DATA_LENGTH + i + 1] == END_FLAG)
                                        {
                                            for (int j = 0; j <= DATA_LENGTH + 1; j++)
                                            {
                                                if (ChartPlotterIndex < DATA_USED_LENGTH && j > DATA_TIME_LENGTH)
                                                {
                                                    ChartPlotterData[ChartPlotterIndex++] = readBuffer[i + j];
                                                }
                                                oddevencheck ^= readBuffer[i + j];
                                            }
                                            if (ChartPlotterIndex == DATA_USED_LENGTH && oddevencheck == readBuffer[i + DATA_LENGTH + 2])
                                            {
                                                i += DATA_LENGTH;
                                                //校验码相同&&接收满了12个数据，可以用于图形显示了
                                                if (ChartPlotterShow == true)
                                                {
                                                    //图形化显示当前串口数据
                                                    ChartPlotterDataShow(ChartPlotterData, DATA_USED_LENGTH);
                                                    iCount++;
                                                }
                                            }
                                            else
                                            {
                                                //校验码不正确
                                            }
                                            ChartPlotterIndex = 0;      //清空数据
                                        }
                                        else
                                        {
                                            ChartPlotterIndex = 0;      //清空数据
                                        }
                                    }
                                }
                                else
                                {
                                    //如果不是头，那么不做任何处理
                                }
                            }
                            count = 0;
                        }
                        
                    }
                    catch (TimeoutException) 
                    { 
                        //对于超时而言，应该有相应的措施进行容错，可是有的情况下超时是被允许的
                    }
                    catch (ArgumentNullException)
                    {
                        //传递的缓冲区 null。
                    }
                    catch (InvalidOperationException)
                    {
                        //指定的端口未打开。
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        //offset 或 count 参数都是有效的区域之外 buffer 传递。要么 offset 或 count 小于零
                    }
                    catch (ArgumentException)
                    {
                        //offset 加上 count 的长度大于 buffer。
                    }
                    catch (IOException)
                    {
                        //线程中途退出
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("ReadPort出错");
                        _keepReading = false;
                        if (fs != null)
                        {
                            fs.Close();
                            fs = null;
                            FileWrite = false;
                            EndReadData();
                        }
                        WriteEduAppLog(ex.Message, ex.StackTrace);
                    }
                }
                else
                {
                    _keepReading = false;
                }
            }
        }

        public void WriteEduAppLog(string ErrorReason, string StackTrace)
        {
            //错误日志
            WriteLog(ErrorReason, StackTrace, "//ComRead.log");
        }

        private void WriteLog(string ErrorReason, string StackTrace, string logFileName)
        {
            //Log文件路径
            string LogPath = AppDomain.CurrentDomain.BaseDirectory + "//MyContentTest//LogInfo";
            if (!Directory.Exists(LogPath))
            {
                //串口操作的异常写入到ComRead日志文件中
                StringBuilder logInfo = new StringBuilder("");
                string currentTime = System.DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss]");
                logInfo.Append("\n").Append(currentTime).Append("：").Append(ErrorReason).Append("\n").Append(StackTrace);
                System.IO.File.AppendAllText(LogPath + logFileName, logInfo.ToString());
            }
        }
    }
}
