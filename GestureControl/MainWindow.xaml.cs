using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Collections.Generic;
using System.Windows.Interop;
using Emgu.CV.Util;
using Emgu.CV.CvEnum;

namespace GestureControl
{
    public partial class MainWindow : Window
    {
        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);
        private VideoCapture capture;
        private bool CaptureInProgress = false;

        MCvScalar redColor = new MCvScalar(0, 0, 255);
        MorphOp firstMorphOp = MorphOp.Erode;
        MorphOp secondMorphOp = MorphOp.Erode;
        Mat kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new System.Drawing.Size(3, 3), new System.Drawing.Point(-1, -1));
        Mat kernel2 = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new System.Drawing.Size(3, 3), new System.Drawing.Point(-1, -1));
        int firstMorphSteps = 1;
        int secondMorphSteps = 1;
        double lowerTresholdLevel;
        double upperTresholdLevel;
        int cannyKernelSize;
        RetrType contouringMode = RetrType.External;
        ChainApproxMethod contouringMethod = ChainApproxMethod.ChainApproxSimple;

        bool doubleMorph = false;

        public MainWindow()
        {
            InitializeComponent();
            lowerTresholdLevel = LowerTresholdLevel.Value;
            upperTresholdLevel = UpperTresholdLevel.Value;
            cannyKernelSize = (int)CannyKernelSize.SelectedValue;
            capture = new VideoCapture();
            capture.ImageGrabbed += ProcessFrame;
        }

        private void CameraControl_Click(object sender, RoutedEventArgs e)
        {
            if (capture != null)
            {
                if (CaptureInProgress)
                {  
                    CameraControl.Content = "Start camera";
                    capture.Pause();
                }
                else
                {
                    CameraControl.Content = "Stop camera";
                    capture.Start();
                }
                CaptureInProgress = !CaptureInProgress;
            }
        }

        private void ProcessFrame(object sender, EventArgs arg)
        {
            Mat frame = new Mat();
            capture.Retrieve(frame, 0);
            //preprocessing
            Image<Bgr, byte> finalImg = frame.ToImage<Bgr, byte>().Flip(FlipType.Horizontal);
            Image<Gray, byte> processingImg = finalImg.Convert<Gray, byte>();
            BiTonalLevel.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (BiTonalLevel.Value > 0)
                    processingImg = processingImg.ThresholdBinary(new Gray(BiTonalLevel.Value), new Gray(255));
            }));
            BlurLevel.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (BlurLevel.Value > 1)
                    CvInvoke.Blur(processingImg, processingImg, new System.Drawing.Size((int)BlurLevel.Value, (int)BlurLevel.Value), new System.Drawing.Point(-1, -1));
            }));
            //morphological processing
            processingImg.MorphologyEx(firstMorphOp, kernel, new System.Drawing.Point(-1, -1), firstMorphSteps, BorderType.Default, new MCvScalar());
            if (doubleMorph)
                processingImg.MorphologyEx(secondMorphOp, kernel2, new System.Drawing.Point(-1, -1), secondMorphSteps, BorderType.Default, new MCvScalar());
            ProcessingVideoBox.Dispatcher.BeginInvoke(new Action(() => ProcessingVideoBox.Source = ToBitmapGrey(processingImg)));
            //edge detection
            Mat edges = new Mat(frame.Size, frame.Depth, 1);
            CvInvoke.Canny(processingImg, edges, lowerTresholdLevel, upperTresholdLevel, cannyKernelSize);
            //contours finding
            VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
            Mat hierarchy = new Mat();
            int largest_contour_index = 0;
            double largest_area = 0;
            CvInvoke.FindContours(edges, contours, hierarchy, contouringMode, contouringMethod);
            for (int i = 0; i < contours.Size; i++)
            {
                double a = CvInvoke.ContourArea(contours[i], false);
                if (a > largest_area)
                {
                    largest_area = a;
                    largest_contour_index = i;
                }
            }
            CvInvoke.DrawContours(finalImg, contours, largest_contour_index, redColor, 3, LineType.EightConnected, hierarchy);
            //defects points finding
            VectorOfInt hull = new VectorOfInt();
            Mat defects = new Mat();
            if (contours.Size > 0)
            {
                VectorOfPoint largestContour = new VectorOfPoint(contours[largest_contour_index].ToArray());
                CvInvoke.ConvexHull(largestContour, hull, false, true);
                CvInvoke.ConvexityDefects(largestContour, hull, defects);
                if (!defects.IsEmpty)
                {
                    Matrix<int> m = new Matrix<int>(defects.Rows, defects.Cols, defects.NumberOfChannels);
                    defects.CopyTo(m);
                    Matrix<int>[] channels = m.Split();
                    for (int i = 1; i < defects.Rows; ++i)
                    {
                        finalImg.Draw(new System.Drawing.Point[] { largestContour[channels[0][i, 0]], largestContour[channels[1][i, 0]] }, new Bgr(100, 255, 100), 2);
                        CvInvoke.Circle(finalImg, new System.Drawing.Point(largestContour[channels[0][i, 0]].X, largestContour[channels[0][i, 0]].Y), 7, new MCvScalar(255, 0, 0), -1);
                    }
                }
            }
            MainVideoBox.Dispatcher.BeginInvoke(new Action(() => MainVideoBox.Source = ToBitmapFinal(finalImg)));
        }

        public static BitmapSource ToBitmapFinal(Image<Bgr, byte> image)
        {
            using (Bitmap source = image.ToBitmap())
            {
                IntPtr ptr = source.GetHbitmap(); //obtain the Hbitmap

                BitmapSource bs = Imaging.CreateBitmapSourceFromHBitmap(
                    ptr,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                DeleteObject(ptr); //release the HBitmap
                return bs;
            }
        }

        public static BitmapSource ToBitmapGrey(Image<Gray, byte> image)
        {
            using (Bitmap source = image.ToBitmap())
            {
                IntPtr ptr = source.GetHbitmap(); //obtain the Hbitmap

                BitmapSource bs = Imaging.CreateBitmapSourceFromHBitmap(
                    ptr,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                DeleteObject(ptr); //release the HBitmap
                return bs;
            }
        }

        private void TwoStep_Checked(object sender, RoutedEventArgs e)
        {
            SecondMorphType.IsEnabled = true;
            SecondKernelType.IsEnabled = true;
            SecondKernelSize.IsEnabled = true;
            SecondMorphSteps.IsEnabled = true;
            doubleMorph = true;
        }

        private void TwoStep_Unchecked(object sender, RoutedEventArgs e)
        {
            SecondMorphType.IsEnabled = false;
            SecondKernelType.IsEnabled = false;
            SecondKernelSize.IsEnabled = false;
            SecondMorphSteps.IsEnabled = false;
            doubleMorph = false;
        }

        private void FirstKernelType_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            int type = (int)FirstKernelType.SelectedValue;
            switch (type)
            {
                case 1:
                    kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, FirstKernelSize == null ? new System.Drawing.Size(3, 3) : (System.Drawing.Size)FirstKernelSize.SelectedValue, new System.Drawing.Point(-1, -1));
                    break;
                case 2:
                    kernel = CvInvoke.GetStructuringElement(ElementShape.Cross, FirstKernelSize == null ? new System.Drawing.Size(3, 3) : (System.Drawing.Size)FirstKernelSize.SelectedValue, new System.Drawing.Point(-1, -1));
                    break;
                case 3:
                    kernel = CvInvoke.GetStructuringElement(ElementShape.Ellipse, FirstKernelSize == null ? new System.Drawing.Size(3, 3) : (System.Drawing.Size)FirstKernelSize.SelectedValue, new System.Drawing.Point(-1, -1));
                    break;
            }
        }

        private void SecondKernelType_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            int type = (int)SecondKernelType.SelectedValue;
            switch (type)
            {
                case 1:
                    kernel2 = CvInvoke.GetStructuringElement(ElementShape.Rectangle, SecondKernelSize == null ? new System.Drawing.Size(3, 3) : (System.Drawing.Size)SecondKernelSize.SelectedValue, new System.Drawing.Point(-1, -1));
                    break;
                case 2:
                    kernel2 = CvInvoke.GetStructuringElement(ElementShape.Cross, SecondKernelSize == null ? new System.Drawing.Size(3, 3) : (System.Drawing.Size)SecondKernelSize.SelectedValue, new System.Drawing.Point(-1, -1));
                    break;
                case 3:
                    kernel2 = CvInvoke.GetStructuringElement(ElementShape.Ellipse, SecondKernelSize == null ? new System.Drawing.Size(3, 3) : (System.Drawing.Size)SecondKernelSize.SelectedValue, new System.Drawing.Point(-1, -1));
                    break;
            }
        }

        private void FirstKernelSize_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            int type = (int)FirstKernelType.SelectedValue;
            switch (type)
            {
                case 1:
                    kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, (System.Drawing.Size)FirstKernelSize.SelectedValue, new System.Drawing.Point(-1, -1));
                    break;
                case 2:
                    kernel = CvInvoke.GetStructuringElement(ElementShape.Cross, (System.Drawing.Size)FirstKernelSize.SelectedValue, new System.Drawing.Point(-1, -1));
                    break;
                case 3:
                    kernel = CvInvoke.GetStructuringElement(ElementShape.Ellipse, (System.Drawing.Size)FirstKernelSize.SelectedValue, new System.Drawing.Point(-1, -1));
                    break;
            }
        }

        private void SecondKernelSize_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            int type = (int)SecondKernelType.SelectedValue;
            switch (type)
            {
                case 1:
                    kernel2 = CvInvoke.GetStructuringElement(ElementShape.Rectangle, (System.Drawing.Size)SecondKernelSize.SelectedValue, new System.Drawing.Point(-1, -1));
                    break;
                case 2:
                    kernel2 = CvInvoke.GetStructuringElement(ElementShape.Cross, (System.Drawing.Size)SecondKernelSize.SelectedValue, new System.Drawing.Point(-1, -1));
                    break;
                case 3:
                    kernel2 = CvInvoke.GetStructuringElement(ElementShape.Ellipse, (System.Drawing.Size)SecondKernelSize.SelectedValue, new System.Drawing.Point(-1, -1));
                    break;
            }
        }

        private void FirstMorphType_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            firstMorphOp = (MorphOp)FirstMorphType.SelectedValue;
        }

        private void SecondMorphType_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            secondMorphOp = (MorphOp)SecondMorphType.SelectedValue;
        }

        private void FirstMorphSteps_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            firstMorphSteps = (int)FirstMorphSteps.SelectedValue;
        }

        private void SecondMorphSteps_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            secondMorphSteps = (int)SecondMorphSteps.SelectedValue;
        }

        private void LowerTresholdLevel_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            lowerTresholdLevel = LowerTresholdLevel.Value;
        }

        private void UpperTresholdLevel_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            upperTresholdLevel = UpperTresholdLevel.Value;
        }

        private void CannyKernelSize_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            cannyKernelSize = (int)CannyKernelSize.SelectedValue;
        }

        private void ContouringMode_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            contouringMode = (RetrType)ContouringMode.SelectedValue;
        }

        private void ContouringMethod_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            int type = (int)ContouringMethod.SelectedValue;
            switch (type)
            {
                case 1:
                    contouringMethod = ChainApproxMethod.ChainApproxSimple;
                    break;
                case 2:
                    contouringMethod = ChainApproxMethod.ChainApproxTc89Kcos;
                    break;
                case 3:
                    contouringMethod = ChainApproxMethod.ChainApproxTc89L1;
                    break;
            }
        }
    }

    public static class CollectionData
    {
        public static Dictionary<MorphOp, string> GetMorphTypes()
        {
            Dictionary<MorphOp, string> morphTypes = new Dictionary<MorphOp, string>();
            morphTypes.Add(MorphOp.Erode, "Erode");
            morphTypes.Add(MorphOp.Dilate, "Dilate");
            morphTypes.Add(MorphOp.Open, "Open");
            morphTypes.Add(MorphOp.Close, "Close");
            morphTypes.Add(MorphOp.Gradient, "Gradient");
            morphTypes.Add(MorphOp.Tophat, "TopHat");
            morphTypes.Add(MorphOp.Blackhat, "BlackHat");
            return morphTypes;
        }

        public static Dictionary<int, string> GetKernelTypes()
        {
            Dictionary<int, string> kernelTypes = new Dictionary<int, string>();
            kernelTypes.Add(1, "Rectangle");
            kernelTypes.Add(2, "Cross");
            kernelTypes.Add(3, "Ellipse");            
            return kernelTypes;
        }

        public static Dictionary<System.Drawing.Size, string> GetKernelSizes()
        {
            Dictionary<System.Drawing.Size, string> kernelSizes = new Dictionary<System.Drawing.Size, string>();
            kernelSizes.Add(new System.Drawing.Size(3, 3), "3x3");
            kernelSizes.Add(new System.Drawing.Size(4, 4), "4x4");
            kernelSizes.Add(new System.Drawing.Size(5, 5), "5x5");
            kernelSizes.Add(new System.Drawing.Size(6, 6), "6x6");
            kernelSizes.Add(new System.Drawing.Size(7, 7), "7x7");
            kernelSizes.Add(new System.Drawing.Size(8, 8), "8x8");
            kernelSizes.Add(new System.Drawing.Size(9, 9), "9x9");
            kernelSizes.Add(new System.Drawing.Size(10, 10), "10x10");
            kernelSizes.Add(new System.Drawing.Size(11, 11), "11x11");
            return kernelSizes;
        }

        public static Dictionary<int, string> GetMorphSteps()
        {
            Dictionary<int, string> morphSteps = new Dictionary<int, string>();
            morphSteps.Add(1, "1");
            morphSteps.Add(2, "2");
            morphSteps.Add(3, "3");
            morphSteps.Add(4, "4");
            morphSteps.Add(5, "5");
            morphSteps.Add(6, "6");
            morphSteps.Add(7, "7");
            morphSteps.Add(8, "8");
            morphSteps.Add(9, "9");
            morphSteps.Add(10, "10");
            return morphSteps; 
        }

        public static Dictionary<int, string> GetCannyKernelSizes()
        {
            Dictionary<int, string> cannyKernelSizes = new Dictionary<int, string>();
            cannyKernelSizes.Add(3, "3x3");
            cannyKernelSizes.Add(5, "5x5");
            cannyKernelSizes.Add(7, "7x7");
            return cannyKernelSizes;
        }

        public static Dictionary<RetrType, string> GetContouringModes()
        {
            Dictionary<RetrType, string> contouringModes = new Dictionary<RetrType, string>();
            contouringModes.Add(RetrType.External, "External");
            contouringModes.Add(RetrType.List, "List");
            contouringModes.Add(RetrType.Tree, "Tree");
            contouringModes.Add(RetrType.Ccomp, "Ccomp");
            return contouringModes;
        }

        public static Dictionary<int, string> GetContouringMethods()
        {
            Dictionary<int, string> contouringMethods = new Dictionary<int, string>();
            contouringMethods.Add(1, "ChainApproxSimple");
            contouringMethods.Add(2, "ChainApproxTc89Kcos");
            contouringMethods.Add(3, "ChainApproxTc89L1");
            return contouringMethods;
        }
    }
}
