using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Security.Credentials;

namespace takent_desktop_app.Services
{
    public class AuthResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class AuthService
    {
        private static readonly HttpClient _http = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:3001"),
            Timeout = TimeSpan.FromSeconds(15)
        };

        private const string VaultResource = "MiRedSocial";
        private const string VaultUsername = "session_user";

        public async Task<AuthResult> LoginAsync(string email, string password)
        {
            try
            {
                var body = JsonSerializer.Serialize(new { email, password });
                var content = new StringContent(body, Encoding.UTF8, "application/json");

                var response = await _http.PostAsync("/api/v1/auth/sign-in", content);
                return await HandleResponseAsync(response);
            }
            catch (TaskCanceledException)
            {
                return Fail("La solicitud tardó demasiado. Comprueba tu conexión.");
            }
            catch (Exception ex)
            {
                return Fail($"Error de red: {ex.Message}");
            }
        }

        public async Task<AuthResult> RegisterAsync(string username, string email, string password)
        {
            try
            {
                var body = JsonSerializer.Serialize(new { username, email, password, firstName = "", lastName = "" });
                var content = new StringContent(body, Encoding.UTF8, "application/json");

                var response = await _http.PostAsync("/api/v1/auth/sign-up", content);
                return await HandleResponseAsync(response);
            }
            catch (TaskCanceledException)
            {
                return Fail("La solicitud tardó demasiado. Comprueba tu conexión.");
            }
            catch (Exception ex)
            {
                return Fail($"Error de red: {ex.Message}");
            }
        }

        private async Task<AuthResult> HandleResponseAsync(HttpResponseMessage response)
        {
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
                catch
                {
                    return Fail($"Error {(int)response.StatusCode}: {response.ReasonPhrase}");
                }
            }

            var json = JsonSerializer.Deserialize<JsonElement>(raw);
            if (json.TryGetProperty("accessToken", out var tokenEl))
            {
                var token = tokenEl.GetString();
                if (!string.IsNullOrEmpty(token))
                {
                    SaveToken(token);
                    return new AuthResult { Success = true };
                }
            }

            return Fail("La API no devolvió un token válido.");
        }

        private static void SaveToken(string token)
        {
            var vault = new PasswordVault();

            try
            {
                var old = vault.Retrieve(VaultResource, VaultUsername);
                vault.Remove(old);
            }
            catch { /* No había sesión previa, no pasa nada */ }

            vault.Add(new PasswordCredential(VaultResource, VaultUsername, token));
        }

        public string? GetStoredToken()
        {
            try
            {
                var vault = new PasswordVault();
                var cred = vault.Retrieve(VaultResource, VaultUsername);
                cred.RetrievePassword();
                return cred.Password;
            }
            catch
            {
                return null; 
            }
        }

        public void Logout()
        {
            try
            {
                var vault = new PasswordVault();
                var cred = vault.Retrieve(VaultResource, VaultUsername);
                vault.Remove(cred);
            }
            catch { /* Ya estaba vacío */ }
        }

        private static AuthResult Fail(string msg) =>
            new AuthResult { Success = false, ErrorMessage = msg };
    }
}
