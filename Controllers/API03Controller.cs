using AngleSharp;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text;

namespace TakeHtml.Controllers
{
    //�Ѧҳs��:https://cloud.tencent.com/developer/article/1876712
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

            //���G�t�@�ӫܱ`�b�t�m�B�z���O�ϥ� LoaderOptions �[�W AngleSharp.Css ���Ѫ� WithCss() �ӧ�� CSS �B�z�᪺���G�A�Ҧp�G
            var config = AngleSharp.Configuration.Default
                .WithDefaultLoader(new AngleSharp.Io.LoaderOptions
                {
                    IsResourceLoadingEnabled = true

                }).WithCss().WithDefaultCookies();

            var browser = BrowsingContext.New(config);

            var url = new AngleSharp.Dom.Url(URL);

            //�ϥ� SetCookie ��ؼЫ��w�n�Ϊ� Cookie
            //�t�mCookie(over18=1)�A��18��
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

            //�ϥοz�ﾹ�ӧ���w�����e
            //�{�b�ڭ̤w�g���\��������e��^�ӤF�A���۴N�O�n�ϥο�ܾ��ӧ�X�ڭ̷Q�n�����e�o�C
            //�p�G���ιL JQuery �άO��Ѽg CSS ���B�����Ӥ��|���͡A�j�P�W�O�o�˪��G
            //div: �N�O�� <div>
            //#wow: �� id �O wow ������
            //.hello: �� class �O hello ������
            //��M�]�i�H�[�H�զX�A�Ҧp div#wow, div > p.hello ���A�����쪺�B�ͥi�H�A�ݤ@�� �泾�е{�� CSS ��ܾ������A��L���p�N���ݭn���ɭԦA�d��Y�i�C
            //�������ڭ̦n�n�[�� HTML ���c�A�i�H�o�{���D�جO��b class="r-ent" �� div �̡A���ڭ̴N�i�H�o�ˤU Selector�Gdiv.r-ent
            //���ۧڭ̴N��� QuerySelectorAll() �o�Ӥ�k�ӥ� Selector ����ڭ̷Q�n�������G
            var queryCss = document.QuerySelectorAll(CssSelect).Select(node => node.InnerHtml);

            //��M AngleSharp �]���ѤF QuerySelector �ӧ����Ӥ����A�o��Ӥ�k�ΰ_�өM JavaScript ���������ӬO�t���h��
            var queryCss2 = document.QuerySelector(CssSelect)?.InnerHtml;

            var sfsdfds = document.QuerySelectorAll(CssSelect).ToList();
            return Ok(queryCss);
        }

        public Post GetPost(string URL)
        {
            var document = GetIDocument(URL).Result;

            var post = document.QuerySelector("div.r-ent");

            //�Ӭ��F����s���A�o��|�ݭn�ϥ� GetAttribute �ӧ�������� href �ݩʡC�o�˼��D�M�s���N�d�w�F�C
            //�t�~�n�`�N���O�G�p�G�峹�Q�R���F�A�i�O�줣��o�ǪF�誺�I�ҥH�i�H�b QuerySelector ����� ?. ���覡�Ӱ��� Null �ɪ����b�A�̫�]�i�H�A�� Where �ӹL�o���L�Ī��峹�C
            var titleElement = post?.QuerySelector("div.title > a");
            var title = titleElement?.InnerHtml;
            var link = titleElement?.GetAttribute("href");

            //�ѤU�����N�ƥi�H�ݨ�O��b < div class="nrec"> �̭��A�o��ڧƱ���ഫ���Ʀr�A��K�ڭ̫���p�G�n�α��N�ư��z��C�ҥH���ڭ��B�~�B�z�@�U�G
            //�u��ܡu�z�v�A�o�ɭԧڭ̴N���@ 100
            //�p�G�����T���Ʀr�A�ഫ�� Int
            //�S���Ʀr���ܴN�� 0�C
            var pushString = post?.QuerySelector("div.nrec > span")?.InnerHtml;
            var pushCount = pushString == "�z" ? 100 : Int16.TryParse(pushString, out var push) ? push : 0;

            return new Post { Title = title, Link = link, Push = pushCount };
        }

        public IEnumerable<Post>? GetPosts(string URL)
        {
            var document = GetIDocument(URL).Result;

            var postSource = document.QuerySelectorAll("div.r-ent");

            //�����ѦҤW�����@�����g�k
            var posts = postSource?.Select(post =>
            {
                var titleElement = post.QuerySelector("div.title > a");
                var title = titleElement?.InnerHtml;
                var link = titleElement?.GetAttribute("href");

                var pushString = post.QuerySelector("div.nrec > span")?.InnerHtml;
                var pushCount =
                    pushString == "�z" ? 100 :
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


        //�f�t���j�Ӧh���X��
        public async Task<IEnumerable<Post>?> GetPagesPosts(string? baseUrl, string? pageUrl, int remainingPages)
        {
            //�Ѧҫe�����g�k�@���g�U�Ӫ�

            //�{�b���ڭ̧�W�����B�z�B�J�v�@�h�����k���A�w���|�ݭn�G
            //���ո� Url�B�]�w Cookie �M�� Open ��^�������e
            //����o�@�����Ҧ��峹���D
            //���o�U�@�����s��
            //���j���o�U�@���Ω��᭶�ƪ��峹�C��
            //��o�@���M�U�@�����᪺�峹�C��ո˰_�Ӧ^��

            var document = GetIDocument(baseUrl + pageUrl).Result;

            var postSource = document.QuerySelectorAll("div.r-ent");

            //�����ѦҤW�����@�����g�k
            var posts = postSource?.Select(post =>
            {
                var titleElement = post.QuerySelector("div.title > a");
                var title = titleElement?.InnerHtml;
                var link = titleElement?.GetAttribute("href");

                var pushString = post.QuerySelector("div.nrec > span")?.InnerHtml;
                var pushCount =
                    pushString == "�z" ? 100 :
                    Int16.TryParse(pushString, out var push) ? push : 0;

                return new Post
                {
                    Title = title,
                    Link = link,
                    Push = pushCount
                };
            })
            .Where(post => post.Title != null);

            //�f�t���j�Ӧh���X��
            //���ڭ̫e���w�g�� AngleSharp ���򥻾ާ@�]���@���F�A�򥻤W���~�G�O�u���}�ؼк��� �� ���ؼФ��� �� �οz�ﾹ��X�ӡv�o�˪� Loop�A�o�`�u�O������o�Ӹ}�������@�I�Ӥw�C
            //�]�����P���쪺�B�ͤ]�i�H�������L�o�@�q�A�����e���A�ǳƥX�o�h�ʤ��ۤv�Q�n�������o�C
            //�{�b���ڭ̦^��峹�C��ӡA�u��Ĥ@����b�S����d�Y�C�p�G�ڭ̷Q�n�����A���򭺥��N�n����X�o�Ӵ������s�G
            //�Q�Ϋe�����쪺�Ԣ����j�k�A�ڭ̥i�H����o�ӫ��s�� Selector �y�k�A���ڭ̪������ QuerySelector �̡A�åB���o���� href �ݩʡG
            var nextPageUrl = document.QuerySelector("div.btn-group.btn-group-paging > a:nth-child(2)")?.GetAttribute("href");

            document.Close();//������������i�H���� Close()

            remainingPages--;
            if (remainingPages == 0)
            {
                return posts;
            }

            var nextPagePosts = await GetPagesPosts(baseUrl, nextPageUrl, remainingPages);

            var postsAll = posts.Concat(nextPagePosts);

            //�̫�N�i�H���ӧڭ̪��n�D�ӳB�z posts ���峹�աA�Ҧp�G
            //���W�N�i�H��X�Q���� 90 ���H�W����A�ҥH�� Linq �N�O��K���C
            var posts90 = postsAll.Where(post => post.Push > 90);

#pragma warning disable CS8604 // �i�঳ Null �ѦҤ޼ơC
            return posts90;
#pragma warning restore CS8604 // �i�঳ Null �ѦҤ޼ơC
        }
    }
}