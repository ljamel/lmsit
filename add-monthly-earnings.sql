-- ============================================================
-- Migration : AddMonthlyEarnings
-- Date      : 2026-04-28
-- Auteur    : généré automatiquement
-- ============================================================
-- Applique cette migration sur la base NomDeTaBase après
-- avoir redémarré le serveur MariaDB.
-- Commande : mysql -h 127.0.0.1 -P 3307 -u root -p NomDeTaBase < add-monthly-earnings.sql
-- ============================================================

CREATE TABLE IF NOT EXISTS `MonthlyEarnings` (
    `Id`                   INT           NOT NULL AUTO_INCREMENT,
    `UserId`               VARCHAR(255)  NOT NULL,
    `Month`                INT           NOT NULL,
    `Year`                 INT           NOT NULL,
    `LessonsCompleted`     INT           NOT NULL DEFAULT 0,
    `TotalLessonsForMonth` INT           NOT NULL DEFAULT 0,
    `EarnedAmount`         DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    `CalculatedAt`         DATETIME(6)   NOT NULL,
    PRIMARY KEY (`Id`)
) CHARACTER SET utf8mb4;

-- Index unique : un seul enregistrement par utilisateur / mois / année
CREATE UNIQUE INDEX IF NOT EXISTS `UX_MonthlyEarnings_UserId_Year_Month`
    ON `MonthlyEarnings` (`UserId`, `Year`, `Month`);

-- Index pour accéder rapidement à l'historique annuel d'un utilisateur
CREATE INDEX IF NOT EXISTS `IX_MonthlyEarnings_UserId_Year`
    ON `MonthlyEarnings` (`UserId`, `Year`);

-- Enregistre cette migration dans l'historique EF Core
INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260428000001_AddMonthlyEarnings', '8.0.2');
