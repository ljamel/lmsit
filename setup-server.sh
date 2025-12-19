#!/bin/bash

################################################################################
# Script d'installation et de configuration serveur pour CrudDemo LMS
# Optimisé pour Debian 11/12
# 
# Ce script installe :
# - .NET 8.0 SDK et Runtime
# - SQL Server 2022 pour Linux
# - Nginx comme reverse proxy
# - Certbot pour SSL/TLS (Let's Encrypt)
# - Configuration du firewall (UFW)
# - Configuration du service systemd pour l'application
################################################################################

set -e  # Arrêter en cas d'erreur

# Détecter la version de Debian
DEBIAN_VERSION=$(cat /etc/debian_version | cut -d. -f1)

# Couleurs pour les messages
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Fonction pour afficher les messages
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Vérifier si le script est exécuté en tant que root
if [[ $EUID -ne 0 ]]; then
   log_error "Ce script doit être exécuté en tant que root (sudo)"
   exit 1
fi

log_info "Début de l'installation du serveur pour CrudDemo LMS"

# Configuration variables (à modifier selon vos besoins)
APP_NAME="cruddemo"
APP_USER="cruddemo"
APP_DIR="/var/www/$APP_NAME"
DOMAIN_NAME=""  # Laisser vide pour localhost, ou votre-domaine.com pour production
SA_PASSWORD="YourStrong!Passw0rd"  # Mot de passe SQL Server (changer en production!)
DOTNET_VERSION="8.0"

# Demander les informations si non définies
read -p "Nom de domaine (laisser vide pour localhost) : " input_domain
DOMAIN_NAME=${input_domain:-localhost}

if [ "$DOMAIN_NAME" != "localhost" ]; then
    read -p "Email pour Let's Encrypt SSL : " ssl_email
fi

read -sp "Mot de passe SQL Server SA (min 8 caractères, majuscule, minuscule, chiffre, symbole) : " input_password
################################################################################
# 1. MISE À JOUR DU SYSTÈME
################################################################################
log_info "Mise à jour du système Debian $DEBIAN_VERSION..."
apt-get update -y
log_success "Système mis à jour"################################################
log_info "Mise à jour du système..."
apt-get update -y
log_success "Système mis à jour"

################################################################################
# 2. INSTALLATION DES OUTILS DE BASE
################################################################################
log_info "Installation des outils de base..."
apt-get install -y \
    curl \
    wget \
    gnupg \
    software-properties-common \
    apt-transport-https \
    ca-certificates \
    unzip \
    git \
    ufw
log_success "Outils de base installés"
################################################################################
# 3. INSTALLATION DE .NET 8.0 SDK ET RUNTIME
################################################################################
log_info "Installation de .NET $DOTNET_VERSION pour Debian..."

# Ajouter le repository Microsoft pour Debian
if [ "$DEBIAN_VERSION" -eq "12" ]; then
    wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
elif [ "$DEBIAN_VERSION" -eq "11" ]; then
    wget https://packages.microsoft.com/config/debian/11/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
else
    log_error "Version Debian non supportée: $DEBIAN_VERSION. Veuillez utiliser Debian 11 ou 12."
    exit 1
fi

dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Installer .NET SDK et Runtime
################################################################################
# 4. INSTALLATION DE SQL SERVER 2022
################################################################################
log_info "Installation de SQL Server 2022 pour Debian..."

# Ajouter la clé GPG Microsoft
curl https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor -o /usr/share/keyrings/microsoft-prod.gpg

# Ajouter le repository SQL Server pour Debian
if [ "$DEBIAN_VERSION" -eq "12" ]; then
    curl https://packages.microsoft.com/config/debian/12/mssql-server-2022.list | tee /etc/apt/sources.list.d/mssql-server-2022.list
elif [ "$DEBIAN_VERSION" -eq "11" ]; then
    curl https://packages.microsoft.com/config/debian/11/mssql-server-2022.list | tee /etc/apt/sources.list.d/mssql-server-2022.list
fi

# Installer SQL Server
apt-get update -y
apt-get install -y mssql-server

# Configurer SQL Server (Developer Edition)
MSSQL_PID=Developer ACCEPT_EULA=Y MSSQL_SA_PASSWORD=$SA_PASSWORD /opt/mssql/bin/mssql-conf setup

# Démarrer et activer SQL Server
systemctl start mssql-server
systemctl enable mssql-server

# Ajouter le repository pour mssql-tools
if [ "$DEBIAN_VERSION" -eq "12" ]; then
    curl https://packages.microsoft.com/config/debian/12/prod.list | tee /etc/apt/sources.list.d/msprod.list
elif [ "$DEBIAN_VERSION" -eq "11" ]; then
    curl https://packages.microsoft.com/config/debian/11/prod.list | tee /etc/apt/sources.list.d/msprod.list
fi

# Installer les outils SQL Server (sqlcmd)
apt-get update -y
ACCEPT_EULA=Y apt-get install -y mssql-tools18 unixodbc-dev

# Ajouter sqlcmd au PATH
echo 'export PATH="$PATH:/opt/mssql-tools18/bin"' >> /etc/profile
echo 'export PATH="$PATH:/opt/mssql-tools18/bin"' >> /root/.bashrc
export PATH="$PATH:/opt/mssql-tools18/bin"

log_success "SQL Server 2022 installé et configuré"
# Installer les outils SQL Server (sqlcmd)
ACCEPT_EULA=Y apt-get install -y mssql-tools unixodbc-dev

# Ajouter sqlcmd au PATH
echo 'export PATH="$PATH:/opt/mssql-tools/bin"' >> /etc/profile
source /etc/profile

log_success "SQL Server 2022 installé et configuré"

################################################################################
# 5. CRÉATION DE L'UTILISATEUR ET DU RÉPERTOIRE APPLICATION
################################################################################
log_info "Création de l'utilisateur $APP_USER..."

# Créer l'utilisateur système pour l'application
if ! id "$APP_USER" &>/dev/null; then
    useradd -r -m -s /bin/bash $APP_USER
    log_success "Utilisateur $APP_USER créé"
else
    log_warning "Utilisateur $APP_USER existe déjà"
fi

# Créer le répertoire de l'application
mkdir -p $APP_DIR
chown -R $APP_USER:$APP_USER $APP_DIR
log_success "Répertoire $APP_DIR créé"

################################################################################
# 6. INSTALLATION ET CONFIGURATION DE NGINX
################################################################################
log_info "Installation de Nginx..."
apt-get install -y nginx

# Créer la configuration Nginx pour l'application
cat > /etc/nginx/sites-available/$APP_NAME <<EOF
server {
    listen 80;
    server_name $DOMAIN_NAME;
    
    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host \$host;
        proxy_cache_bypass \$http_upgrade;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_set_header X-Real-IP \$remote_addr;
        
        # Timeout pour les vidéos
        proxy_connect_timeout 600;
        proxy_send_timeout 600;
        proxy_read_timeout 600;
        send_timeout 600;
        
        # Upload size pour les vidéos
        client_max_body_size 500M;
    }
}
EOF

# Activer le site
ln -sf /etc/nginx/sites-available/$APP_NAME /etc/nginx/sites-enabled/
rm -f /etc/nginx/sites-enabled/default

# Tester la configuration
nginx -t

# Redémarrer Nginx
systemctl restart nginx
systemctl enable nginx

log_success "Nginx installé et configuré"

################################################################################
# 7. CONFIGURATION DU FIREWALL (UFW)
################################################################################
log_info "Configuration du firewall..."

# Autoriser SSH, HTTP, HTTPS
ufw allow OpenSSH
ufw allow 'Nginx Full'
ufw allow 1433/tcp  # SQL Server (à restreindre en production!)

# Activer le firewall
ufw --force enable

log_success "Firewall configuré"

################################################################################
# 8. INSTALLATION DE CERTBOT POUR SSL (si domaine configuré)
################################################################################
if [ "$DOMAIN_NAME" != "localhost" ]; then
    log_info "Installation de Certbot pour SSL..."
    
    apt-get install -y certbot python3-certbot-nginx
    
    # Obtenir le certificat SSL
    certbot --nginx -d $DOMAIN_NAME --non-interactive --agree-tos -m $ssl_email --redirect
    
    # Renouvellement automatique
    systemctl enable certbot.timer
    
    log_success "SSL configuré pour $DOMAIN_NAME"
else
    log_warning "Pas de domaine configuré, SSL non installé"
fi

################################################################################
# 9. CRÉATION DU SERVICE SYSTEMD
################################################################################
log_info "Création du service systemd..."

cat > /etc/systemd/system/$APP_NAME.service <<EOF
[Unit]
Description=CrudDemo LMS Application
After=network.target

[Service]
Type=notify
User=$APP_USER
WorkingDirectory=$APP_DIR
ExecStart=/usr/bin/dotnet $APP_DIR/CrudDemo.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=$APP_NAME
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
EOF

# Recharger systemd
systemctl daemon-reload

log_success "Service systemd créé"

################################################################################
# 10. CRÉATION DU SCRIPT DE DÉPLOIEMENT
################################################################################
log_info "Création du script de déploiement..."

cat > /usr/local/bin/deploy-$APP_NAME.sh <<'EOF'
#!/bin/bash

APP_NAME="cruddemo"
APP_USER="cruddemo"
APP_DIR="/var/www/$APP_NAME"
BUILD_DIR="/tmp/cruddemo-build"

echo "🚀 Début du déploiement..."

# Arrêter l'application
systemctl stop $APP_NAME

# Créer le répertoire de build
rm -rf $BUILD_DIR
mkdir -p $BUILD_DIR

# Copier les fichiers source (à adapter selon votre méthode de déploiement)
# Option 1: Depuis Git
# git clone https://github.com/votre-repo/cruddemo.git $BUILD_DIR

# Option 2: Depuis un fichier ZIP uploadé
# unzip -o /tmp/cruddemo.zip -d $BUILD_DIR

echo "📦 Compilation de l'application..."
cd $BUILD_DIR
dotnet publish -c Release -o $APP_DIR

# Définir les permissions
chown -R $APP_USER:$APP_USER $APP_DIR
chmod -R 755 $APP_DIR

# Sauvegarder appsettings.json si nécessaire
# cp /backup/appsettings.Production.json $APP_DIR/appsettings.json

# Redémarrer l'application
systemctl start $APP_NAME
systemctl status $APP_NAME

echo "✅ Déploiement terminé!"
EOF

chmod +x /usr/local/bin/deploy-$APP_NAME.sh

log_success "Script de déploiement créé: /usr/local/bin/deploy-$APP_NAME.sh"

################################################################################
# 11. CRÉATION DU FICHIER DE CONFIGURATION
################################################################################
log_info "Création du fichier de configuration..."

cat > /root/$APP_NAME-config.txt <<EOF
################################################################################
# CONFIGURATION CRUDDEMO LMS
################################################################################

Application:
  - Nom: $APP_NAME
  - Utilisateur: $APP_USER
  - Répertoire: $APP_DIR
  - Service: $APP_NAME.service

SQL Server:
  - Version: 2022 (Developer Edition)
  - Host: 127.0.0.1
  - Port: 1433
  - SA Password: $SA_PASSWORD
  - Connection String: Server=127.0.0.1,1433;Database=CrudDemoDB;User Id=sa;Password=$SA_PASSWORD;TrustServerCertificate=True;

Web:
  - Domaine: $DOMAIN_NAME
  - Port interne: 5000
  - Nginx: Reverse proxy sur port 80/443

Commandes utiles:
  - Déployer: /usr/local/bin/deploy-$APP_NAME.sh
  - Démarrer: systemctl start $APP_NAME
  - Arrêter: systemctl stop $APP_NAME
  - Redémarrer: systemctl restart $APP_NAME
  - Logs: journalctl -u $APP_NAME -f
  - Nginx logs: tail -f /var/log/nginx/access.log
  - SQL Server status: systemctl status mssql-server

Prochaines étapes:
  1. Copier votre code source dans $APP_DIR
  2. Modifier $APP_DIR/appsettings.json avec la bonne connection string
  3. Exécuter les migrations: dotnet ef database update
  4. Démarrer le service: systemctl start $APP_NAME
  5. Vérifier: curl http://$DOMAIN_NAME

Sécurité:
  ⚠️  IMPORTANT: Changez le mot de passe SA en production!
  ⚠️  Restreignez l'accès SQL Server (port 1433) au localhost uniquement
  ⚠️  Configurez les clés Stripe dans appsettings.json
  ⚠️  Ajoutez un utilisateur SQL dédié (ne pas utiliser SA)

################################################################################
EOF

log_success "Fichier de configuration créé: /root/$APP_NAME-config.txt"

################################################################################
# 12. OPTIMISATIONS SYSTÈME
################################################################################
log_info "Optimisations système..."

# Augmenter les limites de fichiers ouverts
cat >> /etc/security/limits.conf <<EOF
$APP_USER soft nofile 65536
$APP_USER hard nofile 65536
EOF

# Optimiser SQL Server memory
/opt/mssql/bin/mssql-conf set memory.memorylimitmb 2048

log_success "Optimisations appliquées"

################################################################################
# FIN DE L'INSTALLATION
################################################################################
echo ""
echo "═══════════════════════════════════════════════════════════════════════════════"
log_success "🎉 Installation terminée avec succès!"
echo "═══════════════════════════════════════════════════════════════════════════════"
echo ""
log_info "Consultez le fichier de configuration: cat /root/$APP_NAME-config.txt"
echo ""
log_warning "PROCHAINES ÉTAPES:"
echo "  1. Déployez votre code avec: /usr/local/bin/deploy-$APP_NAME.sh"
echo "  2. Configurez appsettings.json avec vos clés Stripe"
echo "  3. Exécutez les migrations de base de données"
echo "  4. Démarrez l'application: systemctl start $APP_NAME"
echo ""
log_info "Serveur prêt pour l'hébergement de CrudDemo LMS!"
echo "═══════════════════════════════════════════════════════════════════════════════"
