using System;
using System.Collections.Generic;

namespace TakeHtml.Models.AvDB
{
    public partial class PostD
    {
        public long PostDpk { get; set; }
        public string PostDname { get; set; } = null!;
        public string CrTime { get; set; } = null!;
        public string? AvNo { get; set; }
        public string? AvTitle { get; set; }
        public string? AvPic { get; set; }
        public string? AvBt { get; set; }
    }
}
