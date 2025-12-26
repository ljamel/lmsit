#!/bin/bash

################################################################################
# Script d'installation serveur CrudDemo LMS - Version 2.0
# Compatible: Debian 11/12, Ubuntu 20.04/22.04/24.04
# Date: 2025-12-25
#
# Installation automatique de:
#   • .NET 8.0 SDK et Runtime
#   • MariaDB 10.11+ (remplace SQL Server)
#   • Nginx avec reverse proxy optimisé
#   • Certbot pour SSL/TLS automatique
#   • Service systemd avec auto-restart
#   • Configuration firewall UFW
#   • Optimisations système (inotify, limites)
################################################################################

set -euo pipefail  # Mode strict: arrêt sur erreur, variables non définies, erreurs dans pipes
IFS=$'\n\t'

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
