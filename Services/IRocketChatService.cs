namespace CrudDemo.Services;

public interface IRocketChatService
{
    /// <summary>
    /// Crée un utilisateur dans Rocket.Chat et retourne l'URL de connexion automatique
    /// </summary>
    Task<string> CreateUserAndGetLoginUrlAsync(string name, string username, string email);
    
    /// <summary>
    /// Crée un utilisateur dans Rocket.Chat avec des rôles personnalisés
    /// </summary>
    Task<RocketChatUserResult> CreateUserAsync(string name, string username, string email, string[] roles);
}

public class RocketChatUserResult
{
    public string UserId { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string LoginUrl { get; set; } = string.Empty;
}
