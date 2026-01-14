# Intégration Rocket.Chat

Ce projet intègre Rocket.Chat pour la création automatique d'utilisateurs et la connexion automatique.

## Configuration

### 1. Configurer appsettings.json

Mettez à jour les valeurs dans `appsettings.json` :

```json
"RocketChat": {
  "BaseUrl": "http://localhost:3000",           // URL de l'API Rocket.Chat
  "ServerUrl": "http://IP_DU_SERVEUR:3000",     // URL publique du serveur
  "AdminAuthToken": "VOTRE_JETON_ADMIN",        // Token d'authentification admin
  "AdminUserId": "VOTRE_ID_ADMIN"               // ID de l'utilisateur admin
}
```

### 2. Obtenir les tokens administrateur

Pour obtenir votre token et user ID admin :

1. Connectez-vous à Rocket.Chat en tant qu'administrateur
2. Allez dans **Mon compte** → **Tokens personnels**
3. Créez un nouveau token ou utilisez un existant
4. Notez le `authToken` et votre `userId`

Alternativement, via l'API :

```bash
curl -X POST http://localhost:3000/api/v1/login \
  -H "Content-Type: application/json" \
  -d '{
    "user": "admin",
    "password": "votre_mot_de_passe"
  }'
```

## Utilisation

### Via le Service (recommandé)

Injectez `IRocketChatService` dans vos contrôleurs :

```csharp
public class MonController : Controller
{
    private readonly IRocketChatService _rocketChatService;
    
    public MonController(IRocketChatService rocketChatService)
    {
        _rocketChatService = rocketChatService;
    }
    
    public async Task<IActionResult> CreerUtilisateur()
    {
        // Créer un utilisateur et obtenir l'URL de connexion
        var loginUrl = await _rocketChatService.CreateUserAndGetLoginUrlAsync(
            "Jean Dupont", 
            "jean.dupont", 
            "jean@example.com"
        );
        
        // Rediriger vers Rocket.Chat avec connexion automatique
        return Redirect(loginUrl);
    }
    
    // Ou avec plus de contrôle
    public async Task<IActionResult> CreerAvecRoles()
    {
        var result = await _rocketChatService.CreateUserAsync(
            "Admin User",
            "admin.user",
            "admin@example.com",
            new[] { "user", "admin" }
        );
        
        return Ok(new {
            userId = result.UserId,
            authToken = result.AuthToken,
            loginUrl = result.LoginUrl
        });
    }
}
```

### Via le Contrôleur de Test

Le projet inclut un contrôleur de test accessible via :

#### 1. Interface Web de Test
```
GET http://localhost:5000/RocketChat/TestCreate
```

Cette page offre un formulaire pour créer des utilisateurs de test.

#### 2. API de Redirection Automatique
```
GET http://localhost:5000/RocketChat/CreateAndRedirect?name=Jean&username=jean123&email=jean@example.com
```

Cette route crée l'utilisateur et redirige immédiatement vers Rocket.Chat.

#### 3. API JSON
```
POST http://localhost:5000/RocketChat/CreateUser
Content-Type: application/json

{
  "name": "Jean Dupont",
  "username": "jean.dupont",
  "email": "jean@example.com",
  "roles": ["user"]
}
```

Réponse :
```json
{
  "success": true,
  "userId": "abc123...",
  "loginUrl": "http://IP_DU_SERVEUR:3000/home?resumeToken=xyz..."
}
```

## Cas d'Usage

### 1. Connexion Automatique après Inscription

```csharp
[HttpPost]
public async Task<IActionResult> Register(RegisterViewModel model)
{
    if (ModelState.IsValid)
    {
        // Créer l'utilisateur dans votre système
        var result = await _userManager.CreateAsync(user, model.Password);
        
        if (result.Succeeded)
        {
            // Créer l'utilisateur dans Rocket.Chat
            var chatUrl = await _rocketChatService.CreateUserAndGetLoginUrlAsync(
                model.Name,
                model.Username,
                model.Email
            );
            
            // Rediriger vers le chat
            return Redirect(chatUrl);
        }
    }
    return View(model);
}
```

### 2. Bouton "Accéder au Support" pour un Utilisateur Connecté

```csharp
public async Task<IActionResult> AccederAuSupport()
{
    var user = await _userManager.GetUserAsync(User);
    
    try
    {
        var loginUrl = await _rocketChatService.CreateUserAndGetLoginUrlAsync(
            user.UserName,
            user.UserName,
            user.Email
        );
        
        return Redirect(loginUrl);
    }
    catch (HttpRequestException)
    {
        // L'utilisateur existe peut-être déjà
        TempData["Error"] = "Impossible de créer l'utilisateur. Contactez l'administrateur.";
        return RedirectToAction("Index", "Home");
    }
}
```

### 3. Création en Lot d'Utilisateurs

```csharp
public async Task<IActionResult> CreerUtilisateursEnLot()
{
    var utilisateurs = new[]
    {
        new { Name = "User 1", Username = "user1", Email = "user1@example.com" },
        new { Name = "User 2", Username = "user2", Email = "user2@example.com" },
        // ...
    };
    
    var resultats = new List<RocketChatUserResult>();
    
    foreach (var u in utilisateurs)
    {
        try
        {
            var result = await _rocketChatService.CreateUserAsync(
                u.Name, u.Username, u.Email, new[] { "user" }
            );
            resultats.Add(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur pour {Username}", u.Username);
        }
    }
    
    return Ok(resultats);
}
```

## Gestion des Erreurs

Le service gère automatiquement les erreurs courantes :

- **Utilisateur déjà existant** : Rocket.Chat retourne une erreur HTTP 400
- **Token invalide** : Erreur HTTP 401 Unauthorized
- **Serveur inaccessible** : HttpRequestException

Exemple de gestion :

```csharp
try
{
    var result = await _rocketChatService.CreateUserAsync(name, username, email, roles);
    return Ok(result);
}
catch (HttpRequestException ex)
{
    _logger.LogError(ex, "Erreur API Rocket.Chat");
    return StatusCode(503, "Service temporairement indisponible");
}
catch (Exception ex)
{
    _logger.LogError(ex, "Erreur inattendue");
    return StatusCode(500, "Erreur interne");
}
```

## Sécurité

⚠️ **Important** :

1. **Jamais en production** : Ne stockez jamais les tokens admin dans `appsettings.json` en production
2. **Variables d'environnement** : Utilisez des variables d'environnement ou Azure Key Vault
3. **Rotation des tokens** : Changez régulièrement les tokens administrateur
4. **HTTPS** : Utilisez toujours HTTPS en production

Configuration en production :

```bash
export RocketChat__AdminAuthToken="votre_token"
export RocketChat__AdminUserId="votre_user_id"
```

## Débogage

Pour tester la connexion à l'API :

```bash
curl -X POST http://localhost:3000/api/v1/users.create \
  -H "X-Auth-Token: VOTRE_TOKEN" \
  -H "X-User-Id: VOTRE_USER_ID" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test User",
    "username": "testuser",
    "email": "test@example.com",
    "password": "TestPass123!",
    "roles": ["user"]
  }'
```

## Documentation de l'API Rocket.Chat

- [Documentation officielle](https://developer.rocket.chat/reference/api)
- [Users API](https://developer.rocket.chat/reference/api/rest-api/endpoints/user-management/users-endpoints)
