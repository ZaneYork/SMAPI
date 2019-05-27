using System;
using System.IO;
using Java.Util.Zip;

namespace ModLoader.Helper
{

    public class ZipHelper
    {

        /// <summary>  
        /// 功能：解压zip格式的文件。  
        /// </summary>  
        /// <param name="stream">压缩文件流</param>  
        /// <param name="unZipDir">解压文件存放路径,为空时默认与压缩文件同一级目录下，跟压缩文件同名的文件夹</param>  
        /// <returns>解压是否成功</returns>  
        public static void UnZip(Stream stream, string unZipDir)
        {
            using (var s = new ZipInputStream(stream))
            {

                ZipEntry theEntry;
                while ((theEntry = s.NextEntry) != null)
                {
                    string directoryName = Path.GetDirectoryName(theEntry.Name);
                    string fileName = Path.GetFileName(theEntry.Name);
                    if (!string.IsNullOrEmpty(directoryName))
                    {
                        Directory.CreateDirectory(unZipDir + directoryName);
                    }
                    if (directoryName != null && !directoryName.EndsWith("/"))
                    {
                    }
                    if (fileName != string.Empty)
                    {
                        FileStream streamWriter = null;
                        try
                        {

                            using (streamWriter = File.Create(unZipDir + theEntry.Name))
                            {

                                int size;
                                byte[] data = new byte[2048];
                                while (true)
                                {
                                    size = s.Read(data, 0, data.Length);
                                    if (size > 0)
                                    {
                                        streamWriter.Write(data, 0, size);
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                        catch (IOException) { }
                        finally
                        {
                            streamWriter?.Close();
                        }
                    }
                }
            }
        }
        public static void UnZip(string zipFilePath, string unZipDir)
        {
            if (zipFilePath == string.Empty)
            {
                throw new Exception("压缩文件不能为空！");
            }
            if (!File.Exists(zipFilePath))
            {
                throw new FileNotFoundException("压缩文件不存在！");
            }
            //解压文件夹为空时默认与压缩文件同一级目录下，跟压缩文件同名的文件夹  
            if (unZipDir == string.Empty)
                unZipDir = zipFilePath.Replace(Path.GetFileName(zipFilePath), Path.GetFileNameWithoutExtension(zipFilePath));
            if (!unZipDir.EndsWith("/"))
                unZipDir += "/";
            if (!Directory.Exists(unZipDir))
                Directory.CreateDirectory(unZipDir);
            UnZip(File.OpenRead(zipFilePath), unZipDir);
        }
    }
}
