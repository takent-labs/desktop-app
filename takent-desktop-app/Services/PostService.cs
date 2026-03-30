using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using takent_desktop_app.Models;

namespace takent_desktop_app.Services
{
    public class PostResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public List<Post> Posts { get; set; } = new();
        public Post? CreatedPost { get; set; }
    }

    public class PostService
    {
        private static readonly HttpClient _http = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:3001"),
            Timeout = TimeSpan.FromSeconds(15)
        };

        private readonly AuthService _auth;

        public PostService(AuthService auth)
        {
            _auth = auth;
        }

        public async Task<PostResult> GetPostsAsync()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/posts");
                AttachToken(request);

                var response = await _http.SendAsync(request);
                var raw = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return Fail($"Error {(int)response.StatusCode} al cargar posts.");

                var posts = JsonSerializer.Deserialize<List<Post>>(raw)
                            ?? new List<Post>();

                return new PostResult { Success = true, Posts = posts };
            }
            catch (TaskCanceledException)
            {
                return Fail("Tiempo de espera agotado.");
            }
            catch (Exception ex)
            {
                return Fail($"Error de red: {ex.Message}");
            }
        }

        public async Task<PostResult> CreatePostAsync(string content)
        {
            try
            {
                var hashtags = ExtractHashtags(content);

                var body = JsonSerializer.Serialize(new
                {
                    content,
                    hashtags
                });

                var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/posts")
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                };
                AttachToken(request);

                var response = await _http.SendAsync(request);
                var raw = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    try
                    {
                        var err = JsonSerializer.Deserialize<JsonElement>(raw);
                        var msg = err.TryGetProperty("message", out var m)
                            ? m.GetString() ?? "Error del servidor."
                            : $"Error {(int)response.StatusCode}";
                        return Fail(msg);
                    }
                    catch { return Fail($"Error {(int)response.StatusCode}"); }
                }

                var created = JsonSerializer.Deserialize<Post>(raw);
                return new PostResult { Success = true, CreatedPost = created };
            }
            catch (TaskCanceledException)
            {
                return Fail("Tiempo de espera agotado.");
            }
            catch (Exception ex)
            {
                return Fail($"Error de red: {ex.Message}");
            }
        }

        private void AttachToken(HttpRequestMessage request)
        {
            var token = _auth.GetStoredToken();
            if (!string.IsNullOrEmpty(token))
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
        }

        private static List<string> ExtractHashtags(string content)
        {
            var tags = new List<string>();
            var words = content.Split(' ', '\n', '\r');
            foreach (var word in words)
            {
                var clean = word.Trim();
                if (clean.StartsWith("#") && clean.Length > 1)
                    tags.Add(clean.Substring(1));
            }
            return tags;
        }

        private static PostResult Fail(string msg) =>
            new PostResult { Success = false, ErrorMessage = msg };
    }
}