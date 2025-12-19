#!/bin/bash

################################################################################
# Script de migration de base de données SQL Server
# Docker (local) → Serveur Linux (production)
################################################################################

set -e

# Couleurs
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

log_info() { echo -e "${BLUE}[INFO]${NC} $1"; }
log_success() { echo -e "${GREEN}[SUCCESS]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

# Configuration
DOCKER_CONTAINER="sqlserver"  # Nom de votre conteneur Docker
DOCKER_PASSWORD="YourStrong!Passw0rd"  # Mot de passe Docker SQL
DATABASE_NAME="CrudDemoDB"
BACKUP_FILE="${DATABASE_NAME}_$(date +%Y%m%d_%H%M%S).bak"
BACKUP_PATH="/tmp/${BACKUP_FILE}"

# Serveur distant
REMOTE_HOST=""
REMOTE_USER="root"
REMOTE_PASSWORD=""
REMOTE_BACKUP_PATH="/tmp/${BACKUP_FILE}"

# Demander les informations du serveur distant
read -p "Adresse IP ou domaine du serveur distant : " REMOTE_HOST
read -p "Utilisateur SSH (défaut: root) : " input_user
REMOTE_USER=${input_user:-root}
read -sp "Mot de passe SQL Server du serveur distant : " REMOTE_PASSWORD
echo

################################################################################
# ÉTAPE 1 : BACKUP DEPUIS DOCKER
################################################################################
log_info "Création du backup depuis le conteneur Docker..."

# Vérifier que le conteneur existe
if ! docker ps -a --format '{{.Names}}' | grep -q "^${DOCKER_CONTAINER}$"; then
    log_error "Conteneur Docker '${DOCKER_CONTAINER}' introuvable!"
    log_info "Conteneurs disponibles:"
    docker ps -a --format "  - {{.Names}}"
    exit 1
fi

# Vérifier que le conteneur est en cours d'exécution
if ! docker ps --format '{{.Names}}' | grep -q "^${DOCKER_CONTAINER}$"; then
    log_info "Démarrage du conteneur ${DOCKER_CONTAINER}..."
    docker start ${DOCKER_CONTAINER}
    sleep 5
fi

# Créer le backup dans le conteneur
docker exec ${DOCKER_CONTAINER} /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "${DOCKER_PASSWORD}" -C \
    -Q "BACKUP DATABASE [${DATABASE_NAME}] TO DISK = N'/var/opt/mssql/data/${BACKUP_FILE}' WITH FORMAT, COMPRESSION, STATS = 10;" \
    || { log_error "Échec de la création du backup"; exit 1; }

log_success "Backup créé dans le conteneur: ${BACKUP_FILE}"

################################################################################
# ÉTAPE 2 : COPIER LE BACKUP DEPUIS DOCKER
################################################################################
log_info "Copie du backup depuis le conteneur..."

docker cp ${DOCKER_CONTAINER}:/var/opt/mssql/data/${BACKUP_FILE} ${BACKUP_PATH} \
    || { log_error "Échec de la copie du backup"; exit 1; }

BACKUP_SIZE=$(du -h ${BACKUP_PATH} | cut -f1)
log_success "Backup copié localement: ${BACKUP_PATH} (${BACKUP_SIZE})"

################################################################################
# ÉTAPE 3 : TRANSFÉRER LE BACKUP VERS LE SERVEUR DISTANT
################################################################################
log_info "Transfert du backup vers ${REMOTE_HOST}..."

scp ${BACKUP_PATH} ${REMOTE_USER}@${REMOTE_HOST}:${REMOTE_BACKUP_PATH} \
    || { log_error "Échec du transfert SCP"; exit 1; }

log_success "Backup transféré vers le serveur distant"

################################################################################
# ÉTAPE 4 : RESTAURER LA BASE SUR LE SERVEUR DISTANT
################################################################################
log_info "Restauration de la base de données sur le serveur distant..."

# Créer un script de restauration temporaire
cat > /tmp/restore.sql <<EOF
-- Vérifier si la base existe déjà
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'${DATABASE_NAME}')
BEGIN
    ALTER DATABASE [${DATABASE_NAME}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [${DATABASE_NAME}];
END
GO

-- Restaurer la base
RESTORE DATABASE [${DATABASE_NAME}]
FROM DISK = N'${REMOTE_BACKUP_PATH}'
WITH 
    MOVE '${DATABASE_NAME}' TO '/var/opt/mssql/data/${DATABASE_NAME}.mdf',
    MOVE '${DATABASE_NAME}_log' TO '/var/opt/mssql/data/${DATABASE_NAME}_log.ldf',
    REPLACE,
    STATS = 10;
GO

-- Vérifier la restauration
SELECT name, state_desc, recovery_model_desc 
FROM sys.databases 
WHERE name = '${DATABASE_NAME}';
GO
EOF

# Transférer le script de restauration
scp /tmp/restore.sql ${REMOTE_USER}@${REMOTE_HOST}:/tmp/restore.sql

# Exécuter la restauration
ssh ${REMOTE_USER}@${REMOTE_HOST} \
    "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P '${REMOTE_PASSWORD}' -C -i /tmp/restore.sql" \
    || { log_error "Échec de la restauration"; exit 1; }

log_success "Base de données restaurée avec succès!"

################################################################################
# ÉTAPE 5 : VÉRIFICATION
################################################################################
log_info "Vérification de la migration..."

ssh ${REMOTE_USER}@${REMOTE_HOST} \
    "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P '${REMOTE_PASSWORD}' -d ${DATABASE_NAME} -C -Q \"
    SELECT 
        (SELECT COUNT(*) FROM Courses) as Courses,
        (SELECT COUNT(*) FROM AspNetUsers) as Users,
        (SELECT COUNT(*) FROM Payments) as Payments,
        (SELECT COUNT(*) FROM Subscriptions) as Subscriptions;
    \"" || log_error "Échec de la vérification"

################################################################################
# ÉTAPE 6 : NETTOYAGE
################################################################################
log_info "Nettoyage des fichiers temporaires..."

# Nettoyer localement
rm -f ${BACKUP_PATH}
rm -f /tmp/restore.sql

# Nettoyer sur le serveur distant
ssh ${REMOTE_USER}@${REMOTE_HOST} "rm -f ${REMOTE_BACKUP_PATH} /tmp/restore.sql"

# Nettoyer dans le conteneur Docker
docker exec ${DOCKER_CONTAINER} rm -f /var/opt/mssql/data/${BACKUP_FILE}

log_success "Nettoyage terminé"

################################################################################
# RÉSUMÉ
################################################################################
echo ""
echo "═══════════════════════════════════════════════════════════════════════════════"
log_success "🎉 Migration terminée avec succès!"
echo "═══════════════════════════════════════════════════════════════════════════════"
echo ""
log_info "Prochaines étapes sur le serveur distant:"
echo "  1. Modifier appsettings.json avec la nouvelle connection string"
echo "  2. Redémarrer l'application: systemctl restart cruddemo"
echo "  3. Tester l'accès: curl http://votre-domaine.com"
echo ""
log_info "Connection string pour le serveur:"
echo "  Server=127.0.0.1,1433;Database=${DATABASE_NAME};User Id=sa;Password=VotreMotDePasse;TrustServerCertificate=True;"
echo ""
echo "═══════════════════════════════════════════════════════════════════════════════"
