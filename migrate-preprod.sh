#!/bin/bash

# Script de migration pour le serveur de préproduction
# Ajoute la colonne SelectedOptionId à la table UserQuizResults

# Configuration - MODIFIEZ CES VALEURS SELON VOTRE SERVEUR
DB_HOST="127.0.0.1"
DB_PORT="3307"
DB_USER="root"
DB_PASS="StrongPass123!"
DB_NAME="NomDeTaBase"

echo "=========================================="
echo "Migration: Add SelectedOptionId column"
echo "=========================================="
echo "Server: $DB_HOST:$DB_PORT"
echo "Database: $DB_NAME"
echo ""

# Vérifier si la colonne existe déjà
echo "Vérification de l'existence de la colonne..."
COLUMN_EXISTS=$(mysql -h "$DB_HOST" -P "$DB_PORT" -u "$DB_USER" -p"$DB_PASS" --skip-ssl "$DB_NAME" -sNe "
SELECT COUNT(*) 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = '$DB_NAME' 
  AND TABLE_NAME = 'UserQuizResults' 
  AND COLUMN_NAME = 'SelectedOptionId';
")

if [ "$COLUMN_EXISTS" -eq "1" ]; then
    echo "✓ La colonne SelectedOptionId existe déjà dans UserQuizResults"
    echo "Migration déjà appliquée."
    exit 0
fi

echo "✗ La colonne n'existe pas. Application de la migration..."
echo ""

# Exécuter la migration
mysql -h "$DB_HOST" -P "$DB_PORT" -u "$DB_USER" -p"$DB_PASS" --skip-ssl "$DB_NAME" <<EOF

-- Step 1: Add the column
ALTER TABLE UserQuizResults 
ADD COLUMN SelectedOptionId INT NOT NULL DEFAULT 0;

-- Step 2: Create an index
CREATE INDEX IX_UserQuizResults_SelectedOptionId 
ON UserQuizResults(SelectedOptionId);

-- Step 3: Add foreign key constraint
ALTER TABLE UserQuizResults 
ADD CONSTRAINT FK_UserQuizResults_QuizOptions_SelectedOptionId 
FOREIGN KEY (SelectedOptionId) REFERENCES QuizOptions(Id);

-- Step 4: Clean up invalid data
DELETE FROM UserQuizResults WHERE SelectedOptionId = 0;

EOF

if [ $? -eq 0 ]; then
    echo ""
    echo "✓ Migration appliquée avec succès!"
    echo ""
    echo "Vérification..."
    mysql -h "$DB_HOST" -P "$DB_PORT" -u "$DB_USER" -p"$DB_PASS" --skip-ssl "$DB_NAME" -e "
    SELECT 
        COLUMN_NAME, 
        COLUMN_TYPE, 
        IS_NULLABLE, 
        COLUMN_DEFAULT,
        COLUMN_KEY
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = '$DB_NAME' 
      AND TABLE_NAME = 'UserQuizResults' 
      AND COLUMN_NAME = 'SelectedOptionId';
    "
    echo ""
    echo "✓ Migration terminée. Redémarrez l'application sur le serveur."
else
    echo ""
    echo "✗ Erreur lors de l'application de la migration."
    exit 1
fi
