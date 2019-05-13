using System.IO;
using System.Threading;
using Android.Content;
using Android.OS;
using Android.Widget;
using Java.IO;

namespace ModLoader.Common
{
    class Utils
    {
        public static byte[] FileToMemory(string filename)
        {
            byte[] bytes = new byte[2048];
            FileInputStream fs = new FileInputStream(filename);
            MemoryStream outStream = new MemoryStream();
            int len;
            while ((len = fs.Read(bytes, 0, bytes.Length)) > 0)
            {
                outStream.Write(bytes, 0, len);
            }

            fs.Close();
            return outStream.ToArray();
        }

        public static void StreamToFile(Stream stream, string fileName)
        {
            byte[] bytes = new byte[2048];
            FileStream fs = new FileStream(fileName, FileMode.Create);
            BinaryWriter bw = new BinaryWriter(fs);
            int len;
            while ((len = stream.Read(bytes, 0, bytes.Length)) > 0)
            {
                bw.Write(bytes, 0, len);
            }

            bw.Close();
            fs.Close();
        }

        public static void MakeToast(Context context, string message, ToastLength toastLength)
        {
            new Thread(() =>
            {

                Looper.Prepare();
                new Handler().Post(() =>
                {
                    Toast.MakeText(context, message, toastLength).Show();
                });
                Looper.Loop();
            }).Start();
        }
    }
}
