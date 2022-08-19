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
    public class API04Controller : ControllerBase
    {
        public IActionResult GetImg(string url)
        {
            AngleSharp.IConfiguration config = Configuration.Default.WithDefaultLoader();

            AngleSharp.Dom.IDocument doc = BrowsingContext.New(config).OpenAsync(url).Result;

            /*CSS Selector寫法*/
            List<string?> imgList = new List<string?>();
            AngleSharp.Dom.IHtmlCollection<AngleSharp.Dom.IElement> imgs = doc.QuerySelectorAll("img");//取得圖片
            foreach (AngleSharp.Dom.IElement img in imgs)
            {
                imgList.Add(img.GetAttribute("src"));
            }

            return Ok(imgList);
        }

        //把讀取出來的HTML代碼做修改
        public IActionResult changeReturnHtml()
        {
            //html代碼
            var source = @"<!DOCTYPE html>
                            <html>
                              <meta charset=utf-8>
                              <meta name=viewport content=""initial-scale=1, width=device-width"">
                              <title>Test Page</title>
                              <style>
                                *{margin:0;padding:0}html,code{font:15px/22px arial,sans-serif}html{background:#fff;color:#222;padding:15px} 
                              </style>
                            <div>
                                   <!--第一張圖沒有alt-->
                                   <img src=""Content/1.jpg"" />
                                   <br/>
                                   <!--第二張圖alt沒給值也沒有等號-->
                                   <img alt src=""Content/2.jpg"" /> 
                                   <br/>
                                    <img alt="""" src=""Content/3.jpg"" /> 
                                   <br/>
                                    <img alt='' src='Content/4.jpg' /> 
                                      <br/>
                                    <img alt=  src=Content/5.jpg /> 
                                    <br/>
                                    <img alt=test src=Content/6.jpg > 
                            </div>";

            AngleSharp.Dom.IDocument document = BrowsingContext.New(Configuration.Default.WithDefaultLoader())
                                .OpenAsync(req => req.Content(source)).Result;

            IEnumerable<AngleSharp.Dom.IElement> imgs = document.QuerySelectorAll("img");//取得所有img
            int i = 1;
            foreach (AngleSharp.Dom.IElement img in imgs)
            {
                img.SetAttribute("alt", "alt_" + i);//設定img的alt屬性
                i++;
            }
            //將修改後的html代碼輸出
            var change = document.ToHtml();

            return Ok(change);
        }
    }
}