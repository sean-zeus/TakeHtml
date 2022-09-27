using AngleSharp;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text;

namespace TakeHtml.Controllers
{
    //參考連結:https://cloud.tencent.com/developer/article/1876712
    [ApiController]
    [Route("[controller]/[action]")]
    public class API03Controller : ControllerBase
    {
        public class Post
        {
            public string? Title { get; set; }
            public int Push { get; set; }
            public string? Link { get; set; }
        }

        private async static Task<AngleSharp.Dom.IDocument> GetIDocument(string URL)
        {
            //var config = AngleSharp.Configuration.Default
            //    .WithDefaultLoader()
            //    .WithDefaultCookies();

            //註：另一個很常在配置處理的是使用 LoaderOptions 加上 AngleSharp.Css 提供的 WithCss() 來抓取 CSS 處理後的結果，例如：
            var config = AngleSharp.Configuration.Default
                .WithDefaultLoader(new AngleSharp.Io.LoaderOptions
                {
                    IsResourceLoadingEnabled = true

                }).WithCss().WithDefaultCookies();

            var browser = BrowsingContext.New(config);

            var url = new AngleSharp.Dom.Url(URL);

            //使用 SetCookie 對目標指定要用的 Cookie
            //配置Cookie(over18=1)，滿18歲
            browser.SetCookie(url, "over18=1'");

            var document = await browser.OpenAsync(url);

            return document;
        }

        public IActionResult GetHtmlTextAsync(string URL)
        {
            var document = GetIDocument(URL).Result;

            return Ok(document.ToHtml());
        }

        public IActionResult GetCssSelect(string URL, string CssSelect)
        {
            var document = GetIDocument(URL).Result;

            //使用篩選器來抓指定的內容
            //現在我們已經成功把網頁內容抓回來了，接著就是要使用選擇器來抓出我們想要的內容囉。
            //如果有用過 JQuery 或是整天寫 CSS 的朋友應該不會陌生，大致上是這樣的：
            //div: 就是抓 <div>
            //#wow: 抓 id 是 wow 的元素
            //.hello: 抓 class 是 hello 的元素
            //當然也可以加以組合，例如 div#wow, div > p.hello 等，有興趣的朋友可以再看一眼 菜鳥教程的 CSS 選擇器說明，其他狀況就等需要的時候再查表即可。
            //首先讓我們好好觀察 HTML 結構，可以發現標題框是放在 class="r-ent" 的 div 裡，那我們就可以這樣下 Selector：div.r-ent
            //接著我們就能用 QuerySelectorAll() 這個方法來用 Selector 抓取我們想要的元素：
            var queryCss = document.QuerySelectorAll(CssSelect).Select(node => node.InnerHtml);

            //當然 AngleSharp 也提供了 QuerySelector 來抓取單個元素，這兩個方法用起來和 JavaScript 的體驗應該是差不多啦
            var queryCss2 = document.QuerySelector(CssSelect)?.InnerHtml;

            var sfsdfds = document.QuerySelectorAll(CssSelect).ToList();
            return Ok(queryCss);
        }

        public Post GetPost(string URL)
        {
            var document = GetIDocument(URL).Result;

            var post = document.QuerySelector("div.r-ent");

            //而為了拿到連結，這邊會需要使用 GetAttribute 來抓取元素的 href 屬性。這樣標題和連結就搞定了。
            //另外要注意的是：如果文章被刪掉了，可是抓不到這些東西的！所以可以在 QuerySelector 之後用 ?. 的方式來做個 Null 時的防呆，最後也可以再用 Where 來過濾掉無效的文章。
            var titleElement = post?.QuerySelector("div.title > a");
            var title = titleElement?.InnerHtml;
            var link = titleElement?.GetAttribute("href");

            //剩下的推噓數可以看到是放在 < div class="nrec"> 裡面，這邊我希望能轉換成數字，方便我們後續如果要用推噓數做篩選。所以讓我們額外處理一下：
            //只顯示「爆」，這時候我們就視作 100
            //如果有明確的數字，轉換為 Int
            //沒有數字的話就當成 0。
            var pushString = post?.QuerySelector("div.nrec > span")?.InnerHtml;
            var pushCount = pushString == "爆" ? 100 : Int16.TryParse(pushString, out var push) ? push : 0;

            return new Post { Title = title, Link = link, Push = pushCount };
        }

        public IEnumerable<Post>? GetPosts(string URL)
        {
            var document = GetIDocument(URL).Result;

            var postSource = document.QuerySelectorAll("div.r-ent");

            //說明參考上面捉一筆的寫法
            var posts = postSource?.Select(post =>
            {
                var titleElement = post.QuerySelector("div.title > a");
                var title = titleElement?.InnerHtml;
                var link = titleElement?.GetAttribute("href");

                var pushString = post.QuerySelector("div.nrec > span")?.InnerHtml;
                var pushCount =
                    pushString == "爆" ? 100 :
                    Int16.TryParse(pushString, out var push) ? push : 0;

                return new Post
                {
                    Title = title,
                    Link = link,
                    Push = pushCount
                };
            })
            .Where(post => post.Title != null);

            return posts;
        }


        //搭配遞迴來多撈幾頁
        public async Task<IEnumerable<Post>?> GetPagesPosts(string? baseUrl, string? pageUrl, int remainingPages)
        {
            //參考前面的寫法一路寫下來的

            //現在讓我們把上面的處理步驟逐一搬移到方法中，預期會需要：
            //先組裝 Url、設定 Cookie 然後 Open 抓回網頁內容
            //抓取這一頁的所有文章標題
            //取得下一頁的連結
            //遞迴取得下一頁及往後頁數的文章列表
            //把這一頁和下一頁往後的文章列表組裝起來回傳

            var document = GetIDocument(baseUrl + pageUrl).Result;

            var postSource = document.QuerySelectorAll("div.r-ent");

            //說明參考上面捉一筆的寫法
            var posts = postSource?.Select(post =>
            {
                var titleElement = post.QuerySelector("div.title > a");
                var title = titleElement?.InnerHtml;
                var link = titleElement?.GetAttribute("href");

                var pushString = post.QuerySelector("div.nrec > span")?.InnerHtml;
                var pushCount =
                    pushString == "爆" ? 100 :
                    Int16.TryParse(pushString, out var push) ? push : 0;

                return new Post
                {
                    Title = title,
                    Link = link,
                    Push = pushCount
                };
            })
            .Where(post => post.Title != null);

            //搭配遞迴來多撈幾頁
            //其實我們前面已經把 AngleSharp 的基本操作跑完一輪了，基本上不外乎是「打開目標網頁 → 找到目標元素 → 用篩選器抓出來」這樣的 Loop，這節只是單純讓這個腳本完善一點而已。
            //因此不感興趣的朋友也可以直接跳過這一段，直接前往，準備出發去動手抓自己想要的網頁囉。
            //現在讓我們回到文章列表來，只抓第一頁實在沒什麼搞頭。如果我們想要換頁，那麼首先就要先抓出這個換頁按鈕：
            //利用前面提到的Ｆ１２大法，我們可以拿到這個按鈕的 Selector 語法，讓我們直接丟到 QuerySelector 裡，並且取得它的 href 屬性：
            var nextPageUrl = document.QuerySelector("div.btn-group.btn-group-paging > a:nth-child(2)")?.GetAttribute("href");

            document.Close();//網頁爬完之後可以順手 Close()

            remainingPages--;
            if (remainingPages == 0)
            {
                return posts;
            }

            var nextPagePosts = await GetPagesPosts(baseUrl, nextPageUrl, remainingPages);

            var postsAll = posts.Concat(nextPagePosts);

            //最後就可以按照我們的要求來處理 posts 的文章啦，例如：
            //馬上就可以抓出十頁內 90 推以上的文，所以說 Linq 就是方便哪。
            var posts90 = postsAll.Where(post => post.Push > 90);

#pragma warning disable CS8604 // 可能有 Null 參考引數。
            return posts90;
#pragma warning restore CS8604 // 可能有 Null 參考引數。
        }
    }
}