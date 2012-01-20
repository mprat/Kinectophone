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
                Joint head = getAndDrawJoint(skeleton, JointID.Head, headEllipse);
                Joint shoulderCenter = getAndDrawJoint(skeleton, JointID.ShoulderCenter, shoulderCenterEllipse);
                Joint shoulderLeft = getAndDrawJoint(skeleton, JointID.ShoulderLeft, shoulderLeftEllipse);
                Joint shoulderRight = getAndDrawJoint(skeleton, JointID.ShoulderRight, shoulderRightEllipse);
                Joint elbowLeft = getAndDrawJoint(skeleton, JointID.ElbowLeft, elbowLeftEllipse);
                Joint elbowRight = getAndDrawJoint(skeleton, JointID.ElbowRight, elbowRightEllipse);
                Joint wristLeft = getAndDrawJoint(skeleton, JointID.WristLeft, wristLeftEllipse);
                Joint wristRight = getAndDrawJoint(skeleton, JointID.WristRight, wristRightEllipse);
                Joint handLeft = getAndDrawJoint(skeleton, JointID.HandLeft, handLeftEllipse);
                Joint handRight = getAndDrawJoint(skeleton, JointID.HandRight, handRightEllipse);
                Joint spine = getAndDrawJoint(skeleton, JointID.Spine, spineEllipse);
            }
        }

        void nui_VideoFrameReady(object sender, ImageFrameReadyEventArgs e)
        {
            kinectColorOut.Source = e.ImageFrame.ToBitmapSource();
        }

        private Joint getAndDrawJoint(SkeletonData skel, JointID jointID, UIElement ellipse)
        {
            Joint jt = skel.Joints[jointID].ScaleTo((int)canvas1.Height, (int)canvas1.Width, .5f, .5f);

            Canvas.SetLeft(ellipse, jt.Position.X);
            Canvas.SetTop(ellipse, jt.Position.Y);
            return jt;
        }
    }
}
