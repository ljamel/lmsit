# Guide de Déploiement - CrudDemo LMS

## 📋 Prérequis

- Serveur Linux (Ubuntu 20.04/22.04 ou Debian 11/12)
- Accès root (sudo)
- Au minimum 2 GB RAM
- 20 GB d'espace disque
- Nom de domaine (optionnel, pour SSL)

## 🚀 Installation du Serveur

### 1. Copier le script sur le serveur

```bash
# Sur votre machine locale
scp setup-server.sh root@votre-serveur:/root/

# Ou télécharger directement sur le serveur
wget https://votre-repo/setup-server.sh
```

### 2. Exécuter le script d'installation

```bash
# Se connecter au serveur
ssh root@votre-serveur

# Rendre le script exécutable
chmod +x setup-server.sh

# Exécuter le script
sudo ./setup-server.sh
```

Le script vous demandera:
- Nom de domaine (laisser vide pour localhost)
- Email pour SSL (si domaine configuré)
- Mot de passe SQL Server SA

### 3. Ce qui est installé automatiquement

- ✅ .NET 8.0 SDK et Runtime
- ✅ SQL Server 2022 (Developer Edition)
- ✅ Nginx (reverse proxy)
- ✅ Certbot (SSL/TLS avec Let's Encrypt)
- ✅ UFW Firewall configuré
- ✅ Service systemd pour l'application
- ✅ Script de déploiement automatique

## 📦 Déploiement de l'Application

### Option 1: Déploiement depuis le dépôt local

```bash
# Sur votre machine locale, compiler l'application
cd /home/lamri/Desktop/lmsprogfacil/CrudDemo
dotnet publish -c Release -o ./publish

# Créer une archive
tar -czf cruddemo.tar.gz ./publish

# Copier sur le serveur
scp cruddemo.tar.gz root@votre-serveur:/tmp/

# Sur le serveur, extraire et déployer
ssh root@votre-serveur
cd /tmp
tar -xzf cruddemo.tar.gz
cp -r publish/* /var/www/cruddemo/
```

### Option 2: Déploiement depuis Git

```bash
# Sur le serveur
cd /var/www/cruddemo
git clone https://github.com/votre-utilisateur/votre-repo.git .
dotnet publish -c Release -o /var/www/cruddemo
```

### Option 3: Utiliser le script de déploiement

Modifiez `/usr/local/bin/deploy-cruddemo.sh` selon votre méthode de déploiement, puis:

```bash
/usr/local/bin/deploy-cruddemo.sh
```

## ⚙️ Configuration de l'Application

### 1. Modifier appsettings.json

```bash
nano /var/www/cruddemo/appsettings.json
```

Mettre à jour:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=127.0.0.1,1433;Database=CrudDemoDB;User Id=sa;Password=VotreMotDePasse;TrustServerCertificate=True;"
  },
  "Stripe": {
    "PublishableKey": "pk_live_VOTRE_CLE",
    "SecretKey": "sk_live_VOTRE_CLE"
  }
}
```

### 2. Exécuter les migrations

```bash
cd /var/www/cruddemo
sudo -u cruddemo dotnet ef database update
```

Si `dotnet ef` n'est pas installé:

```bash
dotnet tool install --global dotnet-ef
export PATH="$PATH:/root/.dotnet/tools"
```

### 3. Créer la base de données manuellement (alternative)

```bash
# Se connecter à SQL Server
/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'VotreMotDePasse'

# Dans sqlcmd:
CREATE DATABASE CrudDemoDB;
GO
EXIT
```

## 🎯 Gestion du Service

### Démarrer l'application

```bash
systemctl start cruddemo
```

### Arrêter l'application

```bash
systemctl stop cruddemo
```

### Redémarrer l'application

```bash
systemctl restart cruddemo
```

### Voir les logs en temps réel

```bash
journalctl -u cruddemo -f
```

### Vérifier le statut

```bash
systemctl status cruddemo
```

### Activer le démarrage automatique

```bash
systemctl enable cruddemo
```

## 🔍 Vérification et Tests

### Tester l'application

```bash
# Test local
curl http://localhost:5000

# Test via Nginx
curl http://votre-domaine.com
```

### Vérifier SQL Server

```bash
systemctl status mssql-server
/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'VotreMotDePasse' -Q "SELECT @@VERSION"
```

### Vérifier Nginx

```bash
systemctl status nginx
nginx -t
tail -f /var/log/nginx/access.log
```

## 🔒 Sécurité (Production)

### 1. Créer un utilisateur SQL dédié

```bash
/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'VotreMotDePasse'
```

```sql
USE CrudDemoDB;
GO

CREATE LOGIN cruddemouser WITH PASSWORD = 'AutreMotDePasseSecurise!123';
GO

CREATE USER cruddemouser FOR LOGIN cruddemouser;
GO

ALTER ROLE db_owner ADD MEMBER cruddemouser;
GO
```

Mettre à jour appsettings.json:

```json
"DefaultConnection": "Server=127.0.0.1,1433;Database=CrudDemoDB;User Id=cruddemouser;Password=AutreMotDePasseSecurise!123;TrustServerCertificate=True;"
```

### 2. Restreindre l'accès SQL Server

```bash
# Bloquer l'accès externe à SQL Server
ufw delete allow 1433/tcp
ufw allow from 127.0.0.1 to any port 1433
```

### 3. Configurer HTTPS uniquement

Modifier `/etc/nginx/sites-available/cruddemo`:

```nginx
# Redirection HTTP vers HTTPS
server {
    listen 80;
    server_name votre-domaine.com;
    return 301 https://$server_name$request_uri;
}
```

### 4. Sauvegardes automatiques

Créer un script de sauvegarde:

```bash
cat > /usr/local/bin/backup-cruddemo.sh <<'EOF'
#!/bin/bash
BACKUP_DIR="/backup/cruddemo"
DATE=$(date +%Y%m%d_%H%M%S)

mkdir -p $BACKUP_DIR

# Backup SQL
/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'VotreMotDePasse' \
  -Q "BACKUP DATABASE CrudDemoDB TO DISK = N'$BACKUP_DIR/db_$DATE.bak'"

# Backup fichiers
tar -czf $BACKUP_DIR/files_$DATE.tar.gz /var/www/cruddemo/wwwroot/videos

# Garder seulement 7 jours
find $BACKUP_DIR -mtime +7 -delete

echo "Backup terminé: $DATE"
EOF

chmod +x /usr/local/bin/backup-cruddemo.sh

# Ajouter au cron (tous les jours à 2h)
crontab -e
# Ajouter: 0 2 * * * /usr/local/bin/backup-cruddemo.sh
```

## 📊 Monitoring

### Installer htop pour surveiller les ressources

```bash
apt-get install -y htop
htop
```

### Vérifier l'utilisation disque

```bash
df -h
du -sh /var/www/cruddemo/*
```

### Logs importants

```bash
# Logs application
journalctl -u cruddemo -n 100

# Logs Nginx
tail -f /var/log/nginx/error.log
tail -f /var/log/nginx/access.log

# Logs SQL Server
tail -f /var/opt/mssql/log/errorlog
```

## 🐛 Dépannage

### L'application ne démarre pas

```bash
# Vérifier les logs
journalctl -u cruddemo -n 50

# Vérifier les permissions
ls -la /var/www/cruddemo
chown -R cruddemo:cruddemo /var/www/cruddemo

# Tester manuellement
cd /var/www/cruddemo
sudo -u cruddemo dotnet CrudDemo.dll
```

### SQL Server ne se connecte pas

```bash
# Vérifier le service
systemctl status mssql-server

# Tester la connexion
/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'VotreMotDePasse'

# Vérifier les logs
tail -f /var/opt/mssql/log/errorlog
```

### Nginx 502 Bad Gateway

```bash
# Vérifier que l'application tourne
systemctl status cruddemo

# Vérifier que le port 5000 est ouvert
netstat -tlnp | grep 5000

# Redémarrer Nginx
systemctl restart nginx
```

## 📝 Commandes Utiles

```bash
# Voir la configuration complète
cat /root/cruddemo-config.txt

# Redéployer l'application
/usr/local/bin/deploy-cruddemo.sh

# Voir tous les services
systemctl list-units --type=service

# Mettre à jour .NET
apt-get update && apt-get upgrade dotnet-sdk-8.0

# Nettoyer l'espace disque
apt-get autoremove -y
apt-get clean
```

## 🔄 Mise à Jour de l'Application

```bash
# 1. Arrêter le service
systemctl stop cruddemo

# 2. Sauvegarder la version actuelle
cp -r /var/www/cruddemo /var/www/cruddemo.backup

# 3. Déployer la nouvelle version
# (utiliser une des méthodes de déploiement ci-dessus)

# 4. Exécuter les migrations si nécessaire
cd /var/www/cruddemo
dotnet ef database update

# 5. Redémarrer le service
systemctl start cruddemo

# 6. Vérifier
journalctl -u cruddemo -f
```

## 💡 Bonnes Pratiques

1. **Toujours tester en local avant de déployer**
2. **Faire des sauvegardes régulières**
3. **Utiliser un utilisateur SQL dédié (pas SA)**
4. **Activer HTTPS en production**
5. **Surveiller les logs régulièrement**
6. **Garder le système à jour**
7. **Documenter vos changements**

## 🆘 Support

Pour plus d'informations, consultez:
- Configuration: `/root/cruddemo-config.txt`
- Logs: `journalctl -u cruddemo -f`
- Nginx: `/etc/nginx/sites-available/cruddemo`
- Service: `/etc/systemd/system/cruddemo.service`
