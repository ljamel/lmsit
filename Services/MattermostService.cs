using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CrudDemo.Services;

public class MattermostService
{
    private readonly HttpClient _http;
    private readonly string _teamName;

    public MattermostService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _http.BaseAddress = new Uri(config["Mattermost:BaseUrl"] ?? "https://mattermost.example.com");
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", config["Mattermost:ApiToken"]);

        _teamName = config["Mattermost:TeamName"] ?? "default";
    }

    // 1️⃣ Créer ou récupérer un user
    public async Task<string> EnsureUserAsync(string email, string username, string firstName, string lastName)
    {
        var payload = new
        {
            email,
            username,
            password = "Temp#" + Guid.NewGuid().ToString("N").Substring(0, 10),
            first_name = firstName,
            last_name = lastName
        };

        var res = await _http.PostAsync(
            "/api/v4/users",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

        if (res.IsSuccessStatusCode)
        {
            var json = await res.Content.ReadAsStringAsync();
            return JsonDocument.Parse(json).RootElement.GetProperty("id").GetString() ?? "";
        }

        // user existe déjà → récupérer par email
        var byEmail = await _http.GetAsync($"/api/v4/users/email/{email}");
        var userJson = await byEmail.Content.ReadAsStringAsync();
        return JsonDocument.Parse(userJson).RootElement.GetProperty("id").GetString() ?? "";
    }

    // 2️⃣ Ajouter à l’équipe
    public async Task AddUserToTeamAsync(string userId)
    {
        var teamRes = await _http.GetAsync($"/api/v4/teams/name/{_teamName}");
        var teamJson = await teamRes.Content.ReadAsStringAsync();
        var teamId = JsonDocument.Parse(teamJson).RootElement.GetProperty("id").GetString();

        var body = new
        {
            team_id = teamId,
            user_id = userId
        };

        await _http.PostAsync(
            $"/api/v4/teams/{teamId}/members",
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
    }

    // 3️⃣ Ajouter à un canal (exercice)
    public async Task AddUserToChannelAsync(string channelName, string userId)
    {
        var channelRes = await _http.GetAsync($"/api/v4/channels/name/{_teamName}/{channelName}");
        var channelJson = await channelRes.Content.ReadAsStringAsync();
        var channelId = JsonDocument.Parse(channelJson).RootElement.GetProperty("id").GetString();

        var body = new { user_id = userId };

        await _http.PostAsync(
            $"/api/v4/channels/{channelId}/members",
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
    }

    // 4️⃣ Désactiver un utilisateur Mattermost (quand l'abonnement expire)
    public async Task<bool> DeactivateUserAsync(string userId)
    {
        try
        {
            var payload = new { active = false };
            var res = await _http.PutAsync(
                $"/api/v4/users/{userId}/active",
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

            return res.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // 5️⃣ Activer un utilisateur Mattermost (quand l'abonnement est renouvelé)
    public async Task<bool> ActivateUserAsync(string userId)
    {
        try
        {
            var payload = new { active = true };
            var res = await _http.PutAsync(
                $"/api/v4/users/{userId}/active",
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

            return res.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // 6️⃣ Récupérer l'ID d'un utilisateur Mattermost par email
    public async Task<string?> GetUserIdByEmailAsync(string email)
    {
        try
        {
            var byEmail = await _http.GetAsync($"/api/v4/users/email/{email}");
            if (!byEmail.IsSuccessStatusCode)
                return null;

            var userJson = await byEmail.Content.ReadAsStringAsync();
            return JsonDocument.Parse(userJson).RootElement.GetProperty("id").GetString();
        }
        catch
        {
            return null;
        }
    }

    // 7️⃣ Envoyer un email de réinitialisation de mot de passe
    public async Task<bool> SendPasswordResetEmailAsync(string email)
    {
        try
        {
            var payload = new { email };
            var res = await _http.PostAsync(
                "/api/v4/users/password/reset/send",
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

            return res.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // 8️⃣ Obtenir l'URL de base Mattermost
    public string GetMattermostUrl()
    {
        return _http.BaseAddress?.ToString().TrimEnd('/') ?? "";
    }
}
