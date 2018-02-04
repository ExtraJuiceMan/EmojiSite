using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EmojiSite.Models
{
    public class EmojiInfo
    {
        public Emoji Emoji { get; set; }
        public User Author { get; set; }
        public int Favorites { get; set; }
        
        public EmojiInfo(Emoji emoji, User author, int favorites)
        {
            Emoji = emoji;
            Author = author;
            Favorites = favorites;
        }
    }
}
