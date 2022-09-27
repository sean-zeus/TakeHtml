using AngleSharp.Dom;
using AngleSharp.Io;
using AngleSharp;
using static HelperDBEventLog;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace TakeHtml
{
    public class MyClass
    {
        /// <summary>
        /// MD5字符串加密
        /// </summary>
        /// <param name="txt"></param>
        /// <returns>加密后字符串</returns>
        public static string GenerateMD5(string txt)
        {
            using (MD5 mi = MD5.Create())
            {
                byte[] buffer = Encoding.Default.GetBytes(txt);
                //开始加密
                byte[] newBuffer = mi.ComputeHash(buffer);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < newBuffer.Length; i++)
                {
                    sb.Append(newBuffer[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }
        //下载文件
        public static bool DwonFile(IElement item, string fileName, string sourceUrl, string filePath, string sub, string attr, string dir, IDocument document)
        {
            //原文：https://www.cjavapy.com/article/696/
            sourceUrl = item.GetAttribute(attr);
            if (string.IsNullOrEmpty(sourceUrl))
                return true;
            if (dir == "jpg" && sourceUrl.IndexOf(";base64,") > -1 || (dir == "css" && sourceUrl.ToLower().IndexOf(".css") == 0))
                return true;
            WebClient webClient = new WebClient();
            sub = Path.Combine(filePath, dir);
            if (!Directory.Exists(sub))
                Directory.CreateDirectory(sub);
            //sourceUrl = fixUrl(sourceUrl, document.Origin);
            fileName = GenerateMD5(sourceUrl) + "." + dir;
            Console.WriteLine(sourceUrl);
            //log.Info(sourceUrl + " = " + sourceUrl);
            //https://www.cjavapy.com/article/696/
            if (!File.Exists(Path.Combine(sub, fileName)))
                try
                {
                    webClient.DownloadFile(sourceUrl, Path.Combine(sub, fileName));
                }
                catch (Exception ex)
                {
                    //log.Info("sourceUrl = " + sourceUrl + " dir = " + dir);
                    //log.Error(ex);
                }
            item.SetAttribute(attr, "/static/" + dir + "/" + fileName);
            item.SetAttribute("referrerPolicy", "no-referrer");
            return false;
        }
        public static string GetHtml(string url, string filePath)
        {
            var requester = new DefaultHttpRequester("Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/69.0.3497.100 Safari/537.36");
            //https://www.cjavapy.com/article/696/
            requester.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");
            requester.Headers.Add("Referer", "");
            requester.Headers.Add("Accept-Language", "zh-Hans-CN,zh-Hans;q=0.8,en-US;q=0.5,en;q=0.3");
            var context = BrowsingContext.New(Configuration.Default.WithLocaleBasedEncoding().WithDefaultLoader().WithDefaultCookies().With(requester));
            //根据虚拟请求/响应模式创建文档
            //https://www.cjavapy.com/article/696/
            var document = context.OpenAsync(url).Result;
            //var blueListItemsLinq = document.All.Where(m => m.LocalName == "li" && m.ClassList.Contains("blue"));
            //或者直接使用CSS选择器
            string sourceUrl = string.Empty;
            var scripts = document.QuerySelectorAll("script");
            string fileName = string.Empty;
            string sub = string.Empty;
            foreach (var item in scripts)
            {
                if (DwonFile(item, fileName, sourceUrl, filePath, sub, "src", "js", document))
                    continue;
            }
            var links = document.QuerySelectorAll("link");
            foreach (var item in links)
            {
                if (DwonFile(item, fileName, sourceUrl, filePath, sub, "href", "css", document))
                    continue;
            }
            var imgs = document.QuerySelectorAll("img");
            foreach (var item in imgs)
            {
                if (DwonFile(item, fileName, sourceUrl, filePath, sub, "src", "jpg", document))
                    continue;
            }
            return document.ToHtml();
        }
    }
}
