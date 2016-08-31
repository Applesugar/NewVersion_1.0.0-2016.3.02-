using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpAvi;
using SharpAvi.Output;
using SharpAvi.Codecs;

namespace NewVersion_1._0._0
{
     class SharpAvi
    {
        //AVi生成类
        private AviWriter writer = null;

        //视频流
        private IAviVideoStream stream = null;

        //是否创建视频文件
        public Boolean IsCreateRecord = false;

        public Boolean IsRecording = false;

        public void InitRecord(String filePath)
        {
            try
            {
                writer = new AviWriter(filePath)
                {
                    FramesPerSecond = 30,
                    // Emitting AVI v1 index in addition to OpenDML index (AVI v2)
                    // improves compatibility with some software, including 
                    // standard Windows programs like Media Player and File Explorer
                    EmitIndex1 = true
                };

                stream = writer.AddMpeg4VideoStream(1920, 1080, 30,
                quality: 70, codec: KnownFourCCs.Codecs.X264, forceSingleThreadedAccess: true);

                IsCreateRecord = false;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
                WriteEduAppLog(ex.Message, ex.StackTrace);
            }
        }

        public void WriteEduAppLog(string ErrorReason, string StackTrace)
        {
            WriteLog(ErrorReason, StackTrace, "//AviCompress.log");
        }

        private void WriteLog(string ErrorReason, string StackTrace, string logFileName)
        {
            //Log文件路径
            string LogPath = AppDomain.CurrentDomain.BaseDirectory + "//MyContentTest//LogInfo";

            //彩色图像压缩的异常写入到AviCompress日志文件中
            StringBuilder logInfo = new StringBuilder("");
            string currentTime = System.DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss]");
            logInfo.Append("\n").Append(currentTime).Append("：").Append(ErrorReason).Append("\n").Append(StackTrace);
            System.IO.File.AppendAllText(LogPath + logFileName, logInfo.ToString());
        }

        public void Recording(byte[] frameData)
        {
            stream.WriteFrame(true, // is key frame? (many codecs use concept of key frames, for others - all frames are keys)
                                              frameData, // array with frame data
                                              0, // starting index in the array
                                              frameData.Length // length of the data
            );
        }

        public void Stoping()
        {
            writer.Close();
            writer = null;
            stream = null;

            IsCreateRecord = false;
            IsRecording = false;
        }

        public AviWriter GetWrite()
        {
            return writer;
        }

    }
}
