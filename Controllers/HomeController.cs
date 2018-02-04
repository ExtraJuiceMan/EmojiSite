using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using EmojiSite.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using System.IO;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Security.Claims;
using PaulMiami.AspNetCore.Mvc.Recaptcha;
using System.Net;
using Microsoft.EntityFrameworkCore;

namespace EmojiSite.Controllers
{
    public class HomeController : Controller
    {
        private readonly EmojiContext Context;

        public static List<string> Admins;
        public static List<string> Tags;

        public HomeController(EmojiContext context)
        {
            // Cache tag and admin lists on application start
            using (StreamReader s = new StreamReader("wwwroot/tags.json"))
            {
                string json = s.ReadToEnd();
                Tags = JsonConvert.DeserializeObject<TagList>(json).Tags;
            }
            using (StreamReader s = new StreamReader("admins.json"))
            {
                string json = s.ReadToEnd();
                Admins = JsonConvert.DeserializeObject<AdminList>(json).admins;
            }
            Context = context;
            Context.Database.EnsureCreated();
        }

        public IActionResult Index(string tags, string keyword, string orderBy, string orderType)
        {
            if (String.IsNullOrWhiteSpace(tags))
                tags = null;

            if (String.IsNullOrWhiteSpace(keyword))
                keyword = null;

            if (String.IsNullOrWhiteSpace(orderBy))
                orderBy = null;

            if (String.IsNullOrWhiteSpace(orderType))
                orderType = null;

            List<EmojiRow> rows = new List<EmojiRow>();

            var emojis = Context.Emoji
                    .AsNoTracking()
                    .Where(x => x.IsApproved);

            if (keyword != null)
                emojis = emojis.Where(x => x.Name.Contains(keyword));

            string[] tagList = null;
            if (tags != null)
                tagList = tags.Split(' ');

            // Can't do String.Split in entity framework query :(
            if (tags != null)
                foreach (string s in tagList)
                    emojis = emojis.Where(x => x.Tags == s ||
                    x.Tags.StartsWith(s + " ") ||
                    x.Tags.Contains(" " + s + " ") ||
                    x.Tags.EndsWith(" " + s));

            if (orderType != null && orderBy != null)
            {
                if (orderType == "desc")
                {
                    if (orderBy == "favorites" || orderBy == "submitted")
                    {
                        if (orderBy == "favorites")
                            emojis = emojis.OrderByDescending(x => Context.Favorites.AsNoTracking().Count(z => z.EmojiId == x.Id));
                        if (orderBy == "submitted")
                            emojis = emojis.OrderByDescending(x => x.Submitted);
                    }
                    else
                        emojis = emojis.OrderByDescending(x => x.Submitted);
                }
                else if (orderType == "asc")
                {
                    if (orderBy == "favorites" || orderBy == "submitted")
                    {
                        if (orderBy == "favorites")
                            emojis = emojis.OrderBy(x => Context.Favorites.AsNoTracking().Count(z => z.EmojiId == x.Id));
                        if (orderBy == "submitted")
                            emojis = emojis.OrderBy(x => x.Submitted);
                    }
                    else
                        emojis = emojis.OrderBy(x => x.Submitted);
                }
                else
                    emojis = emojis.OrderByDescending(x => x.Submitted);
            }
            else
                emojis = emojis.OrderByDescending(x => x.Submitted);

            List<EmojiInfo> e = emojis
                .Join(Context.Users.AsNoTracking(),
                    emoji => emoji.AuthorId,
                    user => user.UserId,
                    (emoji, user) =>
                    new { emoji, user })
                .Select(x => new EmojiInfo(x.emoji, x.user,
                Context.Favorites.Count(y => y.EmojiId == x.emoji.Id))).ToList();

            List<int> userFavorites = new List<int>();

            if (User.Identity.IsAuthenticated)
            {
                ulong userId = ulong.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                userFavorites = Context.Favorites
                    .Where(x => x.UserId == userId)
                    .Select(x => x.EmojiId)
                    .ToList();
            }

            for (int i = 0; i < Context.Emoji.Count() + 4 / 4; i++)
            {
                EmojiRow r = new EmojiRow();

                var toAdd = e.Skip(i * 4).Take(4);

                if (!toAdd.Any())
                    break;

                foreach (EmojiInfo x in toAdd)
                    r.Add(x);

                rows.Add(r);
            }

            ViewData["Emojis"] = rows;
            ViewData["Tags"] = Tags;
            ViewData["UserFavorites"] = userFavorites;

            return View();
        }

        [Route("submit")]
        public IActionResult Submit()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("login");

            ViewData["Tags"] = Tags;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateRecaptcha]
        [Route("submit/submithandler")]
        public IActionResult SubmitHandler(string name, string url, string description)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("login");

            if (String.IsNullOrWhiteSpace(name)
                || String.IsNullOrWhiteSpace(name)
                || String.IsNullOrWhiteSpace(name))
            {
                TempData["Message"] = "Error: You left fields blank.";
                return RedirectToAction("submit");
            }

            string tags = HttpContext.Request.Form.ToList()
                .Find(x => x.Key == "tags").Value.ToString();

            string[] tagList = tags.Split(',');

            TempData["name"] = name;
            TempData["description"] = description;
            TempData["url"] = url;
            TempData["tagList"] = tagList;

            if (!ModelState.IsValid)
            {
                TempData["Message"] = "Error: Invalid CAPTCHA.";
                return RedirectToAction("submit");
            }

            Regex r = new Regex("^[a-zA-Z0-9-]*$");

            if (!r.IsMatch(name) || name.Length > 32 || name.Length < 3)
            {
                TempData["Message"] = "Error: Your emoji's name is invalid.";
                return RedirectToAction("submit");
            }

            if (description.Length > 128 || description.Length < 3)
            {
                TempData["Message"] = "Error: Your emoji's description is too long.";
                return RedirectToAction("submit");
            }


            if (tags.Trim().Length == 0)
            {
                TempData["Message"] = "Error: You must select one or more tags.";
                return RedirectToAction("submit");
            }

            if (!url.StartsWith("https://") && !url.StartsWith("http://"))
            {
                TempData["Message"] = "Error: You have provided an invalid image URL.";
                return RedirectToAction("submit");
            }

            foreach (string s in tagList)
                if (!Tags.Any(x => x == s))
                {
                    TempData["Message"] = "Error: Haha, nice try. Invalid tags.";
                    return RedirectToAction("submit");
                }

            Context.Emoji.Add(new Emoji
            {
                Name = name,
                AuthorId = ulong.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)),
                Description = description,
                ImageUrl = url,
                Extension = tagList.Contains("Animated") ? ".gif" : ".png",
                IsApproved = false,
                Submitted = DateTime.Now,
                Tags = String.Join(' ', tagList)
            });

            Context.SaveChanges();

            TempData["Message"] = "Success: Your submission has succeeded. Please wait for an approval.";
            return RedirectToAction("submit");
        }

        [Route("Approval")]
        public IActionResult Approval()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("login");

            if (!Admins.Contains(User.FindFirstValue(ClaimTypes.NameIdentifier)))
                return Unauthorized();

            List<EmojiRow> rows = new List<EmojiRow>();

            List<EmojiInfo> e = Context.Emoji
                .Where(x => !x.IsApproved)
                .OrderByDescending(x => x.Submitted)
                .Join(Context.Users.AsNoTracking(),
                    emoji => emoji.AuthorId,
                    user => user.UserId,
                    (emoji, user) =>
                    new EmojiInfo(emoji, user, 0)).ToList();

            for (int i = 0; i < Context.Emoji.Count() + 4 / 4; i++)
            {
                EmojiRow r = new EmojiRow();

                List<EmojiInfo> toAdd = e.Skip(i * 4).Take(4).ToList();
                if (!toAdd.Any())
                    break;

                foreach (EmojiInfo x in toAdd)
                    r.Add(x);

                rows.Add(r);
            }

            ViewData["Emojis"] = rows;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("ApproveEmoji")]
        public IActionResult ApproveEmoji(int id, string type)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("login");

            if (!Admins.Contains(User.FindFirstValue(ClaimTypes.NameIdentifier)))
                return Unauthorized();

            if (type != "approve" && type != "deny")
                return RedirectToAction("approval");

            Emoji e = Context.Emoji.FirstOrDefault(x => x.Id == id);
            if (e == null)
            {
                TempData["Message"] = "The action failed. Emoji was null.";
                return RedirectToAction("approval");
            }

            if (type == "approve")
            {
                e.IsApproved = true;
                Context.SaveChanges();

                if (!Directory.Exists($"wwwroot/emoji/{e.AuthorId}"))
                    Directory.CreateDirectory($"wwwroot/emoji/{e.AuthorId}");

                WebClient webClient = new WebClient();
                webClient.DownloadFileAsync(new Uri(e.ImageUrl),
                    $"wwwroot/emoji/{e.AuthorId}/{e.Id}{e.Extension}");

                TempData["Message"] = "APPROVE success.";
                return RedirectToAction("approval");
            }
            else
            {
                Context.Emoji.Remove(e);
                Context.SaveChanges();
                TempData["Message"] = "DENY success.";
                return RedirectToAction("approval");
            }
        }

        [Route("details/{id}")]
        public IActionResult Details(int id)
        {
            EmojiInfo emoji = Context.Emoji
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Join(Context.Users.AsNoTracking(),
                emote => emote.AuthorId,
                user => user.UserId,
                (emote, user) => new EmojiInfo(emote, user,
                Context.Favorites.Count(x => x.EmojiId == id)))
                .FirstOrDefault();

            if (emoji == null)
                return NotFound("That emoji does not seem to exist.");

            List<CommentInfo> comments = Context.Comments.AsNoTracking()
                .Where(x => x.EmojiId == id)
                .OrderByDescending(x => x.Submitted)
                .Join(Context.Users.AsNoTracking(),
                    comment => comment.AuthorId,
                    user => user.UserId,
                    (comment, user) =>
                    new CommentInfo(user, comment)).ToList();

            bool isFavorited = false;

            if (User.Identity.IsAuthenticated)
            {
                ulong userId = ulong.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                isFavorited = Context.Favorites.AsNoTracking().Any(x => x.UserId == userId && x.EmojiId == id);
            }

            ViewData["Emoji"] = emoji;
            ViewData["Comments"] = comments;
            ViewData["Context"] = Context;
            ViewData["Favorited"] = isFavorited;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("comment")]
        public IActionResult Comment(int id, string content)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("login");

            if (String.IsNullOrWhiteSpace(content))
            {
                TempData["Message"] = "Why are you submitting a blank comment?";
                return RedirectToAction("details", new { id = id });
            }

            if (!Context.Emoji.AsNoTracking().Any(x => x.Id == id))
                return RedirectToAction("index");

            if (content.Trim().Length > 1500)
            {
                TempData["Message"] = "Your comment must be less than 1500 characters long.";
                return RedirectToAction("details", new { id = id });
            }

            if (content.Trim().Length < 10)
            {
                TempData["Message"] = "Your comment must be more than 10 characters long.";
                return RedirectToAction("details", new { id = id });
            }

            Context.Comments.Add(new Comment()
            {
                EmojiId = id,
                Content = content,
                Submitted = DateTime.Now,
                AuthorId = ulong.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier))
            });

            Context.SaveChanges();

            return RedirectToAction("details", new { id = id });
        }

        [Route("login")]
        public IActionResult Login(string returnUrl = "/process-user")
        {
            return Challenge(new AuthenticationProperties() { RedirectUri = returnUrl });
        }

        [Route("logout")]
        public IActionResult Logout()
        {
            foreach (string c in Request.Cookies.Keys)
                Response.Cookies.Delete(c);

            return RedirectToAction("index");
        }

        [Route("favorites")]
        public IActionResult Favorites()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("login");

            List<EmojiInfo> e = Context.Emoji
                .AsNoTracking()
                .Where(x => x.IsApproved)
                .OrderByDescending(x => x.Submitted)
                .Join(Context.Users.AsNoTracking(),
                    emoji => emoji.AuthorId,
                    user => user.UserId,
                    (emoji, user) =>
                    new { emoji, user })
                .Join(Context.Favorites.AsNoTracking().Where(x => x.UserId == ulong.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier))),
                    emoji => emoji.emoji.Id,
                    fav => fav.EmojiId,
                    (emoji, fav) => new EmojiInfo(emoji.emoji, emoji.user, 0))
                    .ToList();

            List<EmojiRow> rows = new List<EmojiRow>();
            for (int i = 0; i < Context.Emoji.Count() + 4 / 4; i++)
            {
                EmojiRow r = new EmojiRow();

                var toAdd = e.Skip(i * 4).Take(4);

                if (!toAdd.Any())
                    break;

                foreach (EmojiInfo x in toAdd)
                    r.Add(x);

                rows.Add(r);
            }

            ViewData["Emojis"] = rows;

            return View();
        }

        [Route("myemoji")]
        public IActionResult Emojis()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("login");

            List<EmojiInfo> e = Context.Emoji
                .AsNoTracking()
                .Where(x => x.AuthorId == ulong.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)))
                .OrderByDescending(x => x.Submitted)
                .Join(Context.Users.AsNoTracking(),
                    emoji => emoji.AuthorId,
                    user => user.UserId,
                    (emoji, user) =>
                    new EmojiInfo(emoji, user, 0))
                    .ToList();

            List<EmojiRow> rows = new List<EmojiRow>();
            for (int i = 0; i < Context.Emoji.Count() + 4 / 4; i++)
            {
                EmojiRow r = new EmojiRow();

                var toAdd = e.Skip(i * 4).Take(4);

                if (!toAdd.Any())
                    break;

                foreach (EmojiInfo x in toAdd)
                    r.Add(x);

                rows.Add(r);
            }

            ViewData["Emojis"] = rows;

            return View();
        }

        [HttpPost]
        [Route("favorite")]
        public JsonResult FavoriteHandler(int emojiId)
        {
            FavoriteResult unsuccessful = new FavoriteResult(false, FavoriteAction.NONE);

            if (!User.Identity.IsAuthenticated)
                return Json(unsuccessful);

            if (!Context.Emoji.Any(x => x.Id == emojiId))
                return Json(unsuccessful);

            ulong userId = ulong.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            Favorite fav = Context.Favorites
                .FirstOrDefault(x => x.EmojiId == emojiId && x.UserId == userId);

            FavoriteAction action = FavoriteAction.NONE;

            if (fav != null)
            {
                action = FavoriteAction.UNFAVORITE;
                Context.Favorites.Remove(fav);
            }
            else
            {
                action = FavoriteAction.FAVORITE;
                Context.Favorites.Add(new Favorite(userId, emojiId));
            }

            Context.SaveChanges();

            return Json(new FavoriteResult(true, action));
        }

        [Route("process-user")]
        public IActionResult ProcessUser()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("index");

            if (!Context.Users.Any(x => x.UserId == ulong.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier))))
            {
                Context.Users.Add(new User()
                {
                    UserId = ulong.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)),
                    Username = User.FindFirstValue(ClaimTypes.Name),
                    Discriminator = short.Parse(User.FindFirstValue("urn:discord:discriminator")),
                    Avatar = User.FindFirstValue("urn:discord:avatar")
                });
                Context.SaveChanges();
            }
            else
            {
                User user = Context.Users
                    .FirstOrDefault(x => x.UserId == ulong.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)));

                if (user == null)
                    return RedirectToAction("index");

                if (user.Username != User.FindFirstValue(ClaimTypes.Name))
                    user.Username = User.FindFirstValue(ClaimTypes.Name);

                if (user.Discriminator != short.Parse(User.FindFirstValue("urn:discord:discriminator")))
                    user.Discriminator = short.Parse(User.FindFirstValue("urn:discord:discriminator"));

                if (user.Avatar != User.FindFirstValue("urn:discord:avatar"))
                    user.Avatar = User.FindFirstValue("urn:discord:avatar");

                Context.SaveChanges();
            }
            return RedirectToAction("index");
        }

        [Route("faq")]
        public IActionResult Faq()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
