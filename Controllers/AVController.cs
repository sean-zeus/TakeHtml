using AngleSharp;
using Microsoft.AspNetCore.Mvc;
using TakeHtml.Models.AvDB;
using Dapper;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text;


namespace TakeHtml.Controllers
{
    //[ApiController]
    [Route("[controller]/[action]")]
    public class AVController : ControllerBase
    {
        //private readonly AvDBContext _Ocn;
        //public AVController(AvDBContext Ocn) => _Ocn = Ocn;

        private readonly IDbConnection _DapperOcn;
        public AVController(IDbConnection DapperOcn) => _DapperOcn = DapperOcn;


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


        private async static Task<AngleSharp.Dom.IDocument> GetIDocument(string URL)
        {
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
            browser.SetCookie(url, "over18=1'");

            var document = await browser.OpenAsync(url);

            return document;
        }


        [HttpPost]
        public IActionResult GetPost(string URL, string CssSelect)
        {
            var document = GetIDocument(URL).Result;

            var post = document.QuerySelectorAll(CssSelect);

            var PostMs = new List<PostM>();
            foreach (var item in post)
            {
                //而為了拿到連結，這邊會需要使用 GetAttribute 來抓取元素的 href 屬性。這樣標題和連結就搞定了。
                //另外要注意的是：如果文章被刪掉了，可是抓不到這些東西的！所以可以在 QuerySelector 之後用 ?. 的方式來做個 Null 時的防呆，最後也可以再用 Where 來過濾掉無效的文章。
                var _itemDate = DateTime.Parse(item.TextContent.Split(" - ")[0]);
                var _itemtitle = item.TextContent.Split(" - ")?[1];

                var titleElement = item?.QuerySelector("[itemprop='url']");
                var title = titleElement?.QuerySelector("[itemprop='title']")?.InnerHtml;
                if (_itemtitle == title)
                {
                    //之後看情況判斷是否要在檢查title是否相同
                }
                var link = titleElement?.GetAttribute("href");

                //剩下的推噓數可以看到是放在 < div class="nrec"> 裡面，這邊我希望能轉換成數字，方便我們後續如果要用推噓數做篩選。所以讓我們額外處理一下：
                //只顯示「爆」，這時候我們就視作 100
                //如果有明確的數字，轉換為 Int
                //沒有數字的話就當成 0。
                //var pushString = item?.QuerySelector("div.nrec > span")?.InnerHtml;
                //var pushCount = pushString == "爆" ? 100 : Int16.TryParse(pushString, out var push) ? push : 0;

                PostMs.Add(new PostM { Date = _itemDate, Title = title, Link = link });
            }

            //return new Post { Title = title, Link = link, Push = pushCount };
            return Ok(PostMs);
        }

        [HttpPost]
        public IEnumerable<PostM>? GetPosts(string URL, string CssSelect)
        {
            var document = GetIDocument(URL).Result;

            var postSource = document.QuerySelectorAll(CssSelect);

            //說明參考上面捉一筆的寫法
            var PostMs = postSource?.Select(item =>
            {
                //而為了拿到連結，這邊會需要使用 GetAttribute 來抓取元素的 href 屬性。這樣標題和連結就搞定了。
                //另外要注意的是：如果文章被刪掉了，可是抓不到這些東西的！所以可以在 QuerySelector 之後用 ?. 的方式來做個 Null 時的防呆，最後也可以再用 Where 來過濾掉無效的文章。
                var _itemDate = DateTime.Parse(item.TextContent.Split(" - ")[0]);
                var _itemtitle = item.TextContent.Split(" - ")?[1];

                var titleElement = item?.QuerySelector("[itemprop='url']");
                var title = titleElement?.QuerySelector("[itemprop='title']")?.InnerHtml;
                if (_itemtitle == title)
                {
                    //之後看情況判斷是否要在檢查title是否相同
                }
                var link = titleElement?.GetAttribute("href");

                //剩下的推噓數可以看到是放在 < div class="nrec"> 裡面，這邊我希望能轉換成數字，方便我們後續如果要用推噓數做篩選。所以讓我們額外處理一下：
                //只顯示「爆」，這時候我們就視作 100
                //如果有明確的數字，轉換為 Int
                //沒有數字的話就當成 0。
                //var pushString = item?.QuerySelector("div.nrec > span")?.InnerHtml;
                //var pushCount = pushString == "爆" ? 100 : Int16.TryParse(pushString, out var push) ? push : 0;

                return new PostM
                {
                    Date = _itemDate,
                    Title = title,
                    Link = link
                };
            })
            .Where(x => x.Title != null);
            //.Where(x => x.Title != null && x.Date > DateTime.Parse("2022/01/19"));

            return PostMs;
        }


        //搭配遞迴來多撈幾頁
        public async Task<List<PostM>?> GetPagesPosts(DateTime startDate = default(DateTime), DateTime endDate = default(DateTime), string pageUrl = "", string CssSelect = "", string nextPageCssSelect = "", string baseUrl = "")
        {
            if (startDate == DateTime.MinValue) startDate = DateTime.Now.AddDays(-3);
            if (endDate == DateTime.MinValue) endDate = DateTime.Now;

            //參考前面的寫法一路寫下來的
            //現在讓我們把上面的處理步驟逐一搬移到方法中，預期會需要：
            //先組裝 Url、設定 Cookie 然後 Open 抓回網頁內容
            //抓取這一頁的所有文章標題
            //取得下一頁的連結
            //遞迴取得下一頁及往後頁數的文章列表
            //把這一頁和下一頁往後的文章列表組裝起來回傳

            var document = GetIDocument(baseUrl + pageUrl).Result;

            var postSource = document.QuerySelectorAll(CssSelect);

            //說明參考上面捉一筆的寫法
            var PostMs = postSource?.Select(item =>
            {
                //而為了拿到連結，這邊會需要使用 GetAttribute 來抓取元素的 href 屬性。這樣標題和連結就搞定了。
                //另外要注意的是：如果文章被刪掉了，可是抓不到這些東西的！所以可以在 QuerySelector 之後用 ?. 的方式來做個 Null 時的防呆，最後也可以再用 Where 來過濾掉無效的文章。
                var _itemDate = DateTime.Parse(item.TextContent.Split(" - ")[0]);
                var _itemtitle = item.TextContent.Split(" - ")?[1];

                var titleElement = item?.QuerySelector("[itemprop='url']");
                var title = titleElement?.QuerySelector("[itemprop='title']")?.InnerHtml;
                if (_itemtitle == title)
                {
                    //之後看情況判斷是否要在檢查title是否相同
                }
                var link = titleElement?.GetAttribute("href");

                //剩下的推噓數可以看到是放在 < div class="nrec"> 裡面，這邊我希望能轉換成數字，方便我們後續如果要用推噓數做篩選。所以讓我們額外處理一下：
                //只顯示「爆」，這時候我們就視作 100
                //如果有明確的數字，轉換為 Int
                //沒有數字的話就當成 0。
                //var pushString = item?.QuerySelector("div.nrec > span")?.InnerHtml;
                //var pushCount = pushString == "爆" ? 100 : Int16.TryParse(pushString, out var push) ? push : 0;

                return new PostM
                {
                    Date = _itemDate,
                    Title = title,
                    Link = link
                };
            })
            .Where(x => x.Title != null);

            PostMs = PostMs?.Where(x => x.Date > startDate && x.Date <= endDate);

            //startDate = startDate.AddDays(1);
            if (!PostMs.Any())
            {
                return PostMs.ToList();
            }

            //搭配遞迴來多撈幾頁
            //其實我們前面已經把 AngleSharp 的基本操作跑完一輪了，基本上不外乎是「打開目標網頁 → 找到目標元素 → 用篩選器抓出來」這樣的 Loop，這節只是單純讓這個腳本完善一點而已。
            //因此不感興趣的朋友也可以直接跳過這一段，直接前往，準備出發去動手抓自己想要的網頁囉。
            //現在讓我們回到文章列表來，只抓第一頁實在沒什麼搞頭。如果我們想要換頁，那麼首先就要先抓出這個換頁按鈕：
            //利用前面提到的Ｆ１２大法，我們可以拿到這個按鈕的 Selector 語法，讓我們直接丟到 QuerySelector 裡，並且取得它的 href 屬性：
            var nextPageUrl = document.QuerySelector(nextPageCssSelect)?.GetAttribute("href") ?? "";

            document.Close();//網頁爬完之後可以順手 Close() 

            var nextPagePosts = await GetPagesPosts(startDate, endDate, nextPageUrl, CssSelect, nextPageCssSelect);

#pragma warning disable CS8604 // 可能有 Null 參考引數。
            var PostMsAll = PostMs.Concat(nextPagePosts);
#pragma warning restore CS8604 // 可能有 Null 參考引數。

            //最後就可以按照我們的要求來處理 posts 的文章啦，例如：
            //馬上就可以抓出十頁內 90 推以上的文，所以說 Linq 就是方便哪。
            //var posts90 = PostMsAll.Where(x => x.Date < DateTime.Parse("2022/01/19")) ?? null;

            //_Ocn.PostMs.AddRange(PostMsAll);
            //_Ocn.SaveChanges();


            //..驗證資料,假如資料格式錯誤,不需要建立Connection
            //using (var _Ocn = this.CreateConnection())
            //{
            var result = _DapperOcn.QueryFirst<PostM>("select * from PostM where PostMname= 'B3B4CBAE9CA7E27216BE9CD7C6EEC5F7'");
            //}

            //var listxxx = _Ocn.Database.GetDbConnection().QueryFirst("select * from PostM where PostMname= 'B3B4CBAE9CA7E27216BE9CD7C6EEC5F7'");
            //var name = _Ocn.PostMs.FirstOrDefaultAsync(_ => _.PostMname == "B3B4CBAE9CA7E27216BE9CD7C6EEC5F7").Result;

            return PostMsAll.ToList();
        }
    }
}
