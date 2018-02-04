using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EmojiSite.Models
{
    public class EmojiRow
    {
        public List<EmojiInfo> Row { get; set; }

        public EmojiRow() => Row = new List<EmojiInfo>();

        public EmojiRow(params EmojiInfo[] emojis)
        {
            Row = new List<EmojiInfo>();
            foreach (EmojiInfo e in emojis)
                Row.Add(e);
        }

        public void Add(EmojiInfo e) => Row.Add(e);
    }
}
