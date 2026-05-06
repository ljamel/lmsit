-- ============================================================
-- Migration : AddMissions
-- Date      : 2026-04-28
-- ============================================================
-- Commande :
--   mysql --skip-ssl -h 127.0.0.1 -P 3307 -u root -p NomDeTaBase < add-missions.sql
-- ============================================================

CREATE TABLE IF NOT EXISTS `Missions` (
    `Id`                     INT           NOT NULL AUTO_INCREMENT,
    `Title`                  VARCHAR(200)  NOT NULL,
    `Description`            VARCHAR(2000) NOT NULL,
    `RewardAmount`           DECIMAL(8,2)  NOT NULL,
    `MaxCompletions`         INT           NOT NULL DEFAULT 0,
    `IsActive`               TINYINT(1)    NOT NULL DEFAULT 1,
    `RequiresAdminValidation` TINYINT(1)   NOT NULL DEFAULT 1,
    `StartsAt`               DATETIME(6)   NULL,
    `EndsAt`                 DATETIME(6)   NULL,
    `CreatedBy`              VARCHAR(256)  NOT NULL DEFAULT '',
    `CreatedAt`              DATETIME(6)   NOT NULL,
    `UpdatedAt`              DATETIME(6)   NOT NULL,
    PRIMARY KEY (`Id`)
) CHARACTER SET utf8mb4;

CREATE TABLE IF NOT EXISTS `UserMissionCompletions` (
    `Id`            INT           NOT NULL AUTO_INCREMENT,
    `UserId`        VARCHAR(450)  NOT NULL,
    `MissionId`     INT           NOT NULL,
    `Status`        VARCHAR(20)   NOT NULL DEFAULT 'pending',
    `RewardAwarded` DECIMAL(8,2)  NOT NULL DEFAULT 0.00,
    `SubmittedAt`   DATETIME(6)   NOT NULL,
    `ReviewedAt`    DATETIME(6)   NULL,
    `AdminNote`     VARCHAR(500)  NULL,
    `ProofNote`     VARCHAR(1000) NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_UserMissionCompletions_Missions_MissionId`
        FOREIGN KEY (`MissionId`) REFERENCES `Missions` (`Id`) ON DELETE CASCADE
) CHARACTER SET utf8mb4;

CREATE INDEX IF NOT EXISTS `IX_Missions_IsActive_CreatedAt`
    ON `Missions` (`IsActive`, `CreatedAt`);

CREATE UNIQUE INDEX IF NOT EXISTS `UX_UserMissionCompletions_UserId_MissionId`
    ON `UserMissionCompletions` (`UserId`, `MissionId`);

CREATE INDEX IF NOT EXISTS `IX_UserMissionCompletions_Status_SubmittedAt`
    ON `UserMissionCompletions` (`Status`, `SubmittedAt`);

INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260428000002_AddMissions', '8.0.2');
