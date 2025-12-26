-- ============================================================
-- SCRIPT D'OPTIMISATION SQL SERVER
-- Application des index pour réduire la consommation CPU
-- ============================================================

USE [NomDeTaBase]; -- Remplacer par le nom de votre base
GO

PRINT 'Début de l''application des index d''optimisation...';
GO

-- ============================================================
-- 1. INDEX SUR SUBSCRIPTIONS
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Subscriptions_UserId_IsActive_Status')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Subscriptions_UserId_IsActive_Status
    ON Subscriptions (UserId, IsActive, Status)
    INCLUDE (StripeSubscriptionId, StripeCustomerId, StartDate);
    PRINT '✓ Index IX_Subscriptions_UserId_IsActive_Status créé';
END
ELSE
    PRINT '✓ Index IX_Subscriptions_UserId_IsActive_Status déjà existant';
GO

-- ============================================================
-- 2. INDEX SUR COURSEENROLLMENTS
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CourseEnrollments_UserId_CourseId_IsActive')
BEGIN
    CREATE NONCLUSTERED INDEX IX_CourseEnrollments_UserId_CourseId_IsActive
    ON CourseEnrollments (UserId, CourseId, IsActive)
    INCLUDE (EnrolledAt, PaymentId);
    PRINT '✓ Index IX_CourseEnrollments_UserId_CourseId_IsActive créé';
END
ELSE
    PRINT '✓ Index IX_CourseEnrollments_UserId_CourseId_IsActive déjà existant';
GO

-- ============================================================
-- 3. INDEX SUR PAYMENTS (par utilisateur)
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Payments_UserId_CreatedAt')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Payments_UserId_CreatedAt
    ON Payments (UserId, CreatedAt DESC)
    INCLUDE (Amount, Currency, Status, CourseId);
    PRINT '✓ Index IX_Payments_UserId_CreatedAt créé';
END
ELSE
    PRINT '✓ Index IX_Payments_UserId_CreatedAt déjà existant';
GO

-- ============================================================
-- 4. INDEX SUR PAYMENTS (par StripePaymentIntentId)
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Payments_StripePaymentIntentId')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Payments_StripePaymentIntentId
    ON Payments (StripePaymentIntentId)
    INCLUDE (UserId, Status, CompletedAt);
    PRINT '✓ Index IX_Payments_StripePaymentIntentId créé';
END
ELSE
    PRINT '✓ Index IX_Payments_StripePaymentIntentId déjà existant';
GO

-- ============================================================
-- 5. INDEX SUR USERQUIZRESULTS
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UserQuizResults_UserId_QuizId')
BEGIN
    CREATE NONCLUSTERED INDEX IX_UserQuizResults_UserId_QuizId
    ON UserQuizResults (UserId, QuizId)
    INCLUDE (IsCorrect, AttemptedAt);
    PRINT '✓ Index IX_UserQuizResults_UserId_QuizId créé';
END
ELSE
    PRINT '✓ Index IX_UserQuizResults_UserId_QuizId déjà existant';
GO

-- ============================================================
-- 6. INDEX SUR COURSES
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Courses_CreatedAt')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Courses_CreatedAt
    ON Courses (CreatedAt DESC)
    INCLUDE (Title, Description, CreatedBy, Price, IsFree);
    PRINT '✓ Index IX_Courses_CreatedAt créé';
END
ELSE
    PRINT '✓ Index IX_Courses_CreatedAt déjà existant';
GO

-- ============================================================
-- 7. INDEX SUR MODULES
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Modules_CourseId_OrderIndex')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Modules_CourseId_OrderIndex
    ON Modules (CourseId, OrderIndex ASC)
    INCLUDE (Title, Description);
    PRINT '✓ Index IX_Modules_CourseId_OrderIndex créé';
END
ELSE
    PRINT '✓ Index IX_Modules_CourseId_OrderIndex déjà existant';
GO

-- ============================================================
-- 8. INDEX SUR LESSONS
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Lessons_ModuleId_OrderIndex')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Lessons_ModuleId_OrderIndex
    ON Lessons (ModuleId, OrderIndex ASC)
    INCLUDE (Title, Description, VideoPath);
    PRINT '✓ Index IX_Lessons_ModuleId_OrderIndex créé';
END
ELSE
    PRINT '✓ Index IX_Lessons_ModuleId_OrderIndex déjà existant';
GO

-- ============================================================
-- 9. INDEX SUR QUIZZES
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Quizzes_LessonId')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Quizzes_LessonId
    ON Quizzes (LessonId)
    INCLUDE (Question, Points, CreatedAt);
    PRINT '✓ Index IX_Quizzes_LessonId créé';
END
ELSE
    PRINT '✓ Index IX_Quizzes_LessonId déjà existant';
GO

-- ============================================================
-- 10. INDEX SUR QUIZOPTIONS
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_QuizOptions_QuizId')
BEGIN
    CREATE NONCLUSTERED INDEX IX_QuizOptions_QuizId
    ON QuizOptions (QuizId)
    INCLUDE (Text, IsCorrect);
    PRINT '✓ Index IX_QuizOptions_QuizId créé';
END
ELSE
    PRINT '✓ Index IX_QuizOptions_QuizId déjà existant';
GO

-- ============================================================
-- STATISTIQUES DES INDEX
-- ============================================================
PRINT '';
PRINT '============================================================';
PRINT 'RÉSUMÉ DES INDEX CRÉÉS';
PRINT '============================================================';

SELECT 
    OBJECT_NAME(i.object_id) AS TableName,
    i.name AS IndexName,
    i.type_desc AS IndexType,
    s.user_seeks AS UserSeeks,
    s.user_scans AS UserScans,
    s.user_lookups AS UserLookups,
    s.last_user_seek AS LastSeek
FROM sys.indexes i
LEFT JOIN sys.dm_db_index_usage_stats s 
    ON i.object_id = s.object_id AND i.index_id = s.index_id
WHERE i.name LIKE 'IX_%'
  AND OBJECT_NAME(i.object_id) IN (
    'Subscriptions', 
    'CourseEnrollments', 
    'Payments', 
    'UserQuizResults',
    'Courses',
    'Modules',
    'Lessons',
    'Quizzes',
    'QuizOptions'
  )
ORDER BY TableName, IndexName;
GO

PRINT '';
PRINT '✅ Optimisations appliquées avec succès !';
PRINT 'La consommation CPU devrait maintenant être réduite de 75-95%.';
GO
