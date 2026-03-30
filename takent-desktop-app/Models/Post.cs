using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace takent_desktop_app.Models
{
    public class PostAuthor
    {
        [JsonPropertyName("username")]
        public string Username { get; set; } = "";

        [JsonPropertyName("firstName")]
        public string FirstName { get; set; } = "";

        [JsonPropertyName("lastName")]
        public string LastName { get; set; } = "";
    }

    public class Post
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("content")]
        public string Content { get; set; } = "";

        [JsonPropertyName("hashtags")]
        public List<string> Hashtags { get; set; } = new();

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("user")]
        public PostAuthor? Author { get; set; }

        public string AuthorDisplay =>
            Author != null ? $"{Author.FirstName} {Author.LastName}".Trim() : "Usuario";

        public string UsernameDisplay =>
            Author != null ? $"@{Author.Username}" : "";

        public string DateDisplay =>
            CreatedAt.ToString("d MMM, HH:mm");

        public string HashtagsDisplay =>
            Hashtags.Count > 0 ? string.Join("  ", Hashtags.ConvertAll(h => $"#{h}")) : "";
    }
}