-- Migration: Add Mattermost fields to Subscriptions table
-- Date: 2026-01-13

USE `NomDeTaBase`; -- Remplacez par le nom de votre base de données

ALTER TABLE `Subscriptions` 
ADD COLUMN `MattermostUserId` longtext NULL,
ADD COLUMN `MattermostCreatedAt` datetime(6) NULL;

-- Vérification
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Subscriptions' 
  AND COLUMN_NAME IN ('MattermostUserId', 'MattermostCreatedAt');
