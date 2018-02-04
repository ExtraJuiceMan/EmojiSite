using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EmojiSite.Models
{
    public class EmojiContext : DbContext
    {
        public EmojiContext(DbContextOptions<EmojiContext> options): base(options) { }

        public EmojiContext() { }

        public DbSet<Emoji> Emoji { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Favorite> Favorites { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Favorite>()
                .HasKey(f => new { f.UserId, f.EmojiId });
        }
    }

    public class Emoji
    {
        [Key]
        public int Id { get; set; }
        public ulong AuthorId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Tags { get; set; }
        public string Extension { get; set; }
        public string ImageUrl { get; set; }
        public DateTime Submitted { get; set; }
        public bool IsApproved { get; set; }

        public Emoji(string name, int id, string description, string tags, ulong authorId, DateTime submitted, string extension, bool isApproved, string imageUrl)
        {
            Name = name;
            Id = id;
            Description = description;
            Tags = tags;
            AuthorId = authorId;
            Submitted = submitted;
            Extension = extension;
            IsApproved = isApproved;
            ImageUrl = imageUrl;
        }

        public Emoji() { }

        public string GenerateSlug() => $"/emoji/{AuthorId}/{Id}{Extension}";

        public string[] TagsToArray()
        {
            string[] tags = Tags.Split(" ");
            for (int i = 0; i < tags.Length; i++)
                tags[i] = tags[i].Replace("_", " ");

            return tags;
        }
    }
}
