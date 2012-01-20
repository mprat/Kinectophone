using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Research.Kinect.Nui;
using Microsoft.Research.Kinect.Audio;
using Coding4Fun.Kinect.Wpf;


namespace Kinectophone
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Runtime nui = Runtime.Kinects[0];

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, EventArgs e)
        {
            nui.Initialize(RuntimeOptions.UseSkeletalTracking | RuntimeOptions.UseColor);

            nui.VideoFrameReady += new EventHandler<ImageFrameReadyEventArgs>(nui_VideoFrameReady);
            nui.VideoStream.Open(ImageStreamType.Video, 2, ImageResolution.Resolution640x480, ImageType.Color);

            nui.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(nui_SkeletonFrameReady);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            nui.Uninitialize();
        }

        void nui_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            SkeletonData skeleton = (from s in e.SkeletonFrame.Skeletons
                                     where s.TrackingState == SkeletonTrackingState.Tracked
                                     select s).FirstOrDefault();

            if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
            {
                Joint head = skeleton.Joints[JointID.Head].ScaleTo((int)canvas1.Height, (int)canvas1.Width, .5f, .5f);
                Canvas.SetLeft(headEllipse, head.Position.X);
                Canvas.SetTop(headEllipse, head.Position.Y);
            }
        }

        void nui_VideoFrameReady(object sender, ImageFrameReadyEventArgs e)
        {
            kinectColorOut.Source = e.ImageFrame.ToBitmapSource();
        }
    }
}
