-- Migration: AddSelectedOptionIdToUserQuizResult
-- Ajoute le champ SelectedOptionId à la table UserQuizResults

-- Ajouter la colonne
ALTER TABLE UserQuizResults 
ADD SelectedOptionId INT NOT NULL DEFAULT 0;

-- Créer l'index
CREATE INDEX IX_UserQuizResults_SelectedOptionId 
ON UserQuizResults(SelectedOptionId);

-- Ajouter la clé étrangère
ALTER TABLE UserQuizResults
ADD CONSTRAINT FK_UserQuizResults_QuizOptions_SelectedOptionId
FOREIGN KEY (SelectedOptionId) REFERENCES QuizOptions(Id);
