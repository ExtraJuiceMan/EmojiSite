using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EmojiSite.Models
{
    public class Comment
    {
        [Key]
        public int Id { get; set; }
        public int EmojiId { get; set; }
        public ulong AuthorId { get; set; }
        public string Content { get; set; }
        public DateTime Submitted { get; set; }

        public Comment() { }
    }
}
