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
using System.ComponentModel;
using Microsoft.Kinect;
using Microsoft.Kinect.Tools;
using System.IO;
using System.IO.Ports;
using System.Windows.Forms;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;
using System.Data;
using System.Drawing;
using Microsoft.Win32;
using System.Data.OleDb;
using System.Windows.Threading;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.DataSources;
using Microsoft.Research.DynamicDataDisplay.ViewportRestrictions;
using System.Diagnostics;

namespace NewVersion_1._0._0
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        //用于找窗口句柄
        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll", EntryPoint = "FindWindowEx", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, uint hwndChildAfter, string lpszClass, string lpszWindow);
        [DllImport("user32.dll", EntryPoint = "SendMessage", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int SendMessage(IntPtr hwnd, uint wMsg, int wParam, int lParam);
        [DllImport("user32.dll", EntryPoint = "SetForegroundWindow", SetLastError = true)]
        private static extern void SetForegroundWindow(IntPtr hwnd);

        #region 常量定义
        //任务完成计时器
        private Stopwatch stopwatch = new Stopwatch();

        //3DShow窗口
        private _3DShow windows_3DShow = null;

        //串口数量
        private const int N = 50;

        //手的尺寸
        private const double HandSize = 80;

        //骨骼的厚度
        private const double JointThickness = 20;

        //摄像机边缘的厚度，用于提示是否碰到了摄像机边缘
        private const double ClipBoundsThickness = 10;

        //摄像空间轴
        private const float InferredZPositionClamp = 0.1f;

        //表示手状态的刷子
        private readonly System.Windows.Media.Brush handClosedBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(128, 255, 0, 0));
        private readonly System.Windows.Media.Brush handOpenBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(128, 0, 255, 0));
        private readonly System.Windows.Media.Brush handLassoBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(128, 0, 0, 255));

        //画骨骼的刷子
        private readonly System.Windows.Media.Brush trackedJointBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 68, 192, 68));
        private readonly System.Windows.Media.Brush inferredJointBrush = System.Windows.Media.Brushes.Yellow;

        //画骨骼的笔
        private readonly System.Windows.Media.Pen inferredBonePen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Gray, 1);

        //单个绘图进行运算的绘图集合
        private DrawingGroup drawingGroup;

        //绘画出来的图像
        private DrawingImage imageSource = null;

        //彩色图图像
        private WriteableBitmap colorBitmap = null;

        //Kinect传感器
        private KinectSensor kinectSensor = null;

        //坐标转换
        private CoordinateMapper coordinateMapper = null;

        //身体Reader
        private BodyFrameReader bodyFrameReader = null;

        //彩色图Reader
        private ColorFrameReader colorFrameReader = null;

        //一共6人的身体
        private Body[] bodies = null;

        //人体的骨额
        private List<Tuple<JointType, JointType>> bones;

        //人体两脚6个点
        private List<JointType> foots;

        //图像的宽度
        private int displayWidth;

        //图像的高度
        private int displayHeight;

        //画身体的笔
        private List<System.Windows.Media.Pen> bodyColors;

        //串口列表
        private com_class[] MyComList = new com_class[N];

        //事件绑定
        public event PropertyChangedEventHandler PropertyChanged;

        //封装的彩色图视频录制类
        private SharpAvi AviWrite = null;

        //任务图像列表上级根目录
        private string ImageBasePath = AppDomain.CurrentDomain.BaseDirectory;

        //Kinect Record Path
        private string KinectRecordPath = "";

        //当前任务索引
        private int TestNum = -1;

        //量表总成绩
        private int Score = 0;

        //串口列表
        bool[] ComList = new bool[N];

        //创建的串口数量
        private int ComNumber = 0;    

        //主窗口图形绘制
        private ObservableDataSource<System.Windows.Point> ax = null;
        private ObservableDataSource<System.Windows.Point> ay = null;
        private ObservableDataSource<System.Windows.Point> az = null;
        private ObservableDataSource<System.Windows.Point> gx = null;
        private ObservableDataSource<System.Windows.Point> gy = null;
        private ObservableDataSource<System.Windows.Point> gz = null;
        private DispatcherTimer timer = new DispatcherTimer();
        private LineGraph graphAx = new LineGraph();
        private LineGraph graphAy = new LineGraph();
        private LineGraph graphAz = new LineGraph();
        private LineGraph graphGx = new LineGraph();
        private LineGraph graphGy = new LineGraph();
        private LineGraph graphGz = new LineGraph();
        private Int16[] ChartPlotterData = new Int16[6];
        private const int LINEWIDTH = 1;
        //ChartPlotter显示窗口
        private const int PLOTTER_N = 500;

        //控制Kinect录制与否
        private int Flag = 0;

        //控制出错后是否改变Flag
        private int Flag_Change = 0;
        
        //选择串口数据用于主窗口图形绘制
        private int PreSelectComShow = -1;
        private int ChartPlotterPortNumber = -1;

        //检测是否按了BeginButton
        private bool IsBeginButtonPressed = true;

        #endregion 常量定义

        public MainWindow()
        {
            //取得传感器
            this.kinectSensor = KinectSensor.GetDefault();

            //取得坐标转换器
            this.coordinateMapper = this.kinectSensor.CoordinateMapper;

            //获取彩色图像信息
            FrameDescription colorFrameDescription = this.kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);

            //打开身体Reader
            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

            //打开彩色图Reader
            this.colorFrameReader = this.kinectSensor.ColorFrameSource.OpenReader();

            //获得骨骼空间坐标
            this.displayWidth = colorFrameDescription.Width;
            this.displayHeight = colorFrameDescription.Height;

            //元组链表作为骨骼
            this.bones = new List<Tuple<JointType, JointType>>();

            // 躯干
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

            // 右臂
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

            // 左臂
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

            // 右腿
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));

            // 左腿
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));

            //添加两脚6个点
            this.foots = new List<JointType>();
            this.foots.Add(JointType.KneeRight);
            this.foots.Add(JointType.KneeLeft);
            this.foots.Add(JointType.AnkleRight);
            this.foots.Add(JointType.AnkleLeft);
            this.foots.Add(JointType.FootRight);
            this.foots.Add(JointType.FootLeft);

            // 每个人身体的颜色
            this.bodyColors = new List<System.Windows.Media.Pen>();

            this.bodyColors.Add(new System.Windows.Media.Pen(System.Windows.Media.Brushes.Red, 15));
            this.bodyColors.Add(new System.Windows.Media.Pen(System.Windows.Media.Brushes.Orange, 15));
            this.bodyColors.Add(new System.Windows.Media.Pen(System.Windows.Media.Brushes.Green, 15));
            this.bodyColors.Add(new System.Windows.Media.Pen(System.Windows.Media.Brushes.Blue, 15));
            this.bodyColors.Add(new System.Windows.Media.Pen(System.Windows.Media.Brushes.Indigo, 15));
            this.bodyColors.Add(new System.Windows.Media.Pen(System.Windows.Media.Brushes.Violet, 15));

            //打开传感器
            this.kinectSensor.Open();

            //创建绘图和绑定窗体
            this.drawingGroup = new DrawingGroup();
            //this.imageSource = new DrawingImage(this.drawingGroup);
            this.DataContext = this;

            //创建位图
            this.colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

            InitializeComponent();

            //初始化串口列表
            for (int i = 0; i < MyComList.Length; i++)
            {
                MyComList[i] = new com_class();
            }

            //设置初始化界面
            PreTargetButton.IsEnabled = false;
            BeginButton.IsEnabled = false;

            //设置窗口显示的位置
            this.WindowStartupLocation = WindowStartupLocation.Manual;
            this.Top = 0;
            this.Left = 60;

            //创建任务结果（文本&图像）保存文件夹
            //string strContentTest = AppDomain.CurrentDomain.BaseDirectory + "//MyContentTest";
            string strContentTest = App.GetPath() + "//MyContentTest";
            if (!Directory.Exists(strContentTest))
            {
                Directory.CreateDirectory(strContentTest);
            }

            //判断有没有日志目录，没有就创建
            //string LogPath = AppDomain.CurrentDomain.BaseDirectory + "//MyContentTest//LogInfo";
            string LogPath = App.GetPath() + "//MyContentTest//LogInfo";
            if (!Directory.Exists(LogPath))
            {
                Directory.CreateDirectory(LogPath);
            }
        }

        //获取彩色图像
        public ImageSource ColorImageSource
        {
            get
            {
                return this.colorBitmap;
            }
        }

        //获取骨骼图像
        public ImageSource ImageSource
        {
            get
            {
                return this.imageSource;
            }
            set
            {
                this.imageSource = (DrawingImage)value;
                if (this.PropertyChanged != null)
                {
                    this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs("ImageSource"));
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //添加身体Reader事件
            if (this.bodyFrameReader != null)
            {
                this.bodyFrameReader.FrameArrived += bodyFrameReader_FrameArrived;
            }

            //添加彩色图Reader事件
            if (this.colorFrameReader != null)
            {
                this.colorFrameReader.FrameArrived += colorFrameReader_FrameArrived;
            }

            AviWrite = new SharpAvi();
            
            string []UserInfo = App.GetInfo().Split('#');
            if (UserInfo.Length == 4)
            {
                Label_Name.Content = "姓名：" + UserInfo[1];
                Label_Numb.Content = "编号：" + UserInfo[0];
                Label_Sex.Content = "性别：" + UserInfo[3];
                string[] Year = UserInfo[2].Split('-');
                string Year_YYYYMMDD = Year[0] + Year[1] + Year[2];
                int now = int.Parse(DateTime.Today.ToString("yyyyMMdd"));
                int dob = int.Parse(Year_YYYYMMDD);
                if (now - dob > 0)
                {
                    string dif = (now - dob).ToString();
                    if (dif.Length > 4)
                    {
                        Label_Year.Content = "年龄：" + dif.Substring(0, dif.Length - 4);
                    }
                    else
                    {
                        Label_Year.Content = "年龄：太小";
                    }
                }
                else
                {
                    Label_Year.Content = "年龄：输错";
                }
            }

        }

        public void SetKinectRecordPath(string DataPath)
        {
            KinectRecordPath = DataPath;
        }

        public void StartKinectRecord()
        {
            Thread mKinectServer = new Thread(KinectRecordSettings);
            mKinectServer.IsBackground = true;
            mKinectServer.Start(KinectRecordPath + TestNum.ToString() + ".xef");
            AviWrite.IsCreateRecord = true;
            AviWrite.IsRecording = true;
        }

        public void KinectRecordSettings(object filePath)
        {
            KStudioClient client;
            KStudioEventStreamSelectorCollection streamCollection;
            KStudioRecording recording;
            try
            {
                using (client = KStudio.CreateClient())
                {

                    client.ConnectToService();
                    streamCollection = new KStudioEventStreamSelectorCollection();
                    streamCollection.Add(KStudioEventStreamDataTypeIds.Ir);
                    streamCollection.Add(KStudioEventStreamDataTypeIds.Depth);
                    streamCollection.Add(KStudioEventStreamDataTypeIds.Body);
                    streamCollection.Add(KStudioEventStreamDataTypeIds.BodyIndex);
                    using (recording = client.CreateRecording((string)filePath, streamCollection))
                    {

                        recording.Start();
                        while (recording.State == KStudioRecordingState.Recording)
                        {
                            if (Flag == 1)
                                break;
                        }
                        if (recording.State == KStudioRecordingState.Error)
                        {
                            throw new InvalidOperationException("Error: Recording failed!");
                        }
                        recording.Stop();

                    }
                    client.DisconnectFromService();
                    Flag = 0;
                }
            }
            catch (InvalidOperationException ex)
            {
                Flag = 0;
                Flag_Change = 1;
                System.Windows.MessageBox.Show("录像异常,错误对象为:" + ex.Source + "\n错误为:" + ex.Message);
            }
            catch (AccessViolationException ex)
            {
                Flag = 0;
                Flag_Change = 1;
                System.Windows.MessageBox.Show("录像异常,错误对象为:" + ex.Source + "\n错误为:" + ex.Message);
            }
            catch (Exception ex)
            {
                Flag = 0;
                Flag_Change = 1;
                System.Windows.MessageBox.Show("录像异常,错误对象为:" + ex.Source + "\n错误为:" + ex.Message);
            }
        }

        public void StopKinectRecord()
        {
            if (Flag_Change != 1)
            {
                Flag = 1;
            }
            else
            {
                Flag_Change = 0;
            }
        }

        //彩色Reader回调函数
        void colorFrameReader_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    //获取彩色坐标信息
                    FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                    //绘制彩色图
                    using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                    {
                        this.colorBitmap.Lock();

                        //分析数据将新的彩色图数据写入位图
                        if ((colorFrameDescription.Width == this.colorBitmap.PixelWidth) && (colorFrameDescription.Height == this.colorBitmap.PixelHeight))
                        {

                            if (AviWrite.IsCreateRecord)
                            {
                                AviWrite.InitRecord(KinectRecordPath + TestNum.ToString() + ".avi");
                            }
                            if (AviWrite.IsRecording)
                            {
                                var frameData = new byte[1920 * 1080 * 4];
                                colorFrame.CopyConvertedFrameDataToArray(frameData, ColorImageFormat.Bgra);
                                AviWrite.Recording(frameData);
                            }
                            colorFrame.CopyConvertedFrameDataToIntPtr(
                                this.colorBitmap.BackBuffer,
                                (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                                ColorImageFormat.Bgra);

                            this.colorBitmap.AddDirtyRect(new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight));
                        }

                        this.colorBitmap.Unlock();
                    }
                }
            }
        }

        //身体Reader回调函数
        void bodyFrameReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            //判断是否检测到身体数据
            bool dataReceived = false;

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    //判断是否有增加的人数
                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                }
            }

            if (dataReceived)
            {
                using (DrawingContext dc = this.drawingGroup.Open())
                {
                    // Draw a transparent background to set the render size
                    dc.DrawRectangle(System.Windows.Media.Brushes.Transparent, null, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));

                    int penIndex = 0;
                    foreach (Body body in this.bodies)
                    {
                        System.Windows.Media.Pen drawPen = this.bodyColors[penIndex++];

                        if (body.IsTracked)
                        {
                            this.DrawClippedEdges(body, dc);

                            IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                            // convert the joint points to depth (display) space
                            Dictionary<JointType, System.Windows.Point> jointPoints = new Dictionary<JointType, System.Windows.Point>();

                            foreach (JointType jointType in joints.Keys)
                            {
                                // sometimes the depth(Z) of an inferred joint may show as negative
                                // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                                CameraSpacePoint position = joints[jointType].Position;
                                if (position.Z < 0)
                                {
                                    position.Z = InferredZPositionClamp;
                                }

                                ColorSpacePoint colorSpacePoint = this.coordinateMapper.MapCameraPointToColorSpace(position);
                                jointPoints[jointType] = new System.Windows.Point(colorSpacePoint.X, colorSpacePoint.Y);
                                //DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position);
                                //jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                            }

                            this.DrawBody(joints, jointPoints, dc, drawPen);
                            this.DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft], dc);
                            this.DrawHand(body.HandRightState, jointPoints[JointType.HandRight], dc);
                        }
                    }

                    // prevent drawing outside of our render area
                    this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //关闭Reader和传感器
            if (this.bodyFrameReader != null)
            {
                this.bodyFrameReader.Dispose();
                this.bodyFrameReader = null;
            }
            if (this.colorFrameReader != null)
            {
                this.colorFrameReader.Dispose();
                this.colorFrameReader = null;
            }
            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
            if (this.AviWrite != null)
            {
                this.AviWrite = null;
            }

            for (int i = 0; i < MyComList.Length; i++)
            {
                if (MyComList[i].serialport.IsOpen)
                {
                    MyComList[i].CloseSerialPort();
                }
            }
        }

        //绘制身体
        private void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, System.Windows.Point> jointPoints, DrawingContext drawingContext, System.Windows.Media.Pen drawingPen)
        {
            // Draw the bones
            foreach (var bone in this.bones)
            {
                this.DrawBone(joints, jointPoints, bone.Item1, bone.Item2, drawingContext, drawingPen);
            }

            // Draw the joints
            foreach (JointType jointType in joints.Keys)
            {
                System.Windows.Media.Brush drawBrush = null;

                TrackingState trackingState = joints[jointType].TrackingState;

                if (trackingState == TrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (trackingState == TrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], JointThickness, JointThickness);
                }
            }
        }

        //绘制骨头
        private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, System.Windows.Point> jointPoints, JointType jointType0, JointType jointType1, DrawingContext drawingContext, System.Windows.Media.Pen drawingPen)
        {
            Joint joint0 = joints[jointType0];
            Joint joint1 = joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == TrackingState.NotTracked ||
                joint1.TrackingState == TrackingState.NotTracked)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            System.Windows.Media.Pen drawPen = this.inferredBonePen;
            if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked))
            {
                drawPen = drawingPen;
            }

            drawingContext.DrawLine(drawPen, jointPoints[jointType0], jointPoints[jointType1]);
        }

        //绘制手的状态
        private void DrawHand(HandState handState, System.Windows.Point handPosition, DrawingContext drawingContext)
        {
            switch (handState)
            {
                case HandState.Closed:
                    drawingContext.DrawEllipse(this.handClosedBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Open:
                    drawingContext.DrawEllipse(this.handOpenBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Lasso:
                    drawingContext.DrawEllipse(this.handLassoBrush, null, handPosition, HandSize, HandSize);
                    break;
            }
        }

        //画摄像头边缘
        private void DrawClippedEdges(Body body, DrawingContext drawingContext)
        {
            FrameEdges clippedEdges = body.ClippedEdges;

            if (clippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    System.Windows.Media.Brushes.Red,
                    null,
                    new Rect(0, this.displayHeight - ClipBoundsThickness, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    System.Windows.Media.Brushes.Red,
                    null,
                    new Rect(0, 0, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    System.Windows.Media.Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, this.displayHeight));
            }

            if (clippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    System.Windows.Media.Brushes.Red,
                    null,
                    new Rect(this.displayWidth - ClipBoundsThickness, 0, ClipBoundsThickness, this.displayHeight));
            }
        }

        //是否显示骨骼
        private void ShowsSkeleton(object sender, RoutedEventArgs e)
        {
            if ((bool)this.CheckBoxShow.IsChecked)
            {
                this.ImageSource = new DrawingImage(this.drawingGroup);

            }
            else
            {
                this.ImageSource = null;
            }
        }

        //设置串口
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Com_Manager win = new Com_Manager(this);
            win.ShowDialog();
            win.Focus();
        }

        //3D显示
        private void _3D_Show_Click(object sender, RoutedEventArgs e)
        {
            if (PreSelectComShow != -1)
            {
                System.Windows.Controls.CheckBox cb = (System.Windows.Controls.CheckBox)(ComListBox.Items[PreSelectComShow]);
                if (cb.IsChecked == true)
                {
                    cb.IsChecked = false;
                }
            }
            windows_3DShow = new _3DShow(this);
            windows_3DShow.ShowDialog();
            windows_3DShow.Focus();
        }

        public class DisplayRange
        {
            public double Start { get; set; }
            public double End { get; set; }

            public DisplayRange(double start, double end)
            {
                Start = start;
                End = end;
            }

        }

        public class ViewportAxesRangeRestriction : IViewportRestriction
        {

            public DisplayRange XRange = null;
            public DisplayRange YRange = null;


            public Rect Apply(Rect oldVisible, Rect newVisible, Viewport2D viewport)
            {

                if (XRange != null)
                {
                    newVisible.X = XRange.Start;
                    newVisible.Width = XRange.End - XRange.Start;
                }

                if (YRange != null)
                {
                    newVisible.Y = YRange.Start;
                    newVisible.Height = YRange.End - YRange.Start;
                }

                return newVisible;
            }

            public event EventHandler Changed;
        }

        //主窗口图形绘制
        private void ChartPlotter_Loaded(object sender, RoutedEventArgs e)
        {
            plotter.Viewport.AutoFitToView = true;
            ViewportAxesRangeRestriction restr = new ViewportAxesRangeRestriction();
            restr.XRange = new DisplayRange(0, PLOTTER_N);
            restr.YRange = new DisplayRange(Int16.MinValue, Int16.MaxValue);
            
            plotter.Viewport.Restrictions.Add(restr);
        }

        private void AnimatedPlot(object sender, EventArgs e)
        {
            if (ChartPlotterPortNumber != -1)
            {
                int x = MyComList[ChartPlotterPortNumber].iCount;

                if (x >= PLOTTER_N)
                {
                    MyComList[ChartPlotterPortNumber].iCount %= PLOTTER_N;

                    plotter.Children.Remove(graphAx);
                    plotter.Children.Remove(graphAy);
                    plotter.Children.Remove(graphAz);
                    plotter.Children.Remove(graphGx);
                    plotter.Children.Remove(graphGy);
                    plotter.Children.Remove(graphGz);

                    ax = new ObservableDataSource<System.Windows.Point>();
                    ay = new ObservableDataSource<System.Windows.Point>();
                    az = new ObservableDataSource<System.Windows.Point>();
                    gx = new ObservableDataSource<System.Windows.Point>();
                    gy = new ObservableDataSource<System.Windows.Point>();
                    gz = new ObservableDataSource<System.Windows.Point>();

                    graphAx = plotter.AddLineGraph(ax, Colors.Red, LINEWIDTH, "Acc_X");
                    graphAy = plotter.AddLineGraph(ay, Colors.Black, LINEWIDTH, "Acc_Y");
                    graphAz = plotter.AddLineGraph(az, Colors.YellowGreen, LINEWIDTH, "Acc_Z");
                    graphGx = plotter.AddLineGraph(gx, Colors.Blue, LINEWIDTH, "Gro_X");
                    graphGy = plotter.AddLineGraph(gy, Colors.Pink, LINEWIDTH, "Gro_Y");
                    graphGz = plotter.AddLineGraph(gz, Colors.Green, LINEWIDTH, "Gro_Z");
                }
                else
                {
                    System.Windows.Point point1 = new System.Windows.Point(x, (double)ChartPlotterData[0]);
                    System.Windows.Point point2 = new System.Windows.Point(x, (double)ChartPlotterData[1]);
                    System.Windows.Point point3 = new System.Windows.Point(x, (double)ChartPlotterData[2]);
                    System.Windows.Point point4 = new System.Windows.Point(x, (double)ChartPlotterData[3]);
                    System.Windows.Point point5 = new System.Windows.Point(x, (double)ChartPlotterData[4]);
                    System.Windows.Point point6 = new System.Windows.Point(x, (double)ChartPlotterData[5]);

                    ax.AppendAsync(base.Dispatcher, point1);
                    ay.AppendAsync(base.Dispatcher, point2);
                    az.AppendAsync(base.Dispatcher, point3);
                    gx.AppendAsync(base.Dispatcher, point4);
                    gy.AppendAsync(base.Dispatcher, point5);
                    gz.AppendAsync(base.Dispatcher, point6);
                }
            }
        }

        //串口接口，方便其它窗口使用
        public com_class[] GetCommList()
        {
            return MyComList;
        }

        //通过选择按钮设置任务
        public void SetTestNum(int num)
        {
            TestNum = num;
        }

        public int GetTestNum()
        {
            return TestNum;
        }
        //获取当前主窗口的ImageShow控件资源

        public System.Windows.Controls.Image GetImageShow()
        {
            return this.ImageShow;
        }

        //获取当前主窗口的PreTargetButton控件资源
        public System.Windows.Controls.Button GetPreTargetButton()
        {
            return PreTargetButton;
        }

        //获取当前主窗口的NextTargetButton控件资源
        public System.Windows.Controls.Button GetNextTargetButton()
        {
            return NextTargetButton;
        }

        public void SetComNumber(int num)
        {
            ComNumber = num;
        }

        //获取当前主窗口的BeginButton控件资源
        public System.Windows.Controls.Button GetBeginButton()
        {
            return BeginButton;
        }

        //获得当前主窗口的StopWatch资源
        public Stopwatch getStopWatch()
        {
            return stopwatch;
        }

        private void CheckBoxShow_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void Image_Loaded(object sender, RoutedEventArgs e)
        {
            string str = AppDomain.CurrentDomain.BaseDirectory;
            BitmapImage Init_image = new BitmapImage(new Uri(str + "//MyPicture//Doctor.png", UriKind.RelativeOrAbsolute));
            ImageShow.Source = Init_image;
        }

        private void ChooseButton_Click(object sender, RoutedEventArgs e)
        {
            SelectTarget win = new SelectTarget(this);
            win.ShowDialog();
            win.Focus();
        }

        private void PreTargetButton_Click(object sender, RoutedEventArgs e)
        {
            BeginButton.IsEnabled = true;
            if (TestNum <= 11)
            {
                NextTargetButton.IsEnabled = true;
            }

            TestNum--;
            BitmapImage Target_Image = new BitmapImage(new Uri(ImageBasePath + "//MyPicture//" + TestNum.ToString() + ".png", UriKind.RelativeOrAbsolute));
            ImageShow.Source = Target_Image;
            if (TestNum <= 0)
            {
                PreTargetButton.IsEnabled = false;
            }
        }

        private void NextTargetButton_Click(object sender, RoutedEventArgs e)
        {
            BeginButton.IsEnabled = true;
            if (TestNum >= 0)
            {
                PreTargetButton.IsEnabled = true;
            }

            TestNum++;
            BitmapImage Target_Image = new BitmapImage(new Uri(ImageBasePath + "//MyPicture//" + TestNum.ToString() + ".png", UriKind.RelativeOrAbsolute));
            ImageShow.Source = Target_Image;
            if (TestNum >= 11)
            {
                NextTargetButton.IsEnabled = false;
            }
        }

        private void ComListBox_Loaded(object sender, RoutedEventArgs e)
        {
            //for (int i = 0; i < ComList.Length; i++)
            //{
            //    ComList[i] = false;
            //}
            //for (int i = 0; i < MyComList.Length; i++)
            //{
            //    if (MyComList[i].serialport.IsOpen)
            //    {
            //        ComList[i] = true;
            //    }
            //}
            //for (int i = 0; i < ComList.Length; i++)
            //{
            //    if (ComList[i])
            //    {
            //        System.Windows.Controls.CheckBox cb = new System.Windows.Controls.CheckBox();
            //        cb.IsChecked = false;
            //        Color color = Color.FromArgb(255, 255, 255, 255);
            //        cb.Foreground = new SolidColorBrush(color);
            //        cb.FontFamily = new System.Windows.Media.FontFamily("微软宋体");
            //        cb.FontSize = 15;
            //        cb.Content = "COM" + i.ToString();
            //        cb.Click += new System.Windows.RoutedEventHandler(this.ComListBoxSelected);
            //        ComListBox.Items.Add(cb);
            //    }
            //}
        }

        public void RefreshComList() 
        {
            //刷新串口
            ComListBox.Items.Clear();
            for (int i = 0; i < ComList.Length; i++)
            {
                ComList[i] = false;
            }
            for (int i = 0; i < MyComList.Length; i++)
            {
                if (MyComList[i].serialport.IsOpen)
                {
                    ComList[i] = true;
                }
            }
            for (int i = 0; i < ComList.Length; i++)
            {
                if (ComList[i])
                {
                    System.Windows.Controls.CheckBox cb = new System.Windows.Controls.CheckBox();
                    cb.IsChecked = false;
                    System.Windows.Media.Color color = System.Windows.Media.Color.FromArgb(255, 255, 255, 255);
                    cb.Foreground = new SolidColorBrush(color);
                    cb.FontFamily = new System.Windows.Media.FontFamily("微软宋体");
                    cb.FontSize = 15;
                    cb.Content = "COM" + i.ToString();
                    cb.Click += new System.Windows.RoutedEventHandler(this.ComListBoxSelected);
                    ComListBox.Items.Add(cb);
                }
            }
        }

        //ChartPlotter显示选择的串口数据
        private void ComListBoxSelected(object sender, RoutedEventArgs e)
        {
            int ComListSelectCount = 0;
            int NowSelectComShow = 0;
            System.Windows.Controls.CheckBox NowSelectCheckBox = (System.Windows.Controls.CheckBox)sender;
            string ComName = NowSelectCheckBox.Content.ToString();
            for (int i = 0; i < ComListBox.Items.Count; i++)
            {
                System.Windows.Controls.CheckBox cb = (System.Windows.Controls.CheckBox)(ComListBox.Items[i]);
                if (cb.IsChecked == true)
                {
                    ComListSelectCount++;
                }
                string str = cb.Content.ToString();
                if (str == ComName)
                {
                    NowSelectComShow = i;
                }
            }
            int portNumber = -1;
            //if (ComListSelectCount != 0)
            //{
                //ComListSelectCount == 0，说明关闭了图形显示串口
                System.Windows.Controls.CheckBox Nowcb = (System.Windows.Controls.CheckBox)(ComListBox.Items[NowSelectComShow]);
                string Nowstr = Nowcb.Content.ToString();
                if (Nowstr.Length == 4)
                {
                    portNumber = Nowstr[3] - '0';
                }
                else if (Nowstr.Length == 5)
                {
                    portNumber = (Nowstr[3] - '0') * 10 + (Nowstr[4] - '0');
                }
            //}
            if (ComListSelectCount == 0)
            {
                PreSelectComShow = -1;
                //关闭了图形显示串口，portNumber即为关闭的串口
                MyComList[portNumber].ChartPlotterShow = false;
                MyComList[portNumber].EndReadData();
                ChartPlotterStopRecord();
                ChartPlotterPortNumber = -1;
            }
            else if (ComListSelectCount == 1)
            {
                PreSelectComShow = NowSelectComShow;
                ChartPlotterPortNumber = portNumber;

                if (MyComList[portNumber].serialport.IsOpen)
                {
                    //开启串口，显示串口图形
                    MyComList[portNumber].ChartPlotterShow = true; 
                    MyComList[portNumber].portNumber = portNumber;
                    MyComList[portNumber].ChartPlotterDataShow = c_ChartPlotterDataShow;
                    MyComList[portNumber].StartReadData();
                    ChartPlotterBeginRecord();
                }
            }
            else
            {
                //当开启了两个以上的串口的时候，关闭先前串口的图形显示，开启新的串口的图形显示
                System.Windows.Controls.CheckBox cb_pre = (System.Windows.Controls.CheckBox)(ComListBox.Items[PreSelectComShow]);
                cb_pre.IsChecked = false;
                ComListSelectCount -= 1;

                //将上一个串口数据读取线程关闭
                string str = cb_pre.Content.ToString();
                int prePortNumber = -1;
                if (str.Length == 4)
                {
                    prePortNumber = str[3] - '0';
                }
                else if (str.Length == 5)
                {
                    prePortNumber = (str[3] - '0') * 10 + (str[4] - '0');
                }
                if (prePortNumber != -1) 
                {
                    //关闭ChartPlotter数据显示
                    MyComList[prePortNumber].ChartPlotterShow = false;
                    MyComList[prePortNumber].EndReadData();
                    ChartPlotterStopRecord();
                }

                //开启新的串口数据读取线程
                PreSelectComShow = NowSelectComShow;
                ChartPlotterPortNumber = portNumber;
                if (MyComList[portNumber].serialport.IsOpen)
                {
                    //开启串口，显示串口图形
                    ChartPlotterBeginRecord();
                    MyComList[portNumber].ChartPlotterShow = true;
                    MyComList[portNumber].portNumber = portNumber;
                    MyComList[portNumber].ChartPlotterDataShow = c_ChartPlotterDataShow;
                    MyComList[portNumber].StartReadData();
                }
            }
        }

        public int IsChartPlotterWork()
        {
            return ChartPlotterPortNumber;
        }

        private void c_ChartPlotterDataShow(byte[] ComDataBuffer, int count)
        {
             //对于得到的数据，先进行转换后再显示
            ChartPlotterData[0] = (Int16)(((UInt16)ComDataBuffer[0] << 8) | ((UInt16)ComDataBuffer[1]));
            ChartPlotterData[1] = (Int16)(((UInt16)ComDataBuffer[2] << 8) | ((UInt16)ComDataBuffer[3]));
            ChartPlotterData[2] = (Int16)(((UInt16)ComDataBuffer[4] << 8) | ((UInt16)ComDataBuffer[5]));
            ChartPlotterData[3] = (Int16)(((UInt16)ComDataBuffer[6] << 8) | ((UInt16)ComDataBuffer[7]));
            ChartPlotterData[4] = (Int16)(((UInt16)ComDataBuffer[8] << 8) | ((UInt16)ComDataBuffer[9]));
            ChartPlotterData[5] = (Int16)(((UInt16)ComDataBuffer[10] << 8) | ((UInt16)ComDataBuffer[11]));
        }

        private void BeginButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsBeginButtonPressed)
            {
                IsBeginButtonPressed = false;
                PreTargetButton.IsEnabled = false;
                NextTargetButton.IsEnabled = false;
                ChooseButton.IsEnabled = false;
                BeginButton.Content = "停止";
                ConfirmTest ConfirmTestWin = new ConfirmTest(this);
                ConfirmTestWin.ShowDialog();
                ConfirmTestWin.Focus();
            }
            else
            {
                //暂停计时器
                stopwatch.Stop();
                double time_used = stopwatch.Elapsed.TotalSeconds;
                stopwatch.Reset();
                ChangeLabelShow(time_used, TestNum);

                IsBeginButtonPressed = true;
                ChooseButton.IsEnabled = true;
                if (TestNum > 0)
                {
                    PreTargetButton.IsEnabled = true;
                }
                if (TestNum < 11)
                {
                    NextTargetButton.IsEnabled = true;
                }
                BeginButton.Content = "开始";
                for (int i = 0; i < MyComList.Length; i++)
                {
                    if (MyComList[i].serialport.IsOpen)
                    {
                        if (MyComList[i].fs != null)
                        {
                            MyComList[i].fs.Close();
                            MyComList[i].fs = null;
                            MyComList[i].FileWrite = false;
                            if (MyComList[i].ChartPlotterShow != true)
                            {
                                MyComList[i].EndReadData();
                            }
                        }
                    }
                }

                Thread mTcpServerThread = new Thread(StopKinectRecord);
                mTcpServerThread.IsBackground = true;
                mTcpServerThread.Start();
                if (AviWrite.GetWrite() != null)
                {
                    AviWrite.Stoping(); 
                }

            }
        }

        //统计成绩
        private void ChangeLabelShow(double time_used, int TestNumber)
        {
            int test_count = Directory.GetDirectories(App.GetPath() + "//MyContentTest" + "//", "Target" + TestNumber.ToString() + "*").Length;
            switch (TestNumber)
            {
                case 1:
                    if (test_count > 2 || (test_count == 2 && time_used > 45.0) || (test_count == 1 && time_used > 45.0))
                    {
                        Target1_F.Content = "0";
                        Target1_S.Content = "0";
                        Target1_T.Content = "";
                    }
                    else if (test_count == 2)
                    {
                        Target1_T.Content = time_used.ToString();
                        Target1_S.Content = "1";
                        Target1_F.Content = "";
                        Score += 1;
                    }
                    else
                    {
                        Target1_T.Content = time_used.ToString();
                        Target1_S.Content = "2";
                        Target1_F.Content = "";
                        Score += 2;
                    }
                    if (Score < 10)
                    {
                        ResultLable.Content = "评分（总分0" + Score.ToString() + "分）";
                    }
                    else
                    {
                        ResultLable.Content = "评分（总分" + Score.ToString() + "分）";
                    }
                    break;
                case 2:
                    if (test_count > 2 || (test_count == 2 && time_used > 45.0) || (test_count == 1 && time_used > 45.0))
                    {
                        Target2_F.Content = "0";
                        Target2_S.Content = "0";
                        Target2_T.Content = "";
                    }
                    else if (test_count == 2)
                    {
                        Target2_T.Content = time_used.ToString();
                        Target2_S.Content = "1";
                        Target2_F.Content = "";
                        Score += 1;
                    }
                    else
                    {
                        Target2_T.Content = time_used.ToString();
                        Target2_S.Content = "2";
                        Target2_F.Content = "";
                        Score += 2;
                    }
                    if (Score < 10)
                    {
                        ResultLable.Content = "评分（总分0" + Score.ToString() + "分）";
                    }
                    else
                    {
                        ResultLable.Content = "评分（总分" + Score.ToString() + "分）";
                    }
                    break;
                case 3:
                    if (test_count > 2 || (test_count == 2 && time_used > 45.0) || (test_count == 1 && time_used > 45.0))
                    {
                        Target3_F.Content = "0";
                        Target3_S.Content = "0";
                        Target3_T.Content = "";
                    }
                    else if (test_count == 2)
                    {
                        Target3_T.Content = time_used.ToString();
                        Target3_S.Content = "1";
                        Target3_F.Content = "";
                        Score += 1;
                    }
                    else
                    {
                        Target3_T.Content = time_used.ToString();
                        Target3_S.Content = "2";
                        Target3_F.Content = "";
                        Score += 2;
                    }
                    if (Score < 10)
                    {
                        ResultLable.Content = "评分（总分0" + Score.ToString() + "分）";
                    }
                    else
                    {
                        ResultLable.Content = "评分（总分" + Score.ToString() + "分）";
                    }
                    break;
                case 4:
                    if (test_count >= 2)
                    {
                        if (Target4_S.Content.ToString().Length > 0)
                        {
                            Score -= (int)(Target4_S.Content.ToString()[0] - '0');
                        }
                    }
                    if (time_used > 45.0)
                    {
                        Target4_F.Content = "0";
                        Target4_S.Content = "0";
                        Target4_T.Content = "";
                    }
                    else if (time_used >= 21.0 && time_used <= 45.0)
                    {
                        Target4_T.Content = time_used.ToString();
                        Target4_S.Content = "4";
                        Target4_F.Content = "";
                        Score += 4;
                    }
                    else if (time_used >= 16.0 && time_used < 21.0)
                    {
                        Target4_T.Content = time_used.ToString();
                        Target4_S.Content = "5";
                        Target4_F.Content = "";
                        Score += 5;
                    }
                    else if (time_used >= 11.0 && time_used < 16.0)
                    {
                        Target4_T.Content = time_used.ToString();
                        Target4_S.Content = "6";
                        Target4_F.Content = "";
                        Score += 6;
                    }
                    else
                    {
                        Target4_T.Content = time_used.ToString();
                        Target4_S.Content = "7";
                        Target4_F.Content = "";
                        Score += 7;
                    }
                    if (Score < 10)
                    {
                        ResultLable.Content = "评分（总分0" + Score.ToString() + "分）";
                    }
                    else
                    {
                        ResultLable.Content = "评分（总分" + Score.ToString() + "分）";
                    }
                    break;
                case 5:
                    if (test_count >= 2)
                    {
                        if (Target5_S.Content.ToString().Length > 0)
                        {
                            Score -= (int)(Target5_S.Content.ToString()[0] - '0');
                        }
                    }
                    if (time_used > 75.0)
                    {
                        Target5_F.Content = "0";
                        Target5_T.Content = "";
                        Target5_S.Content = "0";
                    }
                    else if (time_used >= 21.0 && time_used <= 75.0)
                    {
                        Target5_T.Content = time_used.ToString();
                        Target5_S.Content = "4";
                        Target5_F.Content = "";
                        Score += 4;
                    }
                    else if (time_used >= 16.0 && time_used < 21.0)
                    {
                        Target5_T.Content = time_used.ToString();
                        Target5_S.Content = "5";
                        Target5_F.Content = "";
                        Score += 5;
                    }
                    else if (time_used >= 11.0 && time_used < 16.0)
                    {
                        Target5_T.Content = time_used.ToString();
                        Target5_S.Content = "6";
                        Target5_F.Content = "";
                        Score += 6;
                    }
                    else
                    {
                        Target5_T.Content = time_used.ToString();
                        Target5_S.Content = "7";
                        Target5_F.Content = "";
                        Score += 7;
                    }
                    if (Score < 10)
                    {
                        ResultLable.Content = "评分（总分0" + Score.ToString() + "分）";
                    }
                    else
                    {
                        ResultLable.Content = "评分（总分" + Score.ToString() + "分）";
                    }
                    break;
                case 6:
                    if (test_count >= 2)
                    {
                        if (Target6_S.Content.ToString().Length > 0)
                        {
                            Score -= (int)(Target6_S.Content.ToString()[0] - '0');
                        }
                    }
                    if (time_used > 75.0)
                    {
                        Target6_T.Content = "";
                        Target6_F.Content = "0";
                        Target6_S.Content = "0";
                    }
                    else if (time_used >= 21.0 && time_used <= 75.0)
                    {
                        Target6_T.Content = time_used.ToString();
                        Target6_S.Content = "4";
                        Target6_F.Content = "";
                        Score += 4;
                    }
                    else if (time_used >= 16.0 && time_used < 21.0)
                    {
                        Target6_T.Content = time_used.ToString();
                        Target6_S.Content = "5";
                        Target6_F.Content = "";
                        Score += 5;
                    }
                    else if (time_used >= 11.0 && time_used < 16.0)
                    {
                        Target6_T.Content = time_used.ToString();
                        Target6_S.Content = "6";
                        Target6_F.Content = "";
                        Score += 6;
                    }
                    else
                    {
                        Target6_T.Content = time_used.ToString();
                        Target6_S.Content = "7";
                        Target6_F.Content = "";
                        Score += 7;
                    }
                    if (Score < 10)
                    {
                        ResultLable.Content = "评分（总分0" + Score.ToString() + "分）";
                    }
                    else
                    {
                        ResultLable.Content = "评分（总分" + Score.ToString() + "分）";
                    }
                    break;
                case 7:
                    if (test_count >= 2)
                    {
                        if (Target7_S.Content.ToString().Length > 0)
                        {
                            Score -= (int)(Target7_S.Content.ToString()[0] - '0');
                        }
                    }
                    if (time_used > 75.0)
                    {
                        Target7_T.Content = "";
                        Target7_F.Content = "0";
                        Target7_S.Content = "0";
                    }
                    else if (time_used >= 21.0 && time_used <= 75.0)
                    {
                        Target7_T.Content = time_used.ToString();
                        Target7_S.Content = "4";
                        Target7_F.Content = "";
                        Score += 4;
                    }
                    else if (time_used >= 16.0 && time_used < 21.0)
                    {
                        Target7_T.Content = time_used.ToString();
                        Target7_S.Content = "5";
                        Target7_F.Content = "";
                        Score += 5;
                    }
                    else if (time_used >= 11.0 && time_used < 16.0)
                    {
                        Target7_T.Content = time_used.ToString();
                        Target7_S.Content = "6";
                        Target7_F.Content = "";
                        Score += 6;
                    }
                    else
                    {
                        Target7_T.Content = time_used.ToString();
                        Target7_S.Content = "7";
                        Target7_F.Content = "";
                        Score += 7;
                    }
                    if (Score < 10)
                    {
                        ResultLable.Content = "评分（总分0" + Score.ToString() + "分）";
                    }
                    else
                    {
                        ResultLable.Content = "评分（总分" + Score.ToString() + "分）";
                    }
                    break;
                case 8:
                    if (test_count >= 2)
                    {
                        if (Target8_S.Content.ToString().Length > 0)
                        {
                            Score -= (int)(Target8_S.Content.ToString()[0] - '0');
                        }
                    }
                    if (time_used > 75.0)
                    {
                        Target8_T.Content = "";
                        Target8_F.Content = "0";
                        Target8_S.Content = "0";
                    }
                    else if (time_used >= 26.0 && time_used <= 75.0)
                    {
                        Target8_T.Content = time_used.ToString();
                        Target8_S.Content = "4";
                        Target8_F.Content = "";
                        Score += 4;
                    }
                    else if (time_used >= 21.0 && time_used < 26.0)
                    {
                        Target8_T.Content = time_used.ToString();
                        Target8_S.Content = "5";
                        Target8_F.Content = "";
                        Score += 5;
                    }
                    else if (time_used >= 16.0 && time_used < 21.0)
                    {
                        Target8_T.Content = time_used.ToString();
                        Target8_S.Content = "6";
                        Target8_F.Content = "";
                        Score += 6;
                    }
                    else
                    {
                        Target8_T.Content = time_used.ToString();
                        Target8_S.Content = "7";
                        Target8_F.Content = "";
                        Score += 7;
                    }
                    if (Score < 10)
                    {
                        ResultLable.Content = "评分（总分0" + Score.ToString() + "分）";
                    }
                    else
                    {
                        ResultLable.Content = "评分（总分" + Score.ToString() + "分）";
                    }
                    break;
                case 9:
                    if (test_count >= 2)
                    {
                        if (Target9_S.Content.ToString().Length > 0)
                        {
                            Score -= (int)(Target9_S.Content.ToString()[0] - '0');
                        }
                    }
                    if (time_used > 120.0)
                    {
                        Target9_T.Content = "";
                        Target9_F.Content = "0";
                        Target9_S.Content = "0";
                    }
                    else if (time_used >= 56.0 && time_used <= 120.0)
                    {
                        Target9_T.Content = time_used.ToString();
                        Target9_S.Content = "4";
                        Target9_F.Content = "";
                        Score += 4;
                    }
                    else if (time_used >= 36.0 && time_used < 56.0)
                    {
                        Target9_T.Content = time_used.ToString();
                        Target9_S.Content = "5";
                        Target9_F.Content = "";
                        Score += 5;
                    }
                    else if (time_used >= 26.0 && time_used < 36.0)
                    {
                        Target9_T.Content = time_used.ToString();
                        Target9_S.Content = "6";
                        Target9_F.Content = "";
                        Score += 6;
                    }
                    else
                    {
                        Target9_T.Content = time_used.ToString();
                        Target9_S.Content = "7";
                        Target9_F.Content = "";
                        Score += 7;
                    }
                    if (Score < 10)
                    {
                        ResultLable.Content = "评分（总分0" + Score.ToString() + "分）";
                    }
                    else
                    {
                        ResultLable.Content = "评分（总分" + Score.ToString() + "分）";
                    }
                    break;
                case 10:
                    if (test_count >= 2)
                    {
                        if (Target10_S.Content.ToString().Length > 0)
                        {
                            Score -= (int)(Target10_S.Content.ToString()[0] - '0');
                        }
                    }
                    if (time_used > 120.0)
                    {
                        Target10_T.Content = "";
                        Target10_F.Content = "0";
                        Target10_S.Content = "0";
                    }
                    else if (time_used >= 76.0 && time_used <= 120.0)
                    {
                        Target10_T.Content = time_used.ToString();
                        Target10_S.Content = "4";
                        Target10_F.Content = "";
                        Score += 4;
                    }
                    else if (time_used >= 56.0 && time_used < 76.0)
                    {
                        Target10_T.Content = time_used.ToString();
                        Target10_S.Content = "5";
                        Target10_F.Content = "";
                        Score += 5;
                    }
                    else if (time_used >= 41.0 && time_used < 56.0)
                    {
                        Target10_T.Content = time_used.ToString();
                        Target10_S.Content = "6";
                        Target10_F.Content = "";
                        Score += 6;
                    }
                    else
                    {
                        Target10_T.Content = time_used.ToString();
                        Target10_S.Content = "7";
                        Target10_F.Content = "";
                        Score += 7;
                    }
                    if (Score < 10)
                    {
                        ResultLable.Content = "评分（总分0" + Score.ToString() + "分）";
                    }
                    else
                    {
                        ResultLable.Content = "评分（总分" + Score.ToString() + "分）";
                    }
                    break;
                case 11:
                    if (test_count >= 2)
                    {
                        if (Target11_S.Content.ToString().Length > 0)
                        {
                            Score -= (int)(Target11_S.Content.ToString()[0] - '0');
                        }
                    }
                    if (time_used > 120.0)
                    {
                        Target11_T.Content = "";
                        Target11_F.Content = "0";
                        Target11_S.Content = "0";
                    }
                    else if (time_used >= 81.0 && time_used <= 120.0)
                    {
                        Target11_T.Content = time_used.ToString();
                        Target11_S.Content = "4";
                        Target11_F.Content = "";
                        Score += 4;
                    }
                    else if (time_used >= 56.0 && time_used < 81.0)
                    {
                        Target11_T.Content = time_used.ToString();
                        Target11_S.Content = "5";
                        Target11_F.Content = "";
                        Score += 5;
                    }
                    else if (time_used >= 41.0 && time_used < 56.0)
                    {
                        Target11_T.Content = time_used.ToString();
                        Target11_S.Content = "6";
                        Target11_F.Content = "";
                        Score += 6;
                    }
                    else
                    {
                        Target11_T.Content = time_used.ToString();
                        Target11_S.Content = "7";
                        Target11_F.Content = "";
                        Score += 7;
                    }
                    if (Score < 10)
                    {
                        ResultLable.Content = "评分（总分0" + Score.ToString() + "分）";
                    }
                    else
                    {
                        ResultLable.Content = "评分（总分" + Score.ToString() + "分）";
                    }
                    break;
            }
        }

        //选择暂时不进行测试
        public void DontTestNow()
        {
            IsBeginButtonPressed = true;
            BeginButton.IsEnabled = true;
            ChooseButton.IsEnabled = true;
            if (TestNum > 0)
            {
                PreTargetButton.IsEnabled = true;
            }
            if (TestNum < 11)
            {
                NextTargetButton.IsEnabled = true;
            }
            BeginButton.Content = "开始";
        }

        private void BeginRecord_Loaded(object sender, RoutedEventArgs e)
        {
            string str = AppDomain.CurrentDomain.BaseDirectory;
            BitmapImage Init_image = new BitmapImage(new Uri(str + "//MyPicture//start.png", UriKind.RelativeOrAbsolute));
            BeginRecord.Source = Init_image;
        }

        private void StopRecord_Loaded(object sender, RoutedEventArgs e)
        {
            string str = AppDomain.CurrentDomain.BaseDirectory;
            BitmapImage Init_image = new BitmapImage(new Uri(str + "//MyPicture//stop.png", UriKind.RelativeOrAbsolute));
            StopRecord.Source = Init_image;
        }

        private void RefreshRecord_Loaded(object sender, RoutedEventArgs e)
        {
            string str = AppDomain.CurrentDomain.BaseDirectory;
            BitmapImage Init_image = new BitmapImage(new Uri(str + "//MyPicture//refresh.png", UriKind.RelativeOrAbsolute));
            RefreshRecord.Source = Init_image;
        }

        private void BeginRecord_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ChartPlotterPortNumber != -1)
            {
                //关闭ChartPlotter数据显示
                MyComList[ChartPlotterPortNumber].ChartPlotterShow = false;
                MyComList[ChartPlotterPortNumber].EndReadData();
                ChartPlotterStopRecord();

                //开启串口，显示串口图形
                ChartPlotterBeginRecord();
                MyComList[ChartPlotterPortNumber].ChartPlotterShow = true;
                MyComList[ChartPlotterPortNumber].portNumber = ChartPlotterPortNumber;
                MyComList[ChartPlotterPortNumber].ChartPlotterDataShow = c_ChartPlotterDataShow;
                MyComList[ChartPlotterPortNumber].StartReadData();
            }
        }

        //private void ChartPlotterBeginRecord()
        //{
        //    //每次过来一个数据就创建一个线程，可想开销之大！
        //    //Thread mChartPlotter = new Thread(AnimatedPlot);
        //    //mChartPlotter.IsBackground = true;
        //    //mChartPlotter.Start();

        //    //单独让主线程去处理，无暇顾及！
        //    //AnimatedPlot();

        //    this.Dispatcher.Invoke(
        //                             DispatcherPriority.ContextIdle,
        //                             (System.Windows.Forms.MethodInvoker)delegate()
        //                             {
        //                                 if (ChartPlotterPortNumber != -1)
        //                                 {
        //                                     double x = MyComList[ChartPlotterPortNumber].iCount;

        //                                     Point point1 = new Point(x, (double)ChartPlotterData[0]);
        //                                     Point point2 = new Point(x, (double)ChartPlotterData[1]);
        //                                     Point point3 = new Point(x, (double)ChartPlotterData[2]);
        //                                     Point point4 = new Point(x, (double)ChartPlotterData[3]);
        //                                     Point point5 = new Point(x, (double)ChartPlotterData[4]);
        //                                     Point point6 = new Point(x, (double)ChartPlotterData[5]);

        //                                     ax.AppendAsync(Dispatcher, point1);
        //                                     ay.AppendAsync(Dispatcher, point2);
        //                                     az.AppendAsync(Dispatcher, point3);
        //                                     gx.AppendAsync(Dispatcher, point4);
        //                                     gy.AppendAsync(Dispatcher, point5);
        //                                     gz.AppendAsync(Dispatcher, point6);
        //                                 }
        //                             });   
        //}

        public void ChartPlotterBeginRecord()
        {
            ax = new ObservableDataSource<System.Windows.Point>();
            ay = new ObservableDataSource<System.Windows.Point>();
            az = new ObservableDataSource<System.Windows.Point>();
            gx = new ObservableDataSource<System.Windows.Point>();
            gy = new ObservableDataSource<System.Windows.Point>();
            gz = new ObservableDataSource<System.Windows.Point>();

            graphAx = plotter.AddLineGraph(ax, Colors.Red, LINEWIDTH, "Acc_X");
            graphAy = plotter.AddLineGraph(ay, Colors.Black, LINEWIDTH, "Acc_Y");
            graphAz = plotter.AddLineGraph(az, Colors.YellowGreen, LINEWIDTH, "Acc_Z");
            graphGx = plotter.AddLineGraph(gx, Colors.Blue, LINEWIDTH, "Gro_X");
            graphGy = plotter.AddLineGraph(gy, Colors.Pink, LINEWIDTH, "Gro_Y");
            graphGz = plotter.AddLineGraph(gz, Colors.Green, LINEWIDTH, "Gro_Z");

            timer.Interval = TimeSpan.FromMilliseconds(1);
            timer.Tick += AnimatedPlot;
            timer.IsEnabled = true;

            plotter.Viewport.FitToView();
        }

        private void StopRecord_MouseDown(object sender, MouseButtonEventArgs e)
        {
            timer.IsEnabled = false;
        }

        private void RefreshRecord_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ChartPlotterStopRecord();
        }

        public void ChartPlotterStopRecord()
        {
            if (ChartPlotterPortNumber != -1)
            {
                MyComList[ChartPlotterPortNumber].iCount = 0;
                timer.IsEnabled = false;
                plotter.Children.Remove(graphAx);
                plotter.Children.Remove(graphAy);
                plotter.Children.Remove(graphAz);
                plotter.Children.Remove(graphGx);
                plotter.Children.Remove(graphGy);
                plotter.Children.Remove(graphGz);

                //ax = new ObservableDataSource<Point>();
                //ay = new ObservableDataSource<Point>();
                //az = new ObservableDataSource<Point>();
                //gx = new ObservableDataSource<Point>();
                //gy = new ObservableDataSource<Point>();
                //gz = new ObservableDataSource<Point>();
            }
        }
    }
}
