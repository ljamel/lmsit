using System.ComponentModel.DataAnnotations;

namespace CrudDemo.Models;

/// <summary>
/// Stocke les credentials Rocket.Chat pour permettre la connexion automatique
/// </summary>
public class RocketChatCredential
{
    [Key]
    public string UserId { get; set; } = string.Empty;
    
    public string RocketChatUsername { get; set; } = string.Empty;
    
    public string RocketChatUserId { get; set; } = string.Empty;
    
    /// <summary>
    /// Mot de passe chiffré
    /// </summary>
    public string EncryptedPassword { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
