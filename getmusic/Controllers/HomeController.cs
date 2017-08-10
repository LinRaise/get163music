using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Net;
using System.IO;
using System.Text.Encodings.Web;
namespace getmusic.Controllers
{
    public class HomeController : Controller
    {
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
            return Json(new { data = getHtml(id) });
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

        [HttpPost]
        public async Task<IActionResult> GetMusicSrc(List<Parms> parms)
        {
            foreach (var parm in parms)
            {
                StringBuilder sb = new StringBuilder();
                
                sb.AppendFormat("params={0}", System.Net.WebUtility.UrlEncode(parm.prams));
                sb.AppendFormat("&encSecKey={0}", parm.encSecKey);
                await PostMoths("http://music.163.com/weapi/song/enhance/player/url?csrf_token=", sb.ToString());
            }

            return Json(new { });
        }

        public static async Task<string> PostMoths(string url, string param)
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
            response = (System.Net.HttpWebResponse) await request.GetResponseAsync();
            System.IO.Stream s;
            s = response.GetResponseStream();
            string StrDate = "";
            string strValue = "";
            StreamReader Reader = new StreamReader(s, Encoding.UTF8);
            while ((StrDate = Reader.ReadLine()) != null)
            {
                strValue += StrDate + "\r\n";
            }
            return ne;
        }


        public object getHtml(string id)
        {
            var re = new Dictionary<string, string>();
            var te = NSoup.NSoupClient.Connect("http://music.163.com/playlist?id=" + id)
           .Header("Referer", "http://music.163.com/")
           .Header("Host", "music.163.com").Get().Select("ul[class=f-hide] a")
           .ToList();
            te.ForEach(p =>
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
}
