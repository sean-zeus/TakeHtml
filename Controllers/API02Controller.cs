using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text;
using TakeHtml.Model;

namespace TakeHtml.Controllers
{
    //參考連結:https://cloud.tencent.com/developer/article/1876712
    [ApiController]
    [Route("[controller]/[action]")]
    public class API02Controller : ControllerBase
    {
        public IActionResult GetHtmlDocText(string URL)
        {
            string strHtmlDocText = WebCrawler.GetHtmlDocText(URL);
            return Ok(strHtmlDocText);
        }

        public IActionResult GetAllImageLinks(string URL)
        {
            string strLinksOfImage = WebCrawler.GetAllImageLinks(URL);
            return Ok(strLinksOfImage);
        }

        public IActionResult BatchDownloadImages(string URL)
        {
            WebCrawler.BatchDownloadImages(URL, @"ImgData");

            return Ok("OK");
        }

        public class WebCrawler : WebUtility
        {
            public static string GetHtmlDocText(string strURL)
            {
                return GetHtmlDocObj(strURL).Text;
            }
            private static HtmlDocument GetHtmlDocObj(string strURL)
            {
                using (WebClient webClient = new WebClient())
                {
                    using (MemoryStream memoryStream = new MemoryStream(webClient.DownloadData(strURL)))
                    {
                        HtmlDocument doc = new HtmlDocument();
                        doc.Load(memoryStream, Encoding.UTF8);
                        return doc;
                    }
                }
            }
            public static void BatchDownloadImages(string strURL, string saveDir, string fileName = "img", int beginIdx = 1, int interval = 1)
            {
                try
                {
                    HtmlDocument doc = GetHtmlDocObj(strURL);

                    Uri myUri = new Uri(strURL);
                    string Uri = myUri.GetLeftPart(UriPartial.Authority);//獲取URI 的配置和授權區段(避免取得的圖片連結會有相對(不完整)路徑問題

                    var img_urls = doc.DocumentNode.Descendants("img")
                                    .Select(ele => ele.GetAttributeValue("src", null))
                                    .Where(s => !String.IsNullOrEmpty(s));
                    List<string> lsImgUrl = img_urls.ToList();
                    int idx = beginIdx;
                    foreach (string item in lsImgUrl)
                    {
                        string imgURL = "";
                        if (!item.StartsWith("http"))
                        {
                            imgURL = item.Insert(0, myUri.GetLeftPart(UriPartial.Authority));
                        }
                        else
                        {
                            imgURL = item;
                        }
                        string fileExt = GetFileExtensionFromUrl(imgURL);
                        string SaveFilePath = Path.Combine(saveDir, fileName + String.Format("_{0}{1}", idx, fileExt));
                        WebClient webClientImg = new WebClient();
                        webClientImg.DownloadFile(imgURL, SaveFilePath);
                        //webClientImg.DownloadFile(imgURL, String.Format(@"D:\ImgData\img_{0}{1}", idx, fileExt));
                        idx += interval;
                    }
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
            public static string GetAllImageLinks(string strURL)
            {
                HtmlDocument doc = GetHtmlDocObj(strURL);
                Uri myUri = new Uri(strURL);
                string Uri = myUri.GetLeftPart(UriPartial.Authority);
                var img_urls = doc.DocumentNode.Descendants("img")
                                .Select(ele => ele.GetAttributeValue("src", null))
                                .Where(s => !String.IsNullOrEmpty(s));
                List<string> lsImgUrl = img_urls.ToList();
                StringBuilder sbResult = new StringBuilder();
                foreach (string item in lsImgUrl)
                {
                    string imgURL = "";
                    //https://www.taifex.com.tw
                    if (!item.StartsWith("http"))
                    {
                        imgURL = item.Insert(0, myUri.GetLeftPart(UriPartial.Authority));
                    }
                    else
                    {
                        imgURL = item;
                    }
                    sbResult.AppendLine(imgURL);
                }
                return sbResult.ToString();
            }

        }

        public class WebUtility
        {
            /// <summary>
            /// 從URL中取得副檔名
            /// </summary>
            /// <param name="strURL"></param>
            /// <returns></returns>
            public static string GetFileExtensionFromUrl(string strURL)
            {
                strURL = strURL.Split('?')[0];
                strURL = strURL.Split('/').Last();
                return strURL.Contains('.') ? strURL.Substring(strURL.LastIndexOf('.')) : "";
            }
        }
    }
}