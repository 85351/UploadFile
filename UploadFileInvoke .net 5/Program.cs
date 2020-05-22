using System;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UploadFile
{
    class Program
    {
        public static string Token = "";
        public static string WebSite = "http://localhost:5000";
        public static string WebPath = "/api/file/Upload";
        static Queue queue;
        static StringBuilder sb = new StringBuilder();
        static int ThreadCount = 10;
        static int ThreadRunCount = 200;
        static string logName = ".net";
        static void Main(string[] args)
        {
            Console.WriteLine("请选择调用目标1:.net，其他:go");
            var read = Console.ReadLine();
            if(read!="1")
            {
                WebSite = "http://localhost:8888";
                WebPath = "/upload";
                logName = "go";
            }
            queue = new Queue(ThreadCount);
            
            var path = Environment.CurrentDirectory + @"\Img\1.jpg";
            sb = new StringBuilder();

            for (var n = 0; n < ThreadCount; n++)
            {
                Thread t = new Thread(new ParameterizedThreadStart(Bw_DoWork));
                t.Start(path);
            }
            //Bw_DoWork(path);
        }

        private static void Bw_DoWork(object sender)
        {
            var path = sender.ToString();
            for (int n = 0; n < ThreadRunCount; n++)
            {
                var relativePath = $"/aa/{DateTime.Now:HHMMssffffff}{Guid.NewGuid()}.jpg";
                long count;
                Log($"threadId:{Thread.CurrentThread.ManagedThreadId} start {n + 1}: {relativePath}");
                UploadFromCache(n + 1, path, ref relativePath, out count);
                Log($"threadId:{Thread.CurrentThread.ManagedThreadId} end {n + 1}: {relativePath}");
                Log("");
            }
            queue.Enqueue(1);
            if (queue.Count >= ThreadCount)
            {
                File.AppendAllText($"log_{logName}_{ThreadCount}x{ThreadRunCount}.txt", sb.ToString());
            }
        }


        public static void Log(string msg)
        {
            var tip = "";
            if (!string.IsNullOrWhiteSpace(msg))
                tip = $"{DateTime.Now:HH:mm:ss.fffff} {msg}";
            Console.WriteLine(tip);
            sb.AppendLine(tip);
            //File.AppendAllText("log.txt", tip + Environment.NewLine);
        }

        public static string UploadFromCache(int threadRunIndex, string cachePath, ref string relativePath, out long blockCount)
        {
            //string cpath = Combine("", cachePath);
            // 读文件流
            using (FileStream fs = new FileStream(cachePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                // 这部分需要完善
                //string ContentType = "application/octet-stream";

                byte[] fileBytes = new byte[fs.Length];
                fs.Read(fileBytes, 0, Convert.ToInt32(fs.Length));

                blockCount = fs.Length;

                return Upload(threadRunIndex, fileBytes, ref relativePath);
            }
        }
        public static string Upload(int threadRunIndex, byte[] imgs, ref string relativePath)
        {
            // 这部分需要完善
            string ContentType = "application/octet-stream";
            byte[] fileBytes = imgs;

            // 生成需要上传的二进制数组
            CreateBytes cb = new CreateBytes();
            // 所有表单数据
            ArrayList bytesArray = new ArrayList
            {
                cb.CreateFieldData("FileName", relativePath),
                cb.CreateFieldData("Token", Token),
                cb.CreateFieldData("FileData", relativePath, ContentType, fileBytes)
            };

            byte[] bytes = cb.JoinBytes(bytesArray);

            bool uploaded = cb.UploadData(threadRunIndex, relativePath, $"{WebSite}{WebPath}", bytes, out byte[] responseBytes, Log);
            if (uploaded)
            {
                string filename = Encoding.UTF8.GetString(responseBytes);
                return Combine(WebSite, filename);
            }

            return string.Empty;
        }
        private static string Combine(params string[] paths)
        {
            if (paths == null || paths.Count() == 0)
            {
                return string.Empty;
            }

            string path = paths[0].Replace("\\", "/");

            for (int n = 1; n < paths.Count(); n++)
            {
                paths[n] = paths[n].Replace("\\", "/");

                if (path.EndsWith("/"))
                {
                    if (paths[n].StartsWith("/"))
                    {
                        path += paths[n].Substring(1);
                    }
                    else
                    {
                        path += paths[n];
                    }
                }
                else
                {
                    if (paths[n].StartsWith("/"))
                    {
                        path += paths[n].Substring(1);
                    }
                    else
                    {
                        path = path + "/" + paths[n];
                    }
                }
            }
            return path;
        }

    }
    public class CreateBytes
    {
        private readonly Encoding encoding = Encoding.UTF8;

        /// <summary>
        /// 拼接所有的二进制数组为一个数组
        /// </summary>
        /// <param name="byteArrays">数组</param>
        /// <returns></returns>
        /// <remarks>加上结束边界</remarks>
        public byte[] JoinBytes(ArrayList byteArrays)
        {
            int length = 0;
            int readLength = 0;

            // 加上结束边界
            string endBoundary = Boundary + "--\r\n"; //结束边界
            byte[] endBoundaryBytes = encoding.GetBytes(endBoundary);
            byteArrays.Add(endBoundaryBytes);

            foreach (byte[] b in byteArrays)
            {
                length += b.Length;
            }
            byte[] bytes = new byte[length];

            // 遍历复制
            foreach (byte[] b in byteArrays)
            {
                b.CopyTo(bytes, readLength);
                readLength += b.Length;
            }

            return bytes;
        }

        public bool UploadData(int threadRunIndex, string relativePath, string uploadUrl, byte[] bytes, out byte[] responseBytes, Action<string> Log)
        {
            using (WebClient webClient = new WebClient())
            {
                webClient.Headers.Add("Content-Type", ContentType);
                Log($"threadId:{Thread.CurrentThread.ManagedThreadId} begin_Send:{threadRunIndex} : {relativePath}");
                responseBytes = webClient.UploadData(uploadUrl, bytes);
            }

            return true;
        }

        /// <summary>
        /// 获取普通表单区域二进制数组
        /// </summary>
        /// <param name="fieldName">表单名</param>
        /// <param name="fieldValue">表单值</param>
        /// <returns></returns>
        /// <remarks>
        /// -----------------------------7d52ee27210a3c\r\nContent-Disposition: form-data; name=\"表单名\"\r\n\r\n表单值\r\n
        /// </remarks>
        public byte[] CreateFieldData(string fieldName, string fieldValue)
        {
            string textTemplate = Boundary + "\r\nContent-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}\r\n";
            string text = string.Format(textTemplate, fieldName, fieldValue);
            byte[] bytes = encoding.GetBytes(text);
            return bytes;
        }

        /// <summary>
        /// 获取文件上传表单区域二进制数组
        /// </summary>
        /// <param name="fieldName">表单名</param>
        /// <param name="filename">文件名</param>
        /// <param name="contentType">文件类型</param>
        /// <param name="contentLength">文件长度</param>
        /// <param name="stream">文件流</param>
        /// <returns>二进制数组</returns>
        public byte[] CreateFieldData(string fieldName, string filename, string contentType, byte[] fileBytes)
        {
            var end = "\r\n";
            var textTemplate = Boundary + "\r\nContent-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";

            //头数据
            var data = string.Format(textTemplate, fieldName, filename, contentType);
            var bytes = encoding.GetBytes(data);

            //尾数据
            var endBytes = encoding.GetBytes(end);

            //合成后的数组
            var fieldData = new byte[bytes.Length + fileBytes.Length + endBytes.Length];

            bytes.CopyTo(fieldData, 0); // 头数据
            fileBytes.CopyTo(fieldData, bytes.Length); // 文件的二进制数据
            endBytes.CopyTo(fieldData, bytes.Length + fileBytes.Length); // \r\n

            return fieldData;
        }

        #region 属性

        public string Boundary
        {
            get
            {
                string[] bArray, ctArray;
                string contentType = ContentType;
                ctArray = contentType.Split(';');
                if (ctArray[0].Trim().ToLower() == "multipart/form-data")
                {
                    bArray = ctArray[1].Split('=');
                    return "--" + bArray[1];
                }
                return null;
            }
        }

        public string ContentType
        {
            get
            {
                return "multipart/form-data; boundary=---------------------------7d5b915500cee";
                //if (HttpContext.Current == null)
                //{
                //    return "multipart/form-data; boundary=---------------------------7d5b915500cee";
                //}
                //return HttpContext.Current.Request.ContentType;
            }
        }

        #endregion 属性
    }
}

