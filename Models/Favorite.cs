using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmojiSite.Models
{
    public class Favorite
    {
        public ulong UserId { get; set; }

        public int EmojiId { get; set; }

        public Favorite() { }

        public Favorite(ulong userId, int emojiId)
        {
            UserId = userId;
            EmojiId = emojiId;
        }
    }
}
