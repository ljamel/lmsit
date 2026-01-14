using System.Text.Json;

namespace CrudDemo.Services;

public class RocketChatService : IRocketChatService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RocketChatService> _logger;

    public RocketChatService(HttpClient httpClient, IConfiguration configuration, ILogger<RocketChatService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        // Configuration du client HTTP
        var baseUrl = _configuration["RocketChat:BaseUrl"] ?? "http://localhost:3000";
        var adminToken = _configuration["RocketChat:AdminAuthToken"] ?? "JETON_ADMIN";
        var adminUserId = _configuration["RocketChat:AdminUserId"] ?? "ID_ADMIN";

        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Add("X-Auth-Token", adminToken);
        _httpClient.DefaultRequestHeaders.Add("X-User-Id", adminUserId);
    }

    public async Task<string> CreateUserAndGetLoginUrlAsync(string name, string username, string email)
    {
        try
        {
            var result = await CreateUserAsync(name, username, email, new[] { "user" });
            return result.LoginUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création de l'utilisateur Rocket.Chat: {Username}", username);
            throw;
        }
    }

    public async Task<RocketChatUserResult> CreateUserAsync(string name, string username, string email, string[] roles)
    {
        try
        {
            // 1️⃣ Vérifier si l'utilisateur existe déjà
            _logger.LogInformation("Vérification de l'existence de l'utilisateur: {Username}", username);
            
            var existingUser = await GetUserByUsernameAsync(username);
            
            if (existingUser != null)
            {
                // L'utilisateur existe, on retourne juste un lien vers le chat
                _logger.LogInformation("Utilisateur existant trouvé: {UserId}, redirection vers le chat", existingUser.UserId);
                var serverUrl = _configuration["RocketChat:ServerUrl"] ?? _httpClient.BaseAddress?.ToString().TrimEnd('/');
                return new RocketChatUserResult
                {
                    UserId = existingUser.UserId,
                    AuthToken = string.Empty,
                    LoginUrl = $"{serverUrl}/home"
                };
            }

            // 2️⃣ L'utilisateur n'existe pas, on le crée
            // Générer un mot de passe aléatoire
            var randomPassword = GenerateRandomPassword();
            
            var payload = new
            {
                name,
                username,
                email,
                password = randomPassword,
                roles,
                requirePasswordChange = false
            };

            _logger.LogInformation("Création du nouvel utilisateur Rocket.Chat: {Username}", username);

            var response = await _httpClient.PostAsJsonAsync("/api/v1/users.create", payload);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Échec de la création de l'utilisateur Rocket.Chat. Status: {Status}, Erreur: {Error}", 
                    response.StatusCode, errorContent);
                
                // Si l'utilisateur existe déjà (erreur commune), on essaie de le connecter
                if (errorContent.Contains("Username is already in use") || errorContent.Contains("Email already exists"))
                {
                    _logger.LogInformation("L'utilisateur existe déjà selon l'erreur, redirection vers le chat...");
                    var existingUserRetry = await GetUserByUsernameAsync(username);
                    if (existingUserRetry != null)
                    {
                        // Pour un utilisateur existant, retourner juste un lien vers le chat
                        var serverUrl = _configuration["RocketChat:ServerUrl"] ?? _httpClient.BaseAddress?.ToString().TrimEnd('/');
                        return new RocketChatUserResult
                        {
                            UserId = existingUserRetry.UserId,
                            AuthToken = string.Empty,
                            LoginUrl = $"{serverUrl}/home"
                        };
                    }
                }
                
                throw new HttpRequestException($"Échec de la création de l'utilisateur: {response.StatusCode} - {errorContent}");
            }

            var resultContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Réponse de création utilisateur: {Response}", resultContent);
            
            var result = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(resultContent);

            // 3️⃣ L'utilisateur est créé, maintenant on se connecte avec son mot de passe
            if (result.TryGetProperty("user", out var userElement))
            {
                var userId = userElement.GetProperty("_id").GetString() ?? string.Empty;
                var createdUsername = userElement.GetProperty("username").GetString() ?? username;
                
                _logger.LogInformation("Utilisateur créé, connexion avec le mot de passe...");
                
                // Se connecter avec le mot de passe qu'on vient de créer
                return await LoginUserAsync(createdUsername, randomPassword);
            }
            else
            {
                _logger.LogError("Structure de réponse inattendue: {Response}", resultContent);
                throw new HttpRequestException("Structure de réponse inattendue de l'API Rocket.Chat");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création/connexion de l'utilisateur Rocket.Chat: {Username}", username);
            throw;
        }
    }

    /// <summary>
    /// Vérifie si un utilisateur existe par son username
    /// </summary>
    private async Task<ExistingUserInfo?> GetUserByUsernameAsync(string username)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/users.info?username={username}");
            
            if (!response.IsSuccessStatusCode)
            {
                // Utilisateur n'existe pas
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            var userElement = result.GetProperty("user");
            
            return new ExistingUserInfo
            {
                UserId = userElement.GetProperty("_id").GetString() ?? string.Empty,
                Username = userElement.GetProperty("username").GetString() ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erreur lors de la vérification de l'utilisateur: {Username}", username);
            return null;
        }
    }

    /// <summary>
    /// Se connecte à Rocket.Chat avec username et password
    /// </summary>
    private async Task<RocketChatUserResult> LoginUserAsync(string username, string password)
    {
        try
        {
            // Créer un nouveau HttpClient sans les headers admin pour la connexion utilisateur
            using var loginClient = new HttpClient();
            loginClient.BaseAddress = _httpClient.BaseAddress;
            
            var loginPayload = new
            {
                user = username,
                password = password
            };

            _logger.LogInformation("Connexion de l'utilisateur: {Username}", username);

            var response = await loginClient.PostAsJsonAsync("/api/v1/login", loginPayload);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Échec de connexion. Status: {Status}, Erreur: {Error}", 
                    response.StatusCode, errorContent);
                throw new HttpRequestException($"Échec de connexion: {response.StatusCode}");
            }

            var resultContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Connexion réussie");
            
            var result = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(resultContent);
            var data = result.GetProperty("data");
            var authToken = data.GetProperty("authToken").GetString() ?? string.Empty;
            var userId = data.GetProperty("userId").GetString() ?? string.Empty;

            var serverUrl = _configuration["RocketChat:ServerUrl"] ?? _httpClient.BaseAddress?.ToString().TrimEnd('/');
            var redirectUrl = $"{serverUrl}/home?resumeToken={authToken}";

            return new RocketChatUserResult
            {
                UserId = userId,
                AuthToken = authToken,
                LoginUrl = redirectUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la connexion: {Username}", username);
            throw;
        }
    }

    /// <summary>
    /// Génère un token de connexion pour un utilisateur existant
    /// </summary>
    private async Task<RocketChatUserResult> GenerateLoginTokenAsync(string userId, string username)
    {
        try
        {
            // Essayer avec username au lieu de userId
            var payload = new { username };
            
            _logger.LogInformation("Génération de token pour: {Username} (userId: {UserId})", username, userId);
            
            var response = await _httpClient.PostAsJsonAsync("/api/v1/users.createToken", payload);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Échec de génération de token. Status: {Status}, Erreur: {Error}", 
                    response.StatusCode, errorContent);
                throw new HttpRequestException($"Échec de génération de token: {response.StatusCode} - {errorContent}");
            }
            
            var resultContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Réponse createToken: {Response}", resultContent);
            
            var result = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(resultContent);
            var data = result.GetProperty("data");
            var authToken = data.GetProperty("authToken").GetString() ?? string.Empty;
            var returnedUserId = data.GetProperty("userId").GetString() ?? userId;

            var serverUrl = _configuration["RocketChat:ServerUrl"] ?? _httpClient.BaseAddress?.ToString().TrimEnd('/');
            var redirectUrl = $"{serverUrl}/home?resumeToken={authToken}";

            _logger.LogInformation("Token de connexion généré pour l'utilisateur: {Username}", username);

            return new RocketChatUserResult
            {
                UserId = returnedUserId,
                AuthToken = authToken,
                LoginUrl = redirectUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la génération du token pour: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Génère un mot de passe aléatoire sécurisé
    /// </summary>
    private static string GenerateRandomPassword()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 16)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}

/// <summary>
/// Informations sur un utilisateur existant
/// </summary>
internal class ExistingUserInfo
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
}
