using System;
using System.IO;
using System.Linq;
using System.Web.Mvc;

namespace Jj.ImgWeb.Controllers
{
    public class ImageController : Controller
    {
        private static readonly string Container = "imagecontainer";
        string token = System.Configuration.ConfigurationManager.AppSettings["Token"];
        public ContentResult Delete()
        {
            var fileName = Request.Form["FileName"];
            var apiToken = Request.Form["Token"];
            if (apiToken != token)
                return Content("");

            //var topDir = Combine(Server.MapPath("/"), Container);
            //if (topDir.EndsWith("/") || topDir.EndsWith("\\"))
            //{
            //    topDir = topDir.Substring(0, topDir.Length - 1);
            //}
            //topDir = topDir.ToLower();

            var fileNames = fileName.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var file in fileNames)
            {
                var path = Combine(Server.MapPath("/"), Container, file);
                //var f = new System.IO.FileInfo(path);
                //if (f.Exists)
                //{
                //    try
                //    {
                //        f.Delete();
                //    }
                //    catch (Exception ex)
                //    {
                //    }
                //    DeleteNonDir(f.Directory, topDir);
                //}
                if (System.IO.File.Exists(path))
                {
                    try
                    {
                        System.IO.File.Delete(path);
                    }
                    catch
                    {
                    }
                }
            }
            return Content("success");
        }

        private void DeleteNonDir(DirectoryInfo dir, string topDir)
        {
            try
            {
                if (dir == null)
                    return;
                if (!dir.Exists)
                    return;

                var fullName = dir.FullName;
                if (fullName.EndsWith("/") || fullName.EndsWith("\\"))
                {
                    fullName = fullName.Substring(0, fullName.Length - 1);
                }
                fullName = fullName.ToLower();
                if (topDir.Contains(fullName))
                    return;

                if (dir.GetFileSystemInfos().Any())
                    return;
                dir.Delete();
                DeleteNonDir(dir.Parent, topDir);
            }
            catch
            {
                // ignored
            }
        }

        public ContentResult Upload()
        {
            var fileName = Request.Form["FileName"];
            var apiToken = Request.Form["Token"];
            
            if (apiToken != token)
                return Content("Error: Token is not correct!");

            if (Request.Files.Count > 0)
            {
                var file = Request.Files[0];

                if (fileName.EndsWith(".exe"))
                    fileName = fileName + ".bak";

                var filePath = Combine(Server.MapPath("/"), Container, fileName);

                if (file != null)
                {
                    var length = file.ContentLength;
                    var bytes = new byte[length];
                    file.InputStream.Read(bytes, 0, length);
                    CheckDirectory(filePath);

                    System.IO.File.WriteAllBytes(filePath, bytes);
                }

                return Content(Combine(Container, fileName));
            }

            return Content("Error: No file upload.");
        }

        private static void CheckDirectory(string path)
        {
            var dir = path.Substring(0, path.LastIndexOf('/'));
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        private static string Combine(params string[] paths)
        {
            if (paths == null || !paths.Any())
                return string.Empty;

            var path = paths[0].Replace("\\", "/");

            for (var n = 1; n < paths.Count(); n++)
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
                        path += paths[n];
                    }
                    else
                    {
                        path += "/" + paths[n];
                    }
                }
            }
            return path;
        }
    }
}