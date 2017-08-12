using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Net;
using System.IO;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace getmusic.Controllers
{
    public class HomeController : Controller
    {
        private IHostingEnvironment env;
        public HomeController(IHostingEnvironment env)
        {
            this.env = env;
        }
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        /// <summary>
        /// 获取歌单播放列表
        /// </summary>
        /// <returns></returns>
        public IActionResult getMusicList(string id)
        {
            var title = string.Empty;
            return Json(new { data = getHtml(id, ref title), title = title });
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View();
        }

        private static CookieContainer cookie = new CookieContainer();
        private static string contentType = "application/x-www-form-urlencoded";
        private static string accept = "image/gif, image/x-xbitmap, image/jpeg, image/pjpeg, application/x-shockwave-flash, application/x-silverlight, application/vnd.ms-excel, application/vnd.ms-powerpoint, application/msword, application/x-ms-application, application/x-ms-xbap, application/vnd.ms-xpsdocument, application/xaml+xml, application/x-silverlight-2-b1, */*";
        private static string userAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1; .NET CLR 2.0.50727; .NET CLR 3.0.04506.648; .NET CLR 3.5.21022)";

        /// <summary>  
        ///   
        /// </summary>  
        /// <param name="url">网页地址</param>  
        /// <param name="encoding">编码方式</param>  
        /// <returns></returns>  
        public static async Task<string> GetHtmlEx(string url, Encoding encoding)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Headers["UserAgent"] = userAgent;
            request.ContentType = contentType;
            request.CookieContainer = cookie;
            request.Accept = accept;
            request.Method = "get";

            WebResponse response = await request.GetResponseAsync();
            Stream responseStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(responseStream, encoding);
            String html = reader.ReadToEnd();
            response.Dispose();

            return html;
        }

        /// <summary>
        /// 开始下载音乐
        /// </summary>
        /// <param name="parms"></param>
        /// <param name="title"></param>
        /// <param name="taskNum"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult GetMusicSrc(List<Parms> parms, string title, int taskNum = 5)
        {
            taskNum = parms.Count / Environment.ProcessorCount * 2;
            taskNum = taskNum <= 0 ? 5 : taskNum;
            var varNewParms = new List<List<Parms>>();
            var maxCount = parms.Count / taskNum;
            for (var i = 0; i < maxCount; i++)
            {
                varNewParms.Add(parms.GetRange(taskNum * i, taskNum));
            }
            if (maxCount * taskNum < parms.Count)
            {
                varNewParms.Add(parms.GetRange(maxCount * taskNum, parms.Count - maxCount * taskNum));
            }
            varNewParms.ForEach(async p =>
            {
                await DownLoad(p, title);
            });

            return Json(new { });
        }

        public Task DownLoad(List<Parms> parms, string title)
        {

            StringBuilder sb = new StringBuilder();

            return Task.Run(async () =>
            {
                foreach (var parm in parms)
                {
                    sb.Clear();
                    sb.AppendFormat("params={0}", System.Net.WebUtility.UrlEncode(parm.prams));
                    sb.AppendFormat("&encSecKey={0}", parm.encSecKey);
                    var ms = await PostMoths("http://music.163.com/weapi/song/enhance/player/url?csrf_token=", sb.ToString());
                    if (!string.IsNullOrEmpty(ms.url))
                    {
                        var varRe = HttpDownloadAsync(ms.url, Path.Combine(new string[] { env.ContentRootPath, "download", title, string.Format("{0}{1}", GetFileName(parm.name), Path.GetExtension(ms.url)) }));
                    }
                }
            });


        }

        public static string GetFileName(string oldName)
        {
            StringBuilder rBuilder = new StringBuilder(oldName);
            foreach (char rInvalidChar in Path.GetInvalidFileNameChars())
                rBuilder.Replace(rInvalidChar.ToString(), string.Empty);
            return rBuilder.ToString();
        }

        /// <summary>
        /// http下载文件
        /// </summary>
        /// <param name="url">下载文件地址</param>
        /// <param name="path">文件存放地址，包含文件名</param>
        /// <returns></returns>
        public bool HttpDownloadAsync(string url, string path)
        {
            if (System.IO.File.Exists(path)) return true;
            string tempPath = System.IO.Path.GetDirectoryName(path) + @"\temp";
            System.IO.Directory.CreateDirectory(tempPath);  //创建临时文件目录
            string tempFile = tempPath + @"\" + System.IO.Path.GetFileName(path) + ".temp"; //临时文件
            if (System.IO.File.Exists(tempFile)) System.IO.File.Delete(tempFile);    //存在则删除
            try
            {
                using (FileStream fs = new FileStream(tempFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                {
                    // 设置参数
                    HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                    //发送请求并获取相应回应数据
                    HttpWebResponse response = request.GetResponseAsync().Result as HttpWebResponse;
                    //直到request.GetResponse()程序才开始向目标网页发送Post请求
                    using (var responseStream = response.GetResponseStream())
                    {
                        //创建本地文件写入流
                        //Stream stream = new FileStream(tempFile, FileMode.Create);
                        byte[] bArr = new byte[1024];
                        int size = responseStream.Read(bArr, 0, (int)bArr.Length);
                        while (size > 0)
                        {
                            //stream.Write(bArr, 0, size);
                            fs.Write(bArr, 0, size);
                            size = responseStream.Read(bArr, 0, (int)bArr.Length);
                        }
                    }
                }
                System.IO.File.Move(tempFile, path);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static async Task<DataItem> PostMoths(string url, string param)
        {
            string strURL = url;
            System.Net.HttpWebRequest request;
            request = (System.Net.HttpWebRequest)WebRequest.Create(strURL);
            request.Method = "POST";
            //request.Headers["Accept-Encoding"]= "gzip, deflate";
            request.Headers["Accept-Language"] = "zh-CN,zh;q=0.8";
            request.ContentType = "application/x-www-form-urlencoded";
            request.Headers["Accept"] = "*/*";
            request.Headers["Referer"] = "http://music.163.com/";
            request.Headers["Host"] = "music.163.com";
            request.Headers["UserAgent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.90 Safari/537.36";
            request.Headers["Origin"] = "http://music.163.com";
            string paraUrlCoded = param;
            byte[] payload;
            payload = System.Text.Encoding.UTF8.GetBytes(paraUrlCoded);
            //request.ContentLength = payload.Length;
            Stream writer = await request.GetRequestStreamAsync();
            writer.Write(payload, 0, payload.Length);
            writer.Dispose();
            System.Net.HttpWebResponse response;
            response = (System.Net.HttpWebResponse)await request.GetResponseAsync();
            System.IO.Stream s;
            s = response.GetResponseStream();
            string StrDate = "";
            StreamReader Reader = new StreamReader(s, Encoding.UTF8);
            StrDate = Reader.ReadLine();
            //while (() != null)
            //{
            //    strValue += StrDate + "\r\n";
            //}
            var varObj = Newtonsoft.Json.JsonConvert.DeserializeObject<Root>(StrDate);
            return varObj.data.FirstOrDefault();
        }


        public object getHtml(string id, ref string title)
        {
            var re = new Dictionary<string, string>();
            var te = NSoup.NSoupClient.Connect("http://music.163.com/playlist?id=" + id)
           .Header("Referer", "http://music.163.com/")
           .Header("Host", "music.163.com").Get();
            title = te.Title;
            var varSongs = te.Select("ul[class=f-hide] a").ToList();
            varSongs.ForEach(p =>
            {
                if (!re.ContainsKey(p.Text()))
                    re.Add(p.Text(), p.Attr("href").Split('=')[1]);
            });
            return re;
        }
    }

    public class Parms
    {
        public string name { get; set; }
        public string prams { get; set; }
        public string encSecKey { get; set; }
    }

    public class DataItem
    {
        /// <summary>
        /// 
        /// </summary>
        public int id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string url { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int br { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int size { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string md5 { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int code { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int expi { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string type { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double gain { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int fee { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string uf { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int payed { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int flag { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string canExtend { get; set; }
    }

    public class Root
    {
        /// <summary>
        /// 
        /// </summary>
        public List<DataItem> data { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int code { get; set; }
    }
}
