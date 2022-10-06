using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TakeHtml.Models.AvDB
{
    public partial class PostM
    {
        [Key]
        public long PostMpk { get; set; }
        public string PostMname { get; set; } = null!;
        public DateTime? CrTime { get; set; } = null!;
        public string? Title { get; set; }
        public string? Link { get; set; }
        public DateTime? Date { get; set; }
        public string Project { get; set; } = null!;
        public string? imgUrl { get; set; }
        public string? torrentUrl { get; set; }

    }
}
