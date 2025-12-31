# Configuration Serveur MySQL Distant

## Problème résolu
❌ **Erreur** : `Unable to connect to any of the specified MySQL hosts`
✅ **Solution** : Remplacé `ServerVersion.AutoDetect()` par une version MySQL fixe

## Configuration requise sur le serveur distant

### 1. Vérifier que MySQL est démarré
```bash
sudo systemctl status mysql
# ou
sudo systemctl status mariadb
```

Si non démarré :
```bash
sudo systemctl start mysql
sudo systemctl enable mysql  # Démarrage auto
```

### 2. Vérifier que MySQL écoute sur le bon port
```bash
sudo netstat -tlnp | grep mysql
# ou
sudo ss -tlnp | grep mysql
```

Vous devriez voir quelque chose comme :
```
tcp        0      0 127.0.0.1:3307          0.0.0.0:*               LISTEN      1234/mysqld
```

### 3. Configuration appsettings.json sur le serveur

**Option A : Si MySQL écoute sur localhost**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=127.0.0.1;Port=3307;Database=NomDeTaBase;User=root;Password=StrongPass123!;"
  }
}
```

**Option B : Si MySQL écoute sur 0.0.0.0 ou IP spécifique**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=0.0.0.0;Port=3307;Database=NomDeTaBase;User=root;Password=StrongPass123!;"
  }
}
```

### 4. Vérifier la connexion MySQL
Depuis le serveur, testez :
```bash
mysql -h 127.0.0.1 -P 3307 -u root -p
# Entrez le mot de passe : StrongPass123!
```

Si ça ne fonctionne pas, vérifiez :
```bash
# Éditer la config MySQL
sudo nano /etc/mysql/mysql.conf.d/mysqld.cnf

# Cherchez et modifiez :
bind-address = 127.0.0.1
port = 3307

# Redémarrez MySQL
sudo systemctl restart mysql
```

### 5. Créer la base de données si elle n'existe pas
```sql
mysql -u root -p -e "CREATE DATABASE IF NOT EXISTS NomDeTaBase CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;"
```

### 6. Donner les permissions à l'utilisateur
```sql
mysql -u root -p
```

```sql
-- Créer l'utilisateur s'il n'existe pas
CREATE USER IF NOT EXISTS 'root'@'localhost' IDENTIFIED BY 'StrongPass123!';
CREATE USER IF NOT EXISTS 'root'@'127.0.0.1' IDENTIFIED BY 'StrongPass123!';

-- Donner tous les privilèges
GRANT ALL PRIVILEGES ON NomDeTaBase.* TO 'root'@'localhost';
GRANT ALL PRIVILEGES ON NomDeTaBase.* TO 'root'@'127.0.0.1';
FLUSH PRIVILEGES;
```

### 7. Vérifier la version de MySQL
```bash
mysql --version
```

Si la version n'est pas 8.0.21, ajustez dans Program.cs :
```csharp
new MySqlServerVersion(new Version(8, 0, 21)) // Changez selon votre version
// Exemples:
// new MySqlServerVersion(new Version(5, 7, 44))
// new MariaDbServerVersion(new Version(10, 5, 23))
```

### 8. Variables d'environnement (recommandé pour production)
Au lieu de modifier appsettings.json, utilisez des variables d'environnement :

```bash
export ConnectionStrings__DefaultConnection="Server=127.0.0.1;Port=3307;Database=NomDeTaBase;User=root;Password=StrongPass123!;"
```

Ou créez un fichier `/etc/systemd/system/lmsit.service` :
```ini
[Unit]
Description=LMS Application
After=mysql.service

[Service]
WorkingDirectory=/home/admin/lmsit
ExecStart=/usr/bin/dotnet /home/admin/lmsit/CrudDemo.dll --urls "http://0.0.0.0:5216"
Restart=always
RestartSec=10
Environment="ASPNETCORE_ENVIRONMENT=Production"
Environment="ConnectionStrings__DefaultConnection=Server=127.0.0.1;Port=3307;Database=NomDeTaBase;User=root;Password=StrongPass123!;"
User=admin
Group=admin

[Install]
WantedBy=multi-user.target
```

Activez le service :
```bash
sudo systemctl daemon-reload
sudo systemctl enable lmsit
sudo systemctl start lmsit
sudo systemctl status lmsit
```

### 9. Logs utiles pour le diagnostic
```bash
# Logs MySQL
sudo tail -f /var/log/mysql/error.log

# Logs application
journalctl -u lmsit -f

# Vérifier les connexions actives
sudo netstat -anp | grep :3307
```

### 10. Test de connectivité rapide
```bash
# Installer telnet si nécessaire
sudo apt install telnet

# Tester la connexion au port MySQL
telnet 127.0.0.1 3307
# Si ça se connecte, le port est ouvert
# Ctrl+] puis 'quit' pour sortir
```

## Checklist de dépannage

- [ ] MySQL est démarré
- [ ] MySQL écoute sur le bon port (3307)
- [ ] La base de données existe
- [ ] L'utilisateur existe et a les permissions
- [ ] Le mot de passe est correct
- [ ] La connexion fonctionne via mysql CLI
- [ ] La version MySQL dans Program.cs correspond
- [ ] Aucun firewall ne bloque le port
- [ ] appsettings.json a la bonne configuration

## Notes importantes

1. **Port 3307 vs 3306** : Vous utilisez le port 3307, assurez-vous que MySQL écoute bien sur ce port
2. **Production** : N'utilisez JAMAIS root en production, créez un utilisateur dédié
3. **Mot de passe** : Changez le mot de passe en production
4. **SSL** : Pour production, ajoutez `SslMode=Required` dans la chaîne de connexion
