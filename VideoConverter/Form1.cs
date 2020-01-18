using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Enums;

namespace VideoConverter
{
    public partial class Form1 : Form
    {
        // 작업 정지 토큰
        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        public Form1()
        {
            InitializeComponent();
            // 유저의 직접적인 환경변수 등록과정 제외를 위해 설정
            FFmpeg.ExecutablesPath = Application.StartupPath + @"\ffmpeg";
            
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            // 정지 버튼

            label1.Text = "작업 중지됨";
            cancellationTokenSource.Cancel();
        }

        private void loadButton_Click(object sender, EventArgs e)
        {
            // 파일 불러오기 버튼
            // 비디오 확장자만 담아냄

            using(OpenFileDialog open = new OpenFileDialog())
            {
                open.RestoreDirectory = true;

                open.Filter = "Video Files |*.mkv;*.avi;*.mp4;*.mpg;*.flv;*.wmv;*.asf;*.asx;*.ogm;*.ogv;*.mov";

                if(open.ShowDialog() == DialogResult.OK)
                {
                    string path = open.FileName;
                    listBox1.Items.Clear();
                    listBox1.Items.Add(path);
                }
            }
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            // 시작 버튼
            // mp4 확장자로만 변환

            string outputPath;

            using (SaveFileDialog save = new SaveFileDialog())
            {
                save.Filter = "mp4 Files|*.mp4";

                if (save.ShowDialog() == DialogResult.OK)
                {
                    outputPath = save.FileName;
                    RunConversion(listBox1.Items[0].ToString(), outputPath);
                }
            }

        }

        private async void RunConversion(string inputPath, string outputPath)
        {
            // 변환 함수
            // 하드웨어 가속 중 퀵싱크 사용
            // 인텔 cpu에서만 작동할줄 알았는데 AMD 라이젠 3세대(3600x), 비쉐라(FX8300) 에서도 동작 확인
            // 왜 되는거징

            var mediaInfo = await MediaInfo.Get(inputPath);
            var videoStream = mediaInfo.VideoStreams.First();
            var audioStream = mediaInfo.AudioStreams.First();

            var conversion = Conversion.New()
                .UseHardwareAcceleration(HardwareAccelerator.qsv, VideoCodec.H264, VideoCodec.Libx264)
                .AddStream(videoStream)
                .AddStream(audioStream)
                .SetOutput(outputPath)
                .SetOverwriteOutput(true)
                .UseMultiThread(true)
                .SetPreset(ConversionPreset.UltraFast);


            label1.Text = "작업 진행중...";
            progressBar1.Value = 0;

            conversion.OnProgress += (sender, args) =>
            {
                this.Invoke(new Action(
                delegate ()
                {
                    progressBar1.Value = args.Percent;

                    if (args.Percent == 100)
                    {
                        label1.Text = "작업 완료";
                    }

                }));
            };

            try
            {
                await conversion.Start(cancellationTokenSource.Token);
            }

            catch
            {
                return;
            }

        }

        private void Form_Closing(object sender, FormClosingEventArgs e)
        {
            cancellationTokenSource.Cancel();
            Dispose();
            Close();
        }
    }
}
