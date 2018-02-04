using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EmojiSite.Models
{
    public class CommentInfo
    {
        public Comment Comment { get; set; }
        public User Author { get; set; }

        public CommentInfo(User author, Comment comment)
        {
            Author = author;
            Comment = comment;
        }
    }
}
