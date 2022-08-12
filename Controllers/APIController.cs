using CsvHelper;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using TakeHtml.Model;

namespace TakeHtml.Controllers
{
    //參考連結:https://cloud.tencent.com/developer/article/1876712
    [ApiController]
    [Route("[controller]/[action]")]
    public class APIController : ControllerBase
    {
        private readonly ILogger<APIController> _logger;

        public APIController(ILogger<APIController> logger)
        {
            _logger = logger;
        }
        public class Book
        {
            public string? Title { get; set; }
            public string? Price { get; set; }
        }

        public IActionResult ExportToCSV(string url)
        {
            var books = GetBookDetails(url);

            exportToCSV(books);

            return Ok("OK");
        }

        public List<Book> GetBookDetails(string url)
        {
            var urls = GetBookLinks(url);

            var books = new List<Book>();

            foreach (var urlstr in urls)
            {
                HtmlDocument document = GetDocument(urlstr);
                var titleXPath = "//h1";
                var priceXPath = "//div[contains(@class,\"product_main\")]/p[@class=\"price_color\"]";
                var book = new Book();
                book.Title = document.DocumentNode.SelectSingleNode(priceXPath).InnerText;
                book.Price = document.DocumentNode.SelectSingleNode(priceXPath).InnerText;
                books.Add(book);
            }
            return books;
        }

        public List<string> GetBookLinks(string url)
        {
            var bookLinks = new List<string>();
            HtmlDocument doc = GetDocument(url);
            HtmlNodeCollection linkNodes = doc.DocumentNode.SelectNodes("//h3/a");
            var baseUri = new Uri(url);
            foreach (var link in linkNodes)
            {
                string href = link.Attributes["href"].Value;
                bookLinks.Add(new Uri(baseUri, href).AbsoluteUri);
            }
            return bookLinks;
        }

        static HtmlDocument GetDocument(string url)
        {
            HtmlWeb web = new HtmlWeb();
            HtmlDocument doc = web.Load(url);
            return doc;
        }
        static void exportToCSV(List<Book> books)
        {
            using (var writer = new StreamWriter("./books.csv"))

            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(books);
            }
        }
    }
}