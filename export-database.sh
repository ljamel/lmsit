#!/bin/bash

################################################################################
# Script d'export de la base de données SQL Server depuis Docker
# Crée un backup complet de la base de données
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
log_warning() { echo -e "${YELLOW}[WARNING]${NC} $1"; }

################################################################################
# CONFIGURATION
################################################################################

# Paramètres Docker
DOCKER_CONTAINER=""
DOCKER_PASSWORD=""
DATABASE_NAME="CrudDemoDB"

# Destination du backup
BACKUP_DIR="./database-backups"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="${DATABASE_NAME}_${TIMESTAMP}.bak"
BACKUP_PATH="${BACKUP_DIR}/${BACKUP_FILE}"

################################################################################
# DÉTECTION AUTOMATIQUE DU CONTENEUR SQL SERVER
################################################################################
log_info "Recherche du conteneur SQL Server..."

# Chercher les conteneurs avec l'image SQL Server
SQLSERVER_CONTAINERS=$(docker ps -a --filter "ancestor=mcr.microsoft.com/mssql/server" --format "{{.Names}}" 2>/dev/null || true)

if [ -z "$SQLSERVER_CONTAINERS" ]; then
    # Essayer avec d'autres patterns communs
    SQLSERVER_CONTAINERS=$(docker ps -a --format "{{.Names}}" | grep -i "sql\|mssql" || true)
fi

if [ -z "$SQLSERVER_CONTAINERS" ]; then
    log_error "Aucun conteneur SQL Server trouvé!"
    log_info "Conteneurs Docker disponibles:"
    docker ps -a --format "  - {{.Names}} ({{.Image}})"
    echo ""
    read -p "Entrez le nom du conteneur SQL Server : " DOCKER_CONTAINER
else
    echo "Conteneurs SQL Server trouvés:"
    echo "$SQLSERVER_CONTAINERS" | while read container; do
        STATUS=$(docker inspect -f '{{.State.Status}}' "$container")
        echo "  - $container ($STATUS)"
    done
    echo ""
    
    # Si un seul conteneur, le sélectionner automatiquement
    CONTAINER_COUNT=$(echo "$SQLSERVER_CONTAINERS" | wc -l)
    if [ "$CONTAINER_COUNT" -eq 1 ]; then
        DOCKER_CONTAINER="$SQLSERVER_CONTAINERS"
        log_info "Conteneur sélectionné automatiquement: $DOCKER_CONTAINER"
    else
        read -p "Entrez le nom du conteneur à utiliser : " DOCKER_CONTAINER
    fi
fi

# Vérifier que le conteneur existe
if ! docker ps -a --format '{{.Names}}' | grep -q "^${DOCKER_CONTAINER}$"; then
    log_error "Conteneur '$DOCKER_CONTAINER' introuvable!"
    exit 1
fi

# Vérifier que le conteneur est en cours d'exécution
if ! docker ps --format '{{.Names}}' | grep -q "^${DOCKER_CONTAINER}$"; then
    log_warning "Le conteneur n'est pas démarré. Démarrage..."
    docker start ${DOCKER_CONTAINER}
    log_info "Attente du démarrage de SQL Server (10 secondes)..."
    sleep 10
fi

# Demander le mot de passe
read -sp "Mot de passe SQL Server (sa) : " DOCKER_PASSWORD
echo ""

# Demander le nom de la base (option)
read -p "Nom de la base de données (défaut: CrudDemoDB) : " input_db
DATABASE_NAME=${input_db:-CrudDemoDB}

################################################################################
# CRÉATION DU RÉPERTOIRE DE BACKUP
################################################################################
log_info "Création du répertoire de backup..."
mkdir -p ${BACKUP_DIR}

################################################################################
# VÉRIFICATION DE LA BASE DE DONNÉES
################################################################################
log_info "Vérification de l'existence de la base de données..."

DB_EXISTS=$(docker exec ${DOCKER_CONTAINER} /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "${DOCKER_PASSWORD}" -C -h -1 \
    -Q "SET NOCOUNT ON; SELECT CASE WHEN EXISTS(SELECT name FROM sys.databases WHERE name = '${DATABASE_NAME}') THEN 'YES' ELSE 'NO' END" \
    2>/dev/null | tr -d '[:space:]' || echo "ERROR")

if [ "$DB_EXISTS" != "YES" ]; then
    log_error "La base de données '${DATABASE_NAME}' n'existe pas!"
    log_info "Bases de données disponibles:"
    docker exec ${DOCKER_CONTAINER} /opt/mssql-tools18/bin/sqlcmd \
        -S localhost -U sa -P "${DOCKER_PASSWORD}" -C \
        -Q "SELECT name FROM sys.databases WHERE database_id > 4;" 2>/dev/null || true
    exit 1
fi

log_success "Base de données '${DATABASE_NAME}' trouvée"

################################################################################
# AFFICHAGE DES STATISTIQUES DE LA BASE
################################################################################
log_info "Récupération des statistiques..."

docker exec ${DOCKER_CONTAINER} /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "${DOCKER_PASSWORD}" -d ${DATABASE_NAME} -C \
    -Q "
    SELECT 
        'Courses' as TableName, COUNT(*) as RowCount FROM Courses
    UNION ALL
    SELECT 'Modules', COUNT(*) FROM Modules
    UNION ALL
    SELECT 'Lessons', COUNT(*) FROM Lessons
    UNION ALL
    SELECT 'AspNetUsers', COUNT(*) FROM AspNetUsers
    UNION ALL
    SELECT 'Payments', COUNT(*) FROM Payments WHERE 1=0 OR EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='Payments')
    UNION ALL
    SELECT 'Subscriptions', COUNT(*) FROM Subscriptions WHERE 1=0 OR EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='Subscriptions');
    " 2>/dev/null || log_warning "Impossible de récupérer les statistiques"

################################################################################
# CRÉATION DU BACKUP
################################################################################
log_info "Création du backup de la base de données..."
log_info "Fichier: ${BACKUP_FILE}"

docker exec ${DOCKER_CONTAINER} /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "${DOCKER_PASSWORD}" -C \
    -Q "BACKUP DATABASE [${DATABASE_NAME}] TO DISK = N'/var/opt/mssql/data/${BACKUP_FILE}' WITH FORMAT, COMPRESSION, STATS = 10;" \
    || { log_error "Échec de la création du backup"; exit 1; }

log_success "Backup créé dans le conteneur"

################################################################################
# COPIE DU BACKUP DEPUIS LE CONTENEUR
################################################################################
log_info "Copie du backup depuis le conteneur..."

docker cp ${DOCKER_CONTAINER}:/var/opt/mssql/data/${BACKUP_FILE} ${BACKUP_PATH} \
    || { log_error "Échec de la copie du backup"; exit 1; }

# Taille du fichier
BACKUP_SIZE=$(du -h ${BACKUP_PATH} | cut -f1)
log_success "Backup copié: ${BACKUP_PATH} (${BACKUP_SIZE})"

################################################################################
# NETTOYAGE DU CONTENEUR
################################################################################
log_info "Nettoyage du backup dans le conteneur..."
docker exec ${DOCKER_CONTAINER} rm -f /var/opt/mssql/data/${BACKUP_FILE} \
    || log_warning "Impossible de supprimer le fichier dans le conteneur"

################################################################################
# CRÉATION D'UN SCRIPT SQL OPTIONNEL
################################################################################
log_info "Création d'un script SQL complet (schéma + données)..."

SQL_FILE="${BACKUP_DIR}/${DATABASE_NAME}_${TIMESTAMP}.sql"

# Générer le script avec sqlcmd
docker exec ${DOCKER_CONTAINER} /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "${DOCKER_PASSWORD}" -d ${DATABASE_NAME} -C \
    -Q "
    -- Export des utilisateurs
    SELECT 'INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount) VALUES (' +
    QUOTENAME(Id, '''') + ',' +
    QUOTENAME(UserName, '''') + ',' +
    QUOTENAME(NormalizedUserName, '''') + ',' +
    QUOTENAME(Email, '''') + ',' +
    QUOTENAME(NormalizedEmail, '''') + ',' +
    CAST(EmailConfirmed as varchar) + ',' +
    QUOTENAME(PasswordHash, '''') + ',' +
    QUOTENAME(SecurityStamp, '''') + ',' +
    QUOTENAME(ConcurrencyStamp, '''') + ',' +
    CAST(PhoneNumberConfirmed as varchar) + ',' +
    CAST(TwoFactorEnabled as varchar) + ',' +
    CAST(LockoutEnabled as varchar) + ',' +
    CAST(AccessFailedCount as varchar) + ');'
    FROM AspNetUsers;
    " > ${SQL_FILE} 2>/dev/null || log_warning "Impossible de créer le script SQL"

if [ -f "${SQL_FILE}" ] && [ -s "${SQL_FILE}" ]; then
    SQL_SIZE=$(du -h ${SQL_FILE} | cut -f1)
    log_success "Script SQL créé: ${SQL_FILE} (${SQL_SIZE})"
else
    rm -f ${SQL_FILE}
    log_warning "Script SQL non créé (méthode alternative recommandée)"
fi

################################################################################
# RÉSUMÉ
################################################################################
echo ""
echo "═══════════════════════════════════════════════════════════════════════════════"
log_success "🎉 Export terminé avec succès!"
echo "═══════════════════════════════════════════════════════════════════════════════"
echo ""
log_info "Fichiers créés:"
echo "  📦 Backup (.bak): ${BACKUP_PATH} (${BACKUP_SIZE})"
if [ -f "${SQL_FILE}" ]; then
    echo "  📄 Script SQL:    ${SQL_FILE} (${SQL_SIZE})"
fi
echo ""
log_info "Pour restaurer sur un autre serveur:"
echo "  1. Copier le fichier .bak sur le serveur:"
echo "     scp ${BACKUP_PATH} root@serveur:/tmp/"
echo ""
echo "  2. Restaurer avec sqlcmd:"
echo "     /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'Password' -C -Q \\"
echo "     \"RESTORE DATABASE ${DATABASE_NAME} FROM DISK='/tmp/${BACKUP_FILE}' \\"
echo "     WITH MOVE '${DATABASE_NAME}' TO '/var/opt/mssql/data/${DATABASE_NAME}.mdf', \\"
echo "     MOVE '${DATABASE_NAME}_log' TO '/var/opt/mssql/data/${DATABASE_NAME}_log.ldf', REPLACE;\""
echo ""
echo "  3. Ou utiliser le script de migration automatique:"
echo "     ./migrate-database.sh"
echo ""
echo "═══════════════════════════════════════════════════════════════════════════════"
