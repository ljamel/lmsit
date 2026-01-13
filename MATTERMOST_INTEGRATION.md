# Guide d'intégration Mattermost avec vérification d'abonnement

## Modifications effectuées

### 1. Service Mattermost enrichi
**Fichier**: `Services/MattermostService.cs`

Ajout de nouvelles méthodes :
- `DeactivateUserAsync(string userId)` - Désactive un utilisateur Mattermost
- `ActivateUserAsync(string userId)` - Réactive un utilisateur Mattermost  
- `GetUserIdByEmailAsync(string email)` - Récupère l'ID Mattermost par email

### 2. Modèle Subscription mis à jour
**Fichier**: `Models/Payment.cs`

Nouveaux champs ajoutés :
```csharp
public string? MattermostUserId { get; set; }           // ID utilisateur Mattermost
public DateTime? MattermostCreatedAt { get; set; }      // Date création compte
```

### 3. Action d'activation Mattermost
**Fichier**: `Controllers/AccountController.cs`

Nouvelle route : `/Account/ActivateMattermost`

Fonctionnalités :
- ✅ Vérifie l'abonnement actif avant création
- ✅ Crée le compte Mattermost si inexistant
- ✅ Réactive le compte si déjà existant
- ✅ Stocke l'ID Mattermost dans la base de données
- ✅ Affiche des messages de confirmation/erreur

### 4. Interface utilisateur
**Fichier**: `Views/Shared/_MemberLayout.cshtml`

Ajout d'une barre de navigation avec :
- Lien vers les cours
- **Bouton "Activer Mattermost"** (vert, visible)
- Bouton de déconnexion
- Affichage des messages de succès/erreur/warning

### 5. Migration base de données
**Fichiers** : 
- `Migrations/20260113000000_AddMattermostFieldsToSubscription.cs`
- `add-mattermost-fields.sql`

## Installation

### Étape 1 : Appliquer la migration SQL

Connectez-vous à votre conteneur MySQL Docker :
```bash
docker exec -i <nom_conteneur_mysql> mysql -u root -p<mot_de_passe> <nom_base_de_donnees> < add-mattermost-fields.sql
```

Ou via votre client MySQL favori, exécutez :
```sql
ALTER TABLE `Subscriptions` 
ADD COLUMN `MattermostUserId` longtext NULL,
ADD COLUMN `MattermostCreatedAt` datetime(6) NULL;
```

### Étape 2 : Configurer Mattermost dans appsettings.json

Assurez-vous que la configuration Mattermost est présente :
```json
{
  "Mattermost": {
    "BaseUrl": "https://votre-instance-mattermost.com",
    "ApiToken": "votre-token-api",
    "TeamName": "nom-de-votre-equipe"
  }
}
```

### Étape 3 : Redémarrer l'application

```bash
dotnet run
```

## Utilisation

### Pour les utilisateurs

1. **Se connecter** à l'espace membre
2. **Vérifier l'abonnement actif** (sinon redirection vers paiement)
3. **Cliquer sur "Activer Mattermost"** dans la barre de navigation
4. Le système va :
   - Créer automatiquement le compte Mattermost
   - Ajouter l'utilisateur à l'équipe configurée
   - Stocker l'ID Mattermost dans la base de données
   - Afficher un message de confirmation

### Gestion des abonnements expirés

#### Option 1 : Désactivation manuelle via webhook Stripe

Créez un endpoint webhook dans `PaymentController.cs` :

```csharp
[HttpPost]
[AllowAnonymous]
public async Task<IActionResult> StripeWebhook()
{
    var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
    
    try
    {
        var stripeEvent = EventUtility.ParseEvent(json);
        
        if (stripeEvent.Type == Events.CustomerSubscriptionDeleted ||
            stripeEvent.Type == Events.CustomerSubscriptionUpdated)
        {
            var subscription = stripeEvent.Data.Object as Stripe.Subscription;
            
            if (subscription?.Status == "canceled" || subscription?.Status == "past_due")
            {
                var dbSubscription = await _context.Subscriptions
                    .FirstOrDefaultAsync(s => s.StripeSubscriptionId == subscription.Id);
                    
                if (dbSubscription != null && !string.IsNullOrEmpty(dbSubscription.MattermostUserId))
                {
                    // Désactiver l'accès Mattermost
                    await _mattermostService.DeactivateUserAsync(dbSubscription.MattermostUserId);
                    
                    dbSubscription.IsActive = false;
                    dbSubscription.Status = subscription.Status;
                    dbSubscription.CanceledAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }
        }
        
        return Ok();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Erreur webhook Stripe");
        return BadRequest();
    }
}
```

#### Option 2 : Script de désactivation automatique

Créez un job planifié (via cron ou Hangfire) :

```csharp
public async Task DeactivateExpiredMattermostAccounts()
{
    var expiredSubscriptions = await _context.Subscriptions
        .Where(s => !s.IsActive 
            && s.Status != "active" 
            && !string.IsNullOrEmpty(s.MattermostUserId))
        .ToListAsync();
    
    foreach (var subscription in expiredSubscriptions)
    {
        await _mattermostService.DeactivateUserAsync(subscription.MattermostUserId);
        _logger.LogInformation($"Compte Mattermost désactivé pour {subscription.UserId}");
    }
}
```

## Sécurité

✅ **Vérification d'abonnement** : Aucun accès Mattermost sans abonnement actif
✅ **Authentication requise** : Route protégée par `[Authorize]`
✅ **Messages clairs** : L'utilisateur sait exactement où il en est
✅ **Gestion des erreurs** : Toutes les erreurs sont loguées et affichées

## Tests

1. **Créer un utilisateur** et souscrire à un abonnement
2. **Cliquer sur "Activer Mattermost"**
3. **Vérifier dans Mattermost** que l'utilisateur a été créé
4. **Annuler l'abonnement** via Stripe
5. **Vérifier que l'utilisateur Mattermost est désactivé**

## Commandes utiles Docker MySQL

```bash
# Se connecter à MySQL
docker exec -it <conteneur_mysql> mysql -u root -p

# Lister les bases de données
SHOW DATABASES;

# Utiliser une base
USE NomDeTaBase;

# Vérifier les colonnes de Subscriptions
DESCRIBE Subscriptions;

# Voir les abonnements avec Mattermost
SELECT UserId, MattermostUserId, MattermostCreatedAt, IsActive, Status 
FROM Subscriptions;
```

## Dépannage

### Erreur "Abonnement requis"
➡️ Vérifier que l'utilisateur a un abonnement avec `IsActive = true` et `Status = 'active'`

### Compte Mattermost non créé
➡️ Vérifier les logs de l'application
➡️ Vérifier la configuration Mattermost (BaseUrl, ApiToken, TeamName)
➡️ Vérifier que l'API Mattermost est accessible

### Migration SQL échoue
➡️ Vérifier que la table `Subscriptions` existe
➡️ Vérifier les permissions MySQL
➡️ Utiliser un client MySQL graphique pour exécuter le script manuellement

## Prochaines étapes recommandées

1. **Ajouter un webhook Stripe** pour la désactivation automatique
2. **Créer un email de bienvenue Mattermost** avec les identifiants
3. **Ajouter un lien direct vers Mattermost** dans l'espace membre
4. **Implémenter un job de synchronisation** quotidien
5. **Ajouter des logs détaillés** pour le suivi des activations/désactivations
