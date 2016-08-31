using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows;
using System.IO.Ports;
using SharpGL;

namespace NewVersion_1._0._0
{
    public partial class _3DShow : Form
    {
        private const int N = 50;
        private bool[] ComList = new bool[N];
        private uint[] textures = new uint[1];
        private Bitmap textureImage;
        private static Int16 x = 0;
        private static Int16 y = 0;
        private static Int16 z = 0;
        private MainWindow _mainWindow;

        //选择串口数据用于主窗口图形绘制
        private int PreSelectComShow = -1;
        private int ChartPlotterPortNumber = -1;
        public _3DShow(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            this.KeyPreview = true;
            this.KeyPress += new KeyPressEventHandler(Form_KeyPress);
            if (_mainWindow.IsChartPlotterWork() >= 0)
            {
                _mainWindow.GetCommList()[_mainWindow.IsChartPlotterWork()].ChartPlotterShow = false;
                _mainWindow.GetCommList()[_mainWindow.IsChartPlotterWork()].EndReadData();
                _mainWindow.ChartPlotterStopRecord();
            }
            ComListBox_Init();
        }

        private void ComListBox_Init()
        {
            //刷新串口
            ComListBox.Items.Clear();
            for (int i = 0; i < ComList.Length; i++)
            {
                ComList[i] = false;
            }
            for (int i = 0; i < _mainWindow.GetCommList().Length; i++)
            {
                if (_mainWindow.GetCommList()[i].serialport.IsOpen)
                {
                    ComList[i] = true;
                }
            }
            for (int i = 0; i < ComList.Length; i++)
            {
                if (ComList[i])
                {
                    CheckBox cb = new CheckBox();
                    cb.Checked = false;
                    cb.ForeColor = System.Drawing.Color.White; 
                    cb.Font = new Font("微软宋体", 13);
                    cb.Text = "COM" + i.ToString();
                    cb.Click += new EventHandler(this.ComListBoxSelected);
                    ComListBox.Controls.Add(cb);
                    ComListBox.Items.Add(cb);
                }
            }
        }

        private void ComListBoxSelected(object sender, EventArgs e)
        {
            int ComListSelectCount = 0;
            int NowSelectComShow = 0;
            CheckBox NowSelectCheckBox = (CheckBox)sender;
            string ComName = NowSelectCheckBox.Text.ToString();
            for (int i = 0; i < ComListBox.Items.Count; i++)
            {
                CheckBox cb = (CheckBox)(ComListBox.Items[i]);
                if (cb.Checked == true)
                {
                    ComListSelectCount++;
                }
                string str = cb.Text.ToString();
                if (str == ComName)
                {
                    NowSelectComShow = i;
                }
            }
            int portNumber = -1;
            //if (ComListSelectCount != 0)
            //{
            //ComListSelectCount == 0，说明关闭了图形显示串口
            CheckBox Nowcb = (CheckBox)(ComListBox.Items[NowSelectComShow]);
            string Nowstr = Nowcb.Text.ToString();
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
                //关闭了图形显示串口，portNumber即为关闭的串口
                PreSelectComShow = -1;
                _mainWindow.GetCommList()[portNumber].ChartPlotterShow = false;
                _mainWindow.GetCommList()[portNumber].EndReadData();
                ChartPlotterPortNumber = -1;
            }
            else if (ComListSelectCount == 1)
            {
                PreSelectComShow = NowSelectComShow;
                ChartPlotterPortNumber = portNumber;

                if (_mainWindow.GetCommList()[portNumber].serialport.IsOpen)
                {
                    //开启串口，显示串口图形
                    _mainWindow.GetCommList()[portNumber].ChartPlotterShow = true;
                    _mainWindow.GetCommList()[portNumber].portNumber = portNumber;
                    _mainWindow.GetCommList()[portNumber].ChartPlotterDataShow = c_ChartPlotterDataShow;
                    if (_mainWindow.GetCommList()[portNumber]._keepReading == false) 
                    {
                        _mainWindow.GetCommList()[portNumber].StartReadData();
                    }
                }
            }
            else
            {
                //当开启了两个以上的串口的时候，关闭先前串口的图形显示，开启新的串口的图形显示
                CheckBox cb_pre = (CheckBox)(ComListBox.Items[PreSelectComShow]);
                cb_pre.Checked = false;
                ComListSelectCount -= 1;

                //将上一个串口数据读取线程关闭
                string str = cb_pre.Text.ToString();
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
                    _mainWindow.GetCommList()[prePortNumber].ChartPlotterShow = false;
                    _mainWindow.GetCommList()[prePortNumber].EndReadData();
                }

                //开启新的串口数据读取线程
                PreSelectComShow = NowSelectComShow;
                ChartPlotterPortNumber = portNumber;
                if (_mainWindow.GetCommList()[portNumber].serialport.IsOpen)
                {
                    //开启串口，显示串口图形
                    _mainWindow.GetCommList()[portNumber].ChartPlotterShow = true;
                    _mainWindow.GetCommList()[portNumber].portNumber = portNumber;
                    _mainWindow.GetCommList()[portNumber].ChartPlotterDataShow = c_ChartPlotterDataShow;
                    if (_mainWindow.GetCommList()[portNumber]._keepReading == false)
                    {
                        _mainWindow.GetCommList()[portNumber].StartReadData();
                    }
                }
            }
        }

        private void c_ChartPlotterDataShow(byte[] ComDataBuffer, int count)
        {
            //对于得到的数据，先进行转换后再显示
            Int16 temp_x, temp_y, temp_z;
            temp_x = (Int16)(((UInt16)ComDataBuffer[12] << 8) | ((UInt16)ComDataBuffer[13]));
            temp_y = (Int16)(((UInt16)ComDataBuffer[14] << 8) | ((UInt16)ComDataBuffer[15]));
            temp_z = (Int16)(((UInt16)ComDataBuffer[16] << 8) | ((UInt16)ComDataBuffer[17]));
            if (temp_x > -360 && temp_x < 360 && temp_y > -360 && temp_y < 360 && temp_z > -360 && temp_z < 360)
            {
                x = temp_x; y = temp_y; z = temp_z;
                SetText(x.ToString(), y.ToString(), z.ToString());
            }
        }

        private void SetText(string text1, string text2, string text3)
        {
            try
            {
                this.Invoke((EventHandler)delegate
                {
                    label1.Text = "Pitch:" + text1;
                });
                this.Invoke((EventHandler)delegate
                {
                    label2.Text = " Roll:" + text2;
                });
                this.Invoke((EventHandler)delegate
                {
                    label3.Text = "  Yaw:" + text3;
                });
            }
            catch (Exception)
            {

            }
        }


        private void Form_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 27)
            {
                for (int i = 0; i < _mainWindow.GetCommList().Length; i++)
                {
                    if (_mainWindow.GetCommList()[i].serialport.IsOpen)
                    {
                        _mainWindow.GetCommList()[i].ChartPlotterShow = false;
                        _mainWindow.GetCommList()[i].EndReadData();
                        _mainWindow.ChartPlotterStopRecord();
                        _mainWindow.GetCommList()[i].serialport.DiscardInBuffer();
                    }
                }
                //if (_mainWindow.IsChartPlotterWork() >= 0)
                //{
                //    _mainWindow.GetCommList()[_mainWindow.IsChartPlotterWork()].ChartPlotterShow = true;
                //    _mainWindow.GetCommList()[_mainWindow.IsChartPlotterWork()].StartReadData();
                //    _mainWindow.ChartPlotterBeginRecord();
                //}
                this.Dispose();
                this.Close();
            }
        }

        private void openGLControl1_OpenGLDraw(object sender, SharpGL.RenderEventArgs args)
        {
            SharpGL.OpenGL gl = this.openGLControl1.OpenGL;
            gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);
            gl.LoadIdentity();
            gl.Translate(0.0f, 0.0f, -6.0f);

            //绘制正方体，Rotate The Quad On The X, Y, And Z Axes 
            gl.Rotate(x, -1.0f, 0.0f, 0.0f);
            gl.Rotate(y, 0.0f, 0.0f, 1.0f);
            gl.Rotate(z, 0.0f, 1.0f, 0.0f);

            gl.BindTexture(OpenGL.GL_TEXTURE_2D, textures[0]);

            gl.Begin(OpenGL.GL_QUADS);

            // Front Face
            gl.TexCoord(0.0f, 0.0f); gl.Vertex(-1.0f, -1.0f, 1.0f);	// Bottom Left Of The Texture and Quad
            gl.TexCoord(1.0f, 0.0f); gl.Vertex(1.0f, -1.0f, 1.0f);	// Bottom Right Of The Texture and Quad
            gl.TexCoord(1.0f, 1.0f); gl.Vertex(1.0f, 1.0f, 1.0f);	// Top Right Of The Texture and Quad
            gl.TexCoord(0.0f, 1.0f); gl.Vertex(-1.0f, 1.0f, 1.0f);	// Top Left Of The Texture and Quad

            // Back Face
            gl.TexCoord(1.0f, 0.0f); gl.Vertex(-1.0f, -1.0f, -1.0f);	// Bottom Right Of The Texture and Quad
            gl.TexCoord(1.0f, 1.0f); gl.Vertex(-1.0f, 1.0f, -1.0f);	// Top Right Of The Texture and Quad
            gl.TexCoord(0.0f, 1.0f); gl.Vertex(1.0f, 1.0f, -1.0f);	// Top Left Of The Texture and Quad
            gl.TexCoord(0.0f, 0.0f); gl.Vertex(1.0f, -1.0f, -1.0f);	// Bottom Left Of The Texture and Quad

            // Top Face
            gl.TexCoord(0.0f, 1.0f); gl.Vertex(-1.0f, 1.0f, -1.0f);	// Top Left Of The Texture and Quad
            gl.TexCoord(0.0f, 0.0f); gl.Vertex(-1.0f, 1.0f, 1.0f);	// Bottom Left Of The Texture and Quad
            gl.TexCoord(1.0f, 0.0f); gl.Vertex(1.0f, 1.0f, 1.0f);	// Bottom Right Of The Texture and Quad
            gl.TexCoord(1.0f, 1.0f); gl.Vertex(1.0f, 1.0f, -1.0f);	// Top Right Of The Texture and Quad

            // Bottom Face
            gl.TexCoord(1.0f, 1.0f); gl.Vertex(-1.0f, -1.0f, -1.0f);	// Top Right Of The Texture and Quad
            gl.TexCoord(0.0f, 1.0f); gl.Vertex(1.0f, -1.0f, -1.0f);	// Top Left Of The Texture and Quad
            gl.TexCoord(0.0f, 0.0f); gl.Vertex(1.0f, -1.0f, 1.0f);	// Bottom Left Of The Texture and Quad
            gl.TexCoord(1.0f, 0.0f); gl.Vertex(-1.0f, -1.0f, 1.0f);	// Bottom Right Of The Texture and Quad

            // Right face
            gl.TexCoord(1.0f, 0.0f); gl.Vertex(1.0f, -1.0f, -1.0f);	// Bottom Right Of The Texture and Quad
            gl.TexCoord(1.0f, 1.0f); gl.Vertex(1.0f, 1.0f, -1.0f);	// Top Right Of The Texture and Quad
            gl.TexCoord(0.0f, 1.0f); gl.Vertex(1.0f, 1.0f, 1.0f);	// Top Left Of The Texture and Quad
            gl.TexCoord(0.0f, 0.0f); gl.Vertex(1.0f, -1.0f, 1.0f);	// Bottom Left Of The Texture and Quad

            // Left Face
            gl.TexCoord(0.0f, 0.0f); gl.Vertex(-1.0f, -1.0f, -1.0f);	// Bottom Left Of The Texture and Quad
            gl.TexCoord(1.0f, 0.0f); gl.Vertex(-1.0f, -1.0f, 1.0f);	// Bottom Right Of The Texture and Quad
            gl.TexCoord(1.0f, 1.0f); gl.Vertex(-1.0f, 1.0f, 1.0f);	// Top Right Of The Texture and Quad
            gl.TexCoord(0.0f, 1.0f); gl.Vertex(-1.0f, 1.0f, -1.0f);	// Top Left Of The Texture and Quad
            gl.End();

            gl.Flush();

        }

        private void openGLControl1_OpenGLInitialized(object sender, EventArgs e)
        {
            SharpGL.OpenGL gl = this.openGLControl1.OpenGL;
            textureImage = new Bitmap("MyPicture/Texture.bmp");
            gl.Enable(OpenGL.GL_TEXTURE_2D);
            gl.GenTextures(1, textures);
            gl.BindTexture(OpenGL.GL_TEXTURE_2D, textures[0]);
            gl.TexImage2D(OpenGL.GL_TEXTURE_2D, 0, 3, textureImage.Width, textureImage.Height, 0, OpenGL.GL_BGR, OpenGL.GL_UNSIGNED_BYTE,
                textureImage.LockBits(new Rectangle(0, 0, textureImage.Width, textureImage.Height),
                ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb).Scan0);
            gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MIN_FILTER, OpenGL.GL_LINEAR);
            gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MAG_FILTER, OpenGL.GL_LINEAR);
        }

        private void label4_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < _mainWindow.GetCommList().Length; i++)
            {
                if (_mainWindow.GetCommList()[i].serialport.IsOpen)
                {
                    _mainWindow.GetCommList()[i].ChartPlotterShow = false;
                    _mainWindow.GetCommList()[i].EndReadData();
                    _mainWindow.ChartPlotterStopRecord();
                    _mainWindow.GetCommList()[i].serialport.DiscardInBuffer();
                }
            }
            //if (_mainWindow.IsChartPlotterWork() >= 0)
            //{
            //    _mainWindow.GetCommList()[_mainWindow.IsChartPlotterWork()].ChartPlotterShow = true;
            //    _mainWindow.GetCommList()[_mainWindow.IsChartPlotterWork()].StartReadData();
            //    _mainWindow.ChartPlotterBeginRecord();
            //}
            this.Dispose();
            this.Close();
        }

    }
}
