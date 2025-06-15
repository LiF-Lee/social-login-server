namespace social_login_server.Services;

public class TurnstileValidator(HttpClient http, IConfiguration cfg)
{
    private readonly string? _secret = cfg["Turnstile:Secret"];

    public async Task<bool> ValidateAsync(string token, string? ip = null)
    {
        if (string.IsNullOrEmpty(_secret))
        {
            throw new InvalidOperationException("Turnstile secret key is not configured.");
        }

        var response = await http.PostAsJsonAsync("https://challenges.cloudflare.com/turnstile/v0/siteverify", new
        {
            secret = _secret,
            response = token,
            remoteip = ip
        });

        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var result = await response.Content.ReadFromJsonAsync<TurnstileResponse>();
        return result?.Success == true;
    }

    private class TurnstileResponse
    {
        public bool Success { get; set; }
        public List<string>? ErrorCodes { get; set; }
    }
}