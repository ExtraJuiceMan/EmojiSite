using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EmojiSite.Models
{
    public class FavoriteResult
    {
        public bool Success { get; set; }
        public FavoriteAction Action { get; set; }

        public FavoriteResult() { }
        public FavoriteResult(bool success, FavoriteAction action)
        {
            Success = success;
            Action = action;
        }
    }

    public enum FavoriteAction
    {
        NONE,
        FAVORITE,
        UNFAVORITE
    }
}
