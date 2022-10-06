using AngleSharp;
using AngleSharp.Dom;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using TakeHtml.Models.AvDB;
using Dapper;
using Microsoft.EntityFrameworkCore;
using AngleSharp.Html.Parser;
using System.Text.RegularExpressions;
using System;

//using System.Data;
//using System.Text;
//using System.Linq;
//using System;
//using HtmlAgilityPack;
//using System.Net;
//using System.Collections.Generic;

namespace TakeHtml.Controllers
{
    //[ApiController]
    [Route("[controller]/[action]")]
    public class sukebeiController : ControllerBase
    {
        private readonly AvDBContext _Ocn;
        public sukebeiController(AvDBContext Ocn) => _Ocn = Ocn;

        //private readonly IDbConnection _DapperOcn;
        //public AVController(IDbConnection DapperOcn) => _DapperOcn = DapperOcn;

        //private readonly WebAPIContext _context;
        //public TestBaseController(WebAPIContext context, IConfiguration configuration)
        //{
        //    _context = context;
        //    HelperDapper.OcnStr = configuration.GetConnectionString("WebAPI"); //_context.Database.GetConnectionString()
        //}

        //private readonly IConnectionFactory _factory;
        //public AVController(IConnectionFactory factory) => this._factory = factory;

        //    public string Index()
        //    {
        //        //..驗證資料,假如資料格式錯誤,不需要建立Connection
        //        using (var cnn = this._factory.CreateConnection())
        //        {
        //            var result = cnn.QueryFirst<string>("select 'Hello World' message");
        //            return result;
        //        }
        //    }
        //}

        private async Task<AngleSharp.Dom.IDocument> GetIDocument(string URL)
        {
            ////其它寫法沒有試過，感覺可自定義獲取的條件
            ////使用AngleSharp下载获取html代码
            //var requester = new DefaultHttpRequester("Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/69.0.3497.100 Safari/537.36");
            ////https://www.cjavapy.com/article/696/
            //requester.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");
            //requester.Headers.Add("Referer", "");
            //requester.Headers.Add("Accept-Language", "zh-Hans-CN,zh-Hans;q=0.8,en-US;q=0.5,en;q=0.3");
            //var context = BrowsingContext.New(Configuration.Default.WithLocaleBasedEncoding().WithDefaultLoader().WithDefaultCookies().With(requester));
            ////根据虚拟请求/响应模式创建文档
            ////https://www.cjavapy.com/article/696/
            //var document = context.OpenAsync(url).Result;


            var config = AngleSharp.Configuration.Default
                .WithDefaultLoader()
                .WithDefaultCookies();

            //註：另一個很常在配置處理的是使用 LoaderOptions 加上 AngleSharp.Css 提供的 WithCss() 來抓取 CSS 處理後的結果，例如：
            //var config = AngleSharp.Configuration.Default
            //    .WithDefaultLoader(new AngleSharp.Io.LoaderOptions
            //    {
            //        IsResourceLoadingEnabled = true

            //    }).WithCss().WithDefaultCookies();

            var browser = BrowsingContext.New(config);

            var url = new AngleSharp.Dom.Url(URL);

            //使用 SetCookie 對目標指定要用的 Cookie
            //配置Cookie(over18=1)，滿18歲
            //browser.SetCookie(url, "over18=1'");

            var document = await browser.OpenAsync(url);

            return document;
        }

        [HttpPost]
        public IEnumerable<PostM> GetPosts(IDocument document, DateTime startDate, DateTime endDate, string CssSelect = "", string baseUrl = "")
        {
            var postSource = document.QuerySelectorAll(CssSelect);

            //說明參考上面捉一筆的寫法
            var _PostMs = postSource.Select(item =>
            {
                //而為了拿到連結，這邊會需要使用 GetAttribute 來抓取元素的 href 屬性。這樣標題和連結就搞定了。
                //另外要注意的是：如果文章被刪掉了，可是抓不到這些東西的！所以可以在 QuerySelector 之後用 ?. 的方式來做個 Null 時的防呆，最後也可以再用 Where 來過濾掉無效的文章。
                var _itemDate = DateTime.Parse(item.Children[4].TextContent);
                var _itemtitle = item.Children[1].Children[0].TextContent;

                //var titleElement = item?.QuerySelector("[itemprop='url']");
                //var title = titleElement?.QuerySelector("[itemprop='title']")?.InnerHtml;
                //if (_itemtitle == title)
                //{
                //    //之後看情況判斷是否要在檢查title是否相同
                //}
                var link = item.Children[1].Children[0]?.GetAttribute("href");

                //剩下的推噓數可以看到是放在 < div class="nrec"> 裡面，這邊我希望能轉換成數字，方便我們後續如果要用推噓數做篩選。所以讓我們額外處理一下：
                //只顯示「爆」，這時候我們就視作 100
                //如果有明確的數字，轉換為 Int
                //沒有數字的話就當成 0。
                //var pushString = item?.QuerySelector("div.nrec > span")?.InnerHtml;
                //var pushCount = pushString == "爆" ? 100 : Int16.TryParse(pushString, out var push) ? push : 0;

                return new PostM
                {
                    Date = _itemDate,
                    Title = _itemtitle.Replace("[HD/720p]", ""),
                    Link = baseUrl + link,
                    Project = startDate.ToString("yyyy-MM-dd") + '~' + endDate.ToString("yyyy-MM-dd")
                };
            }).Where(x => x.Title != null && x.Date < startDate && x.Date > endDate);

            return _PostMs;
        }

        //搭配遞迴來多撈幾頁
        public async Task<IActionResult> GetPagesPosts(DateTime startDate = default(DateTime), DateTime endDate = default(DateTime), string pageUrl = "", string CssSelect = "", string nextPageCssSelect = "", string baseUrl = "")
        {
            //參考前面的寫法一路寫下來的
            //現在讓我們把上面的處理步驟逐一搬移到方法中，預期會需要：
            //先組裝 Url、設定 Cookie 然後 Open 抓回網頁內容
            //抓取這一頁的所有文章標題
            //取得下一頁的連結
            //遞迴取得下一頁及往後頁數的文章列表
            //把這一頁和下一頁往後的文章列表組裝起來回傳

            if (startDate == DateTime.MinValue) startDate = DateTime.Now.AddDays(-3);
            if (endDate == DateTime.MinValue) endDate = DateTime.Now;

            var PostMsAll = new List<PostM>();
            for (int i = 0; i < 100; i++)
            {
                var document = GetIDocument(baseUrl + pageUrl).Result;

                var PostMs = GetPosts(document, startDate, endDate, CssSelect, baseUrl);

                PostMsAll = PostMsAll.Concat(PostMs).ToList();
                if (PostMs.Any())
                {
                    pageUrl = document.QuerySelector(nextPageCssSelect)?.GetAttribute("href") ?? "";
                }
                else
                {
                    document.Close();//網頁爬完之後可以順手 Close()
                    break;
                }
            }

            var PostMsAllfilter = PostMsAll.Where(x => !x.Title.Contains("FC2PPV") && !x.Title.Contains("HEYZO") && !x.Title.Contains("エッチな") && !x.Title.Contains("人妻斬り") && !x.Title.Contains("10musume") && !x.Title.Contains("Carib") && !x.Title.Contains("1Pondo") && !x.Title.Contains("Kin8tengoku") && !x.Title.Contains("Pacopacomama") && !x.Title.Contains("[HD]"));

            _Ocn.PostMs.AddRange(PostMsAllfilter);
            var _saveCount = _Ocn.SaveChanges();
            var _res = _Ocn.PostMs.Where(x => x.Project == startDate.ToString("yyyy-MM-dd") + '~' + endDate.ToString("yyyy-MM-dd"));

            //var sql = "INSERT INTO PostM (Title, Link, Date, Project) VALUES ( @Title, @Link, @Date, @Project)";
            //await _Ocn.Database.GetDbConnection().ExecuteAsync(sql, PostMsAll);
            //var _res = await _Ocn.Database.GetDbConnection().QueryAsync<PostM>($"select * from PostM where Project= '{startDate.ToString("yyyy-MM-dd") + '~' + endDate.ToString("yyyy-MM-dd")}'");

            return Ok(_res);
        }

        public IActionResult returnHtml()
        {
            var source = @"
<!DOCTYPE html>
<html lang=en>
    <meta charset=utf-8>
    <meta name=viewport content=""initial-scale=1, minimum-scale=1, width=device-width"">
    <title>Error 404 (Not Found)!!1</title>
    <style></style>
    <body>
        <p><b>404.</b> <ins>That’s an error.</ins>
        <p>The requested URL <code>/error</code> was not found on this server.  <ins>That’s all we know.</ins>
    </body>
</html>";

            //使用AngleSharp的默认配置
            var config = Configuration.Default;
            //使用给定的配置创建用于评估web页面的新上下文
            var context = BrowsingContext.New(config);
            //只需要获得DOM表示
            var documentNew = context.OpenAsync(req => req.Content(source)).Result;


            //对html文档执行操作
            var p = documentNew.CreateElement("p");
            p.TextContent = "This is another paragraph.";
            documentNew.Body.AppendChild(p);


            //使用AngleSharp生成html代码自动缩进格式化
            var indentedText = "";
            using (var writer = new StringWriter())
            {
                documentNew.ToHtml(writer, new AngleSharp.Html.PrettyMarkupFormatter
                {
                    Indentation = "\t",
                    NewLine = "\n"
                });
                indentedText = writer.ToString();
            }

            return Ok(indentedText);

            //參考二
            //创建一个（可重用）解析器前端
            //var parser = new HtmlParser();
            ////html DOM节点
            //var source = " <h1>Some example source</h1> <p>This is a paragraph element</p> ";
            ////解析源文件
            //var document = parser.Parse(source);
            ////创建P标签
            //var p = document.CreateElement("p"); p.TextContent = "This is another paragraph.";
            ////添加到DOM
            //document.Body.AppendChild(p);
            ////返回完整html
            //var html = document.DocumentElement.OuterHtml;
        }

        public async Task<IActionResult> GetPostsDetails(string Project, string imgCssSel = "", string TorrentCssSel = "", string baseurl = "")
        {
            var _proJect = _Ocn.PostMs.Where(x => x.Project == Project).ToList();

            var updateItems = new List<dynamic>();
            foreach (var row in _proJect)
            {
                var _downLinks = GetPostDetail(row.Link, imgCssSel, TorrentCssSel, baseurl).Result;
                if (_downLinks != null)
                {
                    updateItems.Add(new
                    {
                        PostMpk = row.PostMpk,
                        imgUrl = _downLinks.imgUrl,
                        torrentUrl = _downLinks.torrentUrl
                    });
                }

                WebCrawler.BatchDownloadImages(@"ImgData", _downLinks.imgUrl, row.Title + ".jpg");
                WebCrawler.BatchDownloadImages(@"ImgData", _downLinks.torrentUrl, row.Title + ".torrent");
                Thread.Sleep(3000);
            }

            var sql = "UPDATE PostM SET PostMpk=@PostMpk, imgUrl=@imgUrl, torrentUrl=@torrentUrl WHERE PostMpk=@PostMpk";
            await _Ocn.Database.GetDbConnection().ExecuteAsync(sql, updateItems);

            var _res = await _Ocn.Database.GetDbConnection().QueryAsync<PostM>($"select * from PostM");

            return Ok(_res);
        }

        [HttpPost]
        public async Task<dynamic> GetPostDetail(string URL, string imgCssSel = "", string torrentCssSel = "", string baseurl = "")
        {
            var dom = GetIDocument(URL).Result;

            //捉一筆
            var _img = dom.QuerySelector(imgCssSel)?.InnerHtml;  //dom.QuerySelector(imgCssSel)?.InnerHtml;
            //捉多筆
            //var _imgs = dom.QuerySelectorAll(imgCssSel).Select(x => x.TextContent); //Select(node => node.InnerHtml);

            var _torrenthref = baseurl + dom.QuerySelector(torrentCssSel)?.GetAttribute("href");

            var _imghref = _img?.Split("(")[2].Split(")");
            var imgDom = GetIDocument(_imghref[0]).Result;
            var imgElement = imgDom?.QuerySelector("#body > p > img"); //Select(node => node.InnerHtml);;
            var imglink = imgElement?.GetAttribute("src");

            return new { imgUrl = imglink, torrentUrl = _torrenthref };
        }

        public class WebCrawler : WebUtility
        {
            public static void BatchDownloadImages(string saveDir, string ImgUrl, string fileName)
            {
                try
                {
                    Regex pattern = new Regex("[;,*/?#|]");
                    var restr = pattern.Replace(fileName, "");

                    //string fileExt = GetFileExtensionFromUrl(ImgUrl);
                    string SaveFilePath = Path.Combine(saveDir, restr);
                    //string SaveFilePath = Path.Combine(saveDir);
                    WebClient webClientImg = new WebClient();
                    webClientImg.DownloadFile(ImgUrl, SaveFilePath);
                }
                catch (Exception)
                {
                    throw;
                }
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
