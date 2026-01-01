-- Migration: Add SelectedOptionId column to UserQuizResults table
-- Date: 2026-01-01
-- Description: Adds the SelectedOptionId column to track which option the user selected in quiz results

-- Step 1: Add the column
ALTER TABLE UserQuizResults 
ADD COLUMN SelectedOptionId INT NOT NULL DEFAULT 0;

-- Step 2: Create an index for better performance
CREATE INDEX IX_UserQuizResults_SelectedOptionId 
ON UserQuizResults(SelectedOptionId);

-- Step 3: Add foreign key constraint
ALTER TABLE UserQuizResults 
ADD CONSTRAINT FK_UserQuizResults_QuizOptions_SelectedOptionId 
FOREIGN KEY (SelectedOptionId) REFERENCES QuizOptions(Id);

-- Step 4: Clean up invalid data (optional - records with SelectedOptionId = 0)
DELETE FROM UserQuizResults WHERE SelectedOptionId = 0;

-- Verify the changes
SELECT 
    COLUMN_NAME, 
    COLUMN_TYPE, 
    IS_NULLABLE, 
    COLUMN_DEFAULT,
    COLUMN_KEY
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = 'NomDeTaBase' 
  AND TABLE_NAME = 'UserQuizResults' 
  AND COLUMN_NAME = 'SelectedOptionId';
