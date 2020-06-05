using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebUpload.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private static readonly string Container = "container";
        string token = "";// System.Configuration.ConfigurationManager.AppSettings["Token"];

        [HttpPost]
        public ContentResult Delete()
        {
            var fileName = Request.Form["FileName"].FirstOrDefault() ?? "";
            var apiToken = Request.Form["Token"].FirstOrDefault() ?? "";
            if (apiToken != token)
                return Content("");

            var fileNames = fileName.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var file in fileNames)
            {
                var path = Combine(ServerDir(), Container, file);
                if (System.IO.File.Exists(path))
                {
                    try
                    {
                        System.IO.File.Delete(path);

                        DeleteEmptyDir(Directory.GetParent(path));
                    }
                    catch
                    {
                    }
                }
            }
            return Content("success");
        }
        private void DeleteEmptyDir(DirectoryInfo dir)
        {
            var containerPath = Combine(ServerDir(), Container).ToLower();
            if (containerPath.StartsWith(dir.FullName.ToLower()))
                return;

            if (dir.GetFiles().Count() > 0)
                return;
            if (dir.GetDirectories().Count() > 0)
                return;
            dir.Delete(false);
            DeleteEmptyDir(dir.Parent);
        }

        private string ServerDir()
        {
            var p = Environment.CurrentDirectory;
            return p;
        }

        [HttpPost]
        public async Task<object> Upload()
        {

           try
            {
                var fileName = Request.Form["FileName"].FirstOrDefault() ?? "";
                var apiToken = Request.Form["Token"].FirstOrDefault() ?? "";

                if (apiToken != token)
                    return new { success = false, msg = "Error: Token is not correct!" };

                var files = Request.Form.Files;
                if (files.Count <= 0)
                    return new { success = false, msg = "Error: No file upload." };

                var file = files[0];

                if (fileName.EndsWith(".exe"))
                    fileName = fileName + ".bak";

                var filePath = Combine(ServerDir(), Container, fileName);

                if (file != null)
                {
                    var length = file.Length;
                    var bytes = new byte[length];
                    using (var stream = file.OpenReadStream())
                    {
                        await stream.ReadAsync(bytes);
                    }
                    CheckDirectory(filePath);

                    await System.IO.File.WriteAllBytesAsync(filePath, bytes);
                }

                return new { success = true, file = Combine(Container, fileName) };
            }
            catch (Exception ex)
            {
                return new { success = false, file = ex.Message };
            }
        }

        private static void CheckDirectory(string path)
        {
            var dir = path.Substring(0, path.LastIndexOf('\\'));
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        private static string Combine(params string[] paths)
        {
            if (paths == null || !paths.Any())
                return string.Empty;

            var path = paths[0].Replace("/", "\\");

            for (var n = 1; n < paths.Count(); n++)
            {
                paths[n] = paths[n].Replace("/", "\\");

                if (path.EndsWith("\\"))
                {
                    if (paths[n].StartsWith("\\"))
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
                    if (paths[n].StartsWith("\\"))
                    {
                        path += paths[n];
                    }
                    else
                    {
                        path += "\\" + paths[n];
                    }
                }
            }
            return path;
        }
    }
}
