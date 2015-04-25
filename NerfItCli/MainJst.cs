using System;
using System.Drawing;
using System.Drawing.Imaging;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;

namespace NerfItCli
{
  class MainJst
  {
    private static byte[] HEADER;
    private static string BOUNDARY = "boundarydonotcross";
    private static byte[] BOUNDARY_PLUS;
    private static string STD_HEADER;
    private static Capture _ImageCapture;
    private static HaarCascade _HaarCascade;
    private static System.Net.Sockets.TcpListener _TcpListener;
    private static ImageCodecInfo _JpegCodecInfo;

    public static void Main (string[] args)
    {
      BOUNDARY_PLUS = System.Text.UTF8Encoding.UTF8.GetBytes ("\r\n--" + BOUNDARY + "\r\n");
      STD_HEADER = "Connection: close\r\n" + "Server: NerfCli Mjpeg Streamer/0.2\r\nCache-Control: no-store, no-cache, must-revalidate, pre-check=0, post-check=0, max-age=0\r\nPragma: no-cache\r\nExpires: Mon, 3 Jan 2000 12:34:56 GMT\r\n";
      HEADER = System.Text.UTF8Encoding.UTF8.GetBytes ("HTTP/1.0 200 OK\r\n" + STD_HEADER + "content-type: multipart/x-mixed-replace;boundary=" + BOUNDARY + "\r\n\r\n--" + BOUNDARY + "\r\n");
      
      EncoderParameter qualityParam = new EncoderParameter (Encoder.Quality, 95L);
      
      // Jpeg image codec
      _JpegCodecInfo = GetEncoderInfo ("image/jpeg");
      
      EncoderParameters encoderParams = new EncoderParameters (1);
      encoderParams.Param [0] = qualityParam;
      
      _TcpListener = new System.Net.Sockets.TcpListener (System.Net.IPAddress.Any, 6123);
      _TcpListener.Start ();
      using (_HaarCascade = new HaarCascade ("/usr/share/opencv/haarcascades/haarcascade_frontalface_alt2.xml")) {
        using (_ImageCapture = new Capture ()) {
          while (true) {
            using (var Socket = _TcpListener.AcceptSocket ()) {
              using (var NetStream = new System.Net.Sockets.NetworkStream (Socket, true)) {
                NetStream.Write (HEADER, 0, HEADER.Length);
                while (Socket.Connected) {
                  using (Image<Bgr, byte> NextFrame = _ImageCapture.QueryFrame ()) {
                    if (NextFrame != null) {
                      // Run face detection and draw a box around each face.
                      using (Image<Gray, byte> grayframe = NextFrame.Convert<Gray, byte> ()) {
                        var faces = _HaarCascade.Detect (grayframe);

                        foreach (var face in faces)
                          NextFrame.Draw (face.rect, new Bgr (0, double.MaxValue, 0), 3);
                      }

                      using (Bitmap NextBitmapFrame = NextFrame.ToBitmap ()) {
                        try {
                          //"Content-Length: {0:N}\r\n" +
                          var FileHeader = System.Text.UTF8Encoding.UTF8.GetBytes (string.Format ("Content-Type: image/jpeg\r\nX-Timestamp: {1:N}.{2:N6}\r\n\r\n", 0, 0, 0));
                          
                          NetStream.Write (FileHeader, 0, FileHeader.Length);
                          NextBitmapFrame.Save (NetStream, _JpegCodecInfo, encoderParams);
                          NetStream.Write (BOUNDARY_PLUS, 0, BOUNDARY_PLUS.Length);
                        } catch (Exception) {
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        }
      }
    }

    private static ImageCodecInfo GetEncoderInfo (string mimeType)
    {
      // Get image codecs for all image formats
      ImageCodecInfo[] Codecs = ImageCodecInfo.GetImageEncoders ();

      // Find the correct image codec
      for (int i = 0; i < Codecs.Length; i++)
        if (Codecs [i].MimeType == mimeType)
          return Codecs [i];
      return null;
    }
  }
}
