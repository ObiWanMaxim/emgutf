﻿//----------------------------------------------------------------------------
//  Copyright (C) 2004-2020 by EMGU Corporation. All rights reserved.       
//----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;
using System.Threading.Tasks;
using System.Diagnostics;
using Emgu.TF;
using Emgu.TF.Models;
using Emgu.Models;
using Tensorflow;

#if __ANDROID__
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Graphics;
using Android.Preferences;
#elif __UNIFIED__ && !__IOS__
using AppKit;
using CoreGraphics;
#elif __IOS__
using UIKit;
using CoreGraphics;
#endif

namespace Emgu.TF.XamarinForms
{
    public class MultiboxDetectionPage : ModelButtonTextImagePage
    {
        private MultiboxGraph _multiboxGraph;

        public override String GetButtonName(ButtonMode mode)
        {
            switch (mode)
            {
                case ButtonMode.WaitingModelDownload:
                    return "Download Model";
                default:
                    return "Detect People";
            }
        }

        public MultiboxDetectionPage()
           : base()
        {
            Title = "Multibox People Detection";

            if (_multiboxGraph == null)
            {
                SessionOptions so = new SessionOptions();
                if (TfInvoke.IsGoogleCudaEnabled)
                {
                    Tensorflow.ConfigProto config = new Tensorflow.ConfigProto();
                    config.GpuOptions = new Tensorflow.GPUOptions();
                    config.GpuOptions.AllowGrowth = true;
                    so.SetConfig(config.ToProtobuf());
                }
                _multiboxGraph = new MultiboxGraph(null, so);
                _multiboxGraph.OnDownloadProgressChanged += onDownloadProgressChanged;
                _multiboxGraph.OnDownloadCompleted += onDownloadCompleted;
                _multiboxGraph.OnDownloadCompleted += (sender, e) =>
                {
                    OnButtonClicked(sender, e);
                };
            }

            OnImagesLoaded += (sender, image) =>
            {
                try
                {
                    SetMessage("Please wait...");
                    SetImage();
                    Stopwatch watch = Stopwatch.StartNew();

                    Tensor imageTensor = Emgu.TF.Models.ImageIO.ReadTensorFromImageFile<float>(image[0], 224, 224, 128.0f, 1.0f / 128.0f);
                    MultiboxGraph.Result[] detectResult = _multiboxGraph.Detect(imageTensor);
                    watch.Stop();
                    Emgu.Models.Annotation[] annotations = MultiboxGraph.FilterResults(detectResult, 0.1f);

                    var jpeg = Emgu.Models.NativeImageIO.ImageFileToJpeg(image[0], annotations);

                    watch.Stop();
                    SetImage(jpeg.Raw, jpeg.Width, jpeg.Height);
#if __MACOS__
                    var displayImage = this.DisplayImage;
                    displayImage.WidthRequest = jpeg.Width;
                    displayImage.HeightRequest = jpeg.Height;
#endif

                    SetMessage(String.Format("Detected in {0} milliseconds.", watch.ElapsedMilliseconds));
                }
                catch (Exception excpt)
                {
                    String msg = excpt.Message.Replace(System.Environment.NewLine, " ");
                    SetMessage(msg);
                }
            };
        }

        public override void OnButtonClicked(Object sender, EventArgs args)
        {
            base.OnButtonClicked(sender, args);

            if (_buttonMode == ButtonMode.WaitingModelDownload)
            {
                _multiboxGraph.Init();
            }
            else
            {
                LoadImages(new string[] { "surfers.jpg" });
            }
        }

    }
}
