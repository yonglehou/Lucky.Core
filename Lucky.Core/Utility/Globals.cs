﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Lucky.Core.Infrastructure;
using Lucky.Core.Logging;

namespace Lucky.Core.Utility
{
    public sealed class Globals
    {
        public static ActionResult Execute(Func<ActionResult> action)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                EngineContext.Current.Resolve<ILogger>().Error("{0}\r\n{1}", ex.Message, ex.InnerException?.Message);
            }
            return null;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="action"></param>
        public static void Execute(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                EngineContext.Current.Resolve<ILogger>().Error("{0}\r\n{1}", ex.Message, ex.InnerException?.Message);
            }
        }
        public static T InvokeHandler<T>(Delegate delegateWarpper, object[] parameters)
        {
            try
            {
                //这边关于第一个参数要注意一下
                //当委托绑定的方法是静态的，那么就可以传“null”
                //如果绑定的方法不是静态的，就必须传当前方法的实例才行，也就是 -- delegateWarpper.Target
                //如果还不懂，直接MSDN查找，我也是看MSDN的
                return (T)delegateWarpper.Method.Invoke(delegateWarpper.Target, parameters);
            }
            catch (TargetInvocationException exception)
            {
                Console.WriteLine(exception.InnerException.Message + Environment.NewLine + exception.InnerException.StackTrace);
            }
            return default(T);
        }
        public static string SetEncoding(HttpContext context)
        {
            bool gzip, deflate;
            var encoding = "none";
            if (!string.IsNullOrEmpty(context.Request.ServerVariables["HTTP_ACCEPT_ENCODING"]))
            {
                var acceptedTypes = context.Request.ServerVariables["HTTP_ACCEPT_ENCODING"].ToLower();
                gzip = acceptedTypes.Contains("gzip") || acceptedTypes.Contains("x-gzip") || acceptedTypes.Contains("*");
                deflate = acceptedTypes.Contains("deflate");
            }
            else
                gzip = deflate = false;

            encoding = gzip ? "gzip" : (deflate ? "deflate" : "none");

            if (context.Request.Browser.Browser != "IE") return encoding;
            if (context.Request.Browser.MajorVersion < 6)
                encoding = "none";
            else if (context.Request.Browser.MajorVersion == 6 &&
                     !string.IsNullOrEmpty(context.Request.ServerVariables["HTTP_USER_AGENT"]) &&
                     context.Request.ServerVariables["HTTP_USER_AGENT"].Contains("EV1"))
                encoding = "none";
            return encoding;
        }
        /// <summary>
        /// 根据字节码判断文件真实类型,编码不一样 txt 结果可能不一样
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static FileExtension GetFileType(string filePath)
        {
            FileExtension extension;
            FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            BinaryReader reader = new BinaryReader(stream);
            string fileclass = "";
            try
            {

                for (int i = 0; i < 2; i++)
                {
                    fileclass += reader.ReadByte().ToString();
                }
                extension = (FileExtension) Enum.Parse(typeof (FileExtension), fileclass);
            }
            catch (Exception)
            {
                extension=FileExtension.Null;
                // ignored
            }
            return extension;
            /*文件扩展名说明
             * 
             * 4946/104116 txt
             * 7173        gif 
             * 255216      jpg
             * 13780       png
             * 6677        bmp
             * 239187      txt,aspx,asp,sql
             * 208207      xls.doc.ppt
             * 6063        xml
             * 6033        htm,html
             * 4742        js
             * 8075        xlsx,zip,pptx,mmap,zip
             * 8297        rar   
             * 01          accdb,mdb
             * 7790        exe,dll
             * 5666        psd 
             * 255254      rdp 
             * 10056       bt种子 
             * 64101       bat 
             * 4059        sgf    
             */

        }

        public static string GetContentType(string extension)
        {
            var contentType = string.Empty;
            switch (extension)
            {
                case "jpg":
                    contentType = "image/jpeg";
                    break;
                case "png":
                    contentType = "image/png";
                    break;
                case "gif":
                    contentType = "image/gif";
                    break;
                case "swf":
                    contentType = "application/x-shockwave-flash";
                    break;
                case "css":
                    contentType = "text/css";
                    break;
                case "js":
                    contentType = "application/x-javascript";
                    break;
            }
            return contentType;
        }

        public static bool IsCachedOnBrowser(HttpContext context, string hash, string contentType)
        {
            if (string.IsNullOrEmpty(context.Request.ServerVariables["HTTP_IF_NONE_MATCH"]) ||
                !context.Request.ServerVariables["HTTP_IF_NONE_MATCH"].Equals(hash)) return false;
            context.Response.ClearHeaders();
            context.Response.Status = "304 Not Modified";
            context.Response.AppendHeader("Content-Length", "0");
            return true;
        }

        public static string GetLocalFile(Uri uri, HttpContext context, List<string> fileNames)
        {
            var html = "";
            try
            {
                var path2 = context.Server.MapPath(uri.AbsolutePath);
                html = File.ReadAllText(path2);
                fileNames.Add(path2);
            }
            catch
            {
                html = GetRemoteFile(uri);
            }
            return html;
        }

        public static string GetRemoteFile(Uri uri)
        {
            var html = new StringBuilder();
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(uri);
                request.Credentials = CredentialCache.DefaultNetworkCredentials;
                using (var resp = request.GetResponse() as HttpWebResponse)
                {
                    if (resp != null)
                        using (var recDataStream = resp.GetResponseStream())
                        {
                            var buffer = new byte[1024];
                            var read = 0;
                            do
                            {
                                if (recDataStream != null) read = recDataStream.Read(buffer, 0, buffer.Length);
                                html.Append(Encoding.UTF8.GetString(buffer, 0, read));
                            } while (read > 0);
                        }
                }
            }
            catch
            {
                html.Append("");
            }
            return html.ToString();
        }


        public static string GetMd5Sum(string str)
        {
            var enc = Encoding.Unicode.GetEncoder();
            var unicodeText = new byte[str.Length * 2];
            enc.GetBytes(str.ToCharArray(), 0, str.Length, unicodeText, 0, true);
            var md5 = new MD5CryptoServiceProvider();
            var result = md5.ComputeHash(unicodeText);
            var sb = new StringBuilder();
            foreach (var t in result)
            {
                sb.Append(t.ToString("X2"));
            }
            return sb.ToString();
        }
        public static byte[] Serialize(object o)
        {
            if (o == null)
            {
                return null;
            }

            BinaryFormatter binaryFormatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                binaryFormatter.Serialize(memoryStream, o);
                byte[] objectDataAsStream = memoryStream.ToArray();
                return objectDataAsStream;
            }
        }

        public static T Deserialize<T>(byte[] stream)
        {
            if (stream == null)
            {
                return default(T);
            }

            BinaryFormatter binaryFormatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream(stream))
            {
                T result = (T)binaryFormatter.Deserialize(memoryStream);
                return result;
            }
        }
        public static T Clone<T>(T RealObject)
        {
            using (Stream objectStream = new MemoryStream())
            {
                //利用 System.Runtime.Serialization序列化与反序列化完成引用对象的复制
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(objectStream, RealObject);
                objectStream.Seek(0, SeekOrigin.Begin);
                return (T)formatter.Deserialize(objectStream);
            }
        } 
    }
}
