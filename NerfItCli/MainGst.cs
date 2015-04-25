using System;
using System.Drawing;
using System.Drawing.Imaging;
using Gst;
using GLib;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;


namespace NerfItCli
{
  class MainGst
  {
    static MainLoop _Loop;
    static Gst.App.AppSrc _AppSrc;
    static Pipeline _Pipeline;
    static HaarCascade _HaarCascade;
    static Capture _ImageCapture;

    public static void Main (string[] args)
    {
      Console.WriteLine ("1");
      using (_HaarCascade = new HaarCascade ("/usr/share/opencv/haarcascades/haarcascade_frontalface_alt2.xml")) {
        Console.WriteLine ("2");
        using (_ImageCapture = new Capture()) {
          Application.Init ();
          _Loop = new MainLoop ();

          // Construct all elements
          _Pipeline = new Pipeline ();
          _AppSrc = new Gst.App.AppSrc ("OpenCv");
          Element X264Encoder = ElementFactory.Make ("x264enc");
          Element QTMuxer = ElementFactory.Make ("qtmux");
          Gst.CorePlugins.FileSink Sink = (Gst.CorePlugins.FileSink)ElementFactory.Make ("filesink");
          Sink.Location = "testVideo.mov";

          // Link elements
          _Pipeline.Add (_AppSrc, X264Encoder, QTMuxer, Sink);
          Element.Link (_AppSrc, X264Encoder, QTMuxer, Sink);

          // Set the caps on AppSrc to RGBA 640x480 4fps square pixels
          _AppSrc.Caps = Gst.Video.VideoUtil.FormatNewCaps (Gst.Video.VideoFormat.BGR, 640, 480, 4, 1, 1, 1);

          // Connect handlers
          _AppSrc.NeedData += Handle_AppSrcNeedData;
          _Pipeline.Bus.AddSignalWatch ();
          _Pipeline.Bus.Message += Handle_PipelineBusMessage;

          Console.WriteLine ("3");
          // Run, loop, run
          _Pipeline.SetState (State.Playing);
          _Loop.Run ();
          _Pipeline.SetState (State.Null);
        }
      }
    }

    static void Handle_AppSrcNeedData (object o, Gst.App.NeedDataArgs args)
    {
      Gst.Buffer Data;
      using (Image<Emgu.CV.Structure.Bgr, byte> NextFrame = _ImageCapture.QueryFrame()) {
        if (NextFrame == null)
          Data = new Gst.Buffer ();
        else {
          // Run face detection and draw a box around each face.
          using (Image<Emgu.CV.Structure.Gray, byte> grayframe = NextFrame.Convert<Gray, byte> ()) {
            var faces = _HaarCascade.Detect (grayframe);

            foreach (var face in faces)
              NextFrame.Draw (face.rect, new Bgr (0, double.MaxValue, 0), 3);
          }
          Data = new Gst.Buffer(NextFrame.Bytes);
        }
      }
      _AppSrc.PushBuffer (Data);
    }

    static void Handle_PipelineBusMessage (object o, MessageArgs args)
    {
      string Text = String.Format ("Message from {0}:  \t{1}", args.Message.Src.Name, args.Message.Type);
      switch (args.Message.Type) {
      case MessageType.Error:
        Enum err;
        string msg;
        args.Message.ParseError (out err, out msg);
        Text += String.Format ("\t({0})", msg);
        break;
      case MessageType.StateChanged:
        State oldstate, newstate, pending;
        args.Message.ParseStateChanged (out oldstate, out newstate, out pending);
        Text += string.Format ("\t\t{0} -> {1}   ({2})", oldstate, newstate, pending);
        break;
      case MessageType.Eos:
        _Loop.Quit ();
        break;
      default:
        Text += "Unknown Message: " + args.Message;
        break;
      }
      Console.WriteLine (Text);
    }

  }
}

