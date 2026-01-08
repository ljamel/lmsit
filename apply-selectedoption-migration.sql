-- Migration: AddSelectedOptionIdToUserQuizResult
-- Ajout de la colonne SelectedOptionId à UserQuizResults

ALTER TABLE `UserQuizResults` 
ADD COLUMN `SelectedOptionId` int NOT NULL DEFAULT 0;

CREATE INDEX `IX_UserQuizResults_SelectedOptionId` 
ON `UserQuizResults` (`SelectedOptionId`);

ALTER TABLE `UserQuizResults` 
ADD CONSTRAINT `FK_UserQuizResults_QuizOptions_SelectedOptionId` 
FOREIGN KEY (`SelectedOptionId`) 
REFERENCES `QuizOptions` (`Id`) 
ON DELETE RESTRICT;

-- Enregistrer la migration dans l'historique EF
INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260101000000_AddSelectedOptionIdToUserQuizResult', '9.0.0');
