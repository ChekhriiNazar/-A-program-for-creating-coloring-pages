using System;
using System.Drawing;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace курсова
{
    public partial class Form1 : Form
    {
        private VideoCapture videoCapture;
        private Bitmap originalFrame;
        private Bitmap editedFrame;
        private bool isVideoPlaying;
        private double contourContrast;

        public Form1()
        {
            InitializeComponent();
            trackBar.Minimum = 0;
            trackBar.Maximum = 10;
            trackBar.Value = 0;
            trackBar.TickFrequency = 1;
            trackBar.Scroll += TrackBar_Scroll;
        }

        private void Open_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.bmp|Video Files|*.mp4;*.avi;*.mov;*.mkv;*.wmv";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                if (openFileDialog.FilterIndex == 1)
                {
                    originalFrame = new Bitmap(openFileDialog.FileName);
                    videoCapture = null;
                    pictureBox.Image = originalFrame;
                }
                else if (openFileDialog.FilterIndex == 2)
                {
                    videoCapture = new VideoCapture(openFileDialog.FileName);
                    originalFrame = null;
                    editedFrame = null;
                    isVideoPlaying = false;
                    Video();
                }
            }
        }

        private void Video()
        {
            if (!isVideoPlaying)
            {
                double minutes = Convert.ToDouble(Minute.Text);
                double seconds = Convert.ToDouble(Second1.Text);
                double totalSeconds = (minutes * 60) + seconds;

                videoCapture.Set(VideoCaptureProperties.PosMsec, totalSeconds * 1000);
                isVideoPlaying = true;
            }

            Mat frame = new Mat();
            if (!videoCapture.Read(frame) || frame.Empty())
            {
                MessageBox.Show("Неможливо прочитати кадр відео", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            originalFrame = BitmapConverter.ToBitmap(frame);
            editedFrame = null;
            pictureBox.Image = originalFrame;
        }

        private void Contours_Click(object sender, EventArgs e)
        {
            if (originalFrame == null && (videoCapture == null || !videoCapture.IsOpened()))
            {
                MessageBox.Show("Спочатку відкрийте зображення або відео", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (originalFrame != null)
            {
                Mat srcImage = BitmapConverter.ToMat(originalFrame);
                Mat grayImage = new Mat();
                Cv2.CvtColor(srcImage, grayImage, ColorConversionCodes.BGR2GRAY);

                Mat cannyImage = new Mat();
                Cv2.Canny(grayImage, cannyImage, 50, 150);

                Mat contoursImage = new Mat();
                Cv2.CvtColor(cannyImage, contoursImage, ColorConversionCodes.GRAY2BGR);
                OpenCvSharp.Point[][] contours;
                HierarchyIndex[] hierarchy;
                Cv2.FindContours(cannyImage, out contours, out hierarchy, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple);
                Cv2.DrawContours(contoursImage, contours, -1, Scalar.Black, -1, LineTypes.Link8, hierarchy);
                Cv2.BitwiseNot(cannyImage, cannyImage);
                contoursImage.SetTo(Scalar.White, cannyImage);
                for (int i = 0; i < contours.Length; i++)
                {
                    Scalar color = new Scalar(0, 0, 0);
                    int thickness = (int)(0 + contourContrast);
                    Cv2.DrawContours(contoursImage, contours, i, color, thickness, LineTypes.Link8, hierarchy);
                }


                editedFrame = BitmapConverter.ToBitmap(contoursImage);
                pictureBox.Image = editedFrame;
            }
            else if (videoCapture != null && videoCapture.IsOpened())
            {
                Video();
            }
        }

        private void Save_Click(object sender, EventArgs e)
        {
            if (editedFrame == null)
            {
                MessageBox.Show("Немає відредагованого зображення", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "JPEG Image|*.jpg";
            saveFileDialog.Title = "Зберегти відредаговане зображення";
            saveFileDialog.ShowDialog();

            if (saveFileDialog.FileName != "")
            {
                editedFrame.Save(saveFileDialog.FileName);
            }
        }

        private void Clear_Click(object sender, EventArgs e)
        {
            pictureBox.Image = null;
            editedFrame = null;
        }

        private void TrackBar_Scroll(object sender, EventArgs e)
        {
            contourContrast = trackBar.Value;
            Contours_Click(sender, e);
        }
    }
}