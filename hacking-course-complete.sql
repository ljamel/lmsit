-- Script SQL pour créer le cours complet de Hacking Éthique
-- Structure: 1 Cours -> 7 Modules (affichés dans Details)

-- Supprimer toutes les données
DELETE FROM Lessons;
DELETE FROM Modules;
DELETE FROM Courses;

-- Créer le cours principal
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt) 
VALUES ('Hacking Éthique et Sécurité Informatique', 
        'Formation complète en sécurité informatique et hacking éthique, couvrant tous les aspects de la cybersécurité moderne.',
        FALSE,
        'Admin', 
        NOW(),
        NOW());

SET @CourseId = LAST_INSERT_ID();

-- ============================================
-- MODULE 1: Introduction
-- ============================================
INSERT INTO Modules (Title, Description, CourseId, OrderIndex, CreatedAt) 
VALUES ('Introduction', 'Introduction au hacking éthique et à la sécurité informatique', @CourseId, 1, NOW());

-- ============================================
-- MODULE 2: Réseaux
-- ============================================
INSERT INTO Modules (Title, Description, CourseId, OrderIndex, CreatedAt) 
VALUES ('Réseaux', 'Sécurité des réseaux et attaques réseau', @CourseId, 2, NOW());

-- ============================================
-- MODULE 3: Programmation Python
-- ============================================
INSERT INTO Modules (Title, Description, CourseId, OrderIndex, CreatedAt) 
VALUES ('Programmation Python', 'Python pour le hacking et l automatisation', @CourseId, 3, NOW());

-- ============================================
-- MODULE 4: Le web et ses vulnérabilités
-- ============================================
INSERT INTO Modules (Title, Description, CourseId, OrderIndex, CreatedAt) 
VALUES ('Le web et ses vulnérabilités', 'Sécurité des applications web', @CourseId, 4, NOW());

-- ============================================
-- MODULE 5: Hacking avancé
-- ============================================
INSERT INTO Modules (Title, Description, CourseId, OrderIndex, CreatedAt) 
VALUES ('Hacking avancé', 'Techniques avancées de hacking', @CourseId, 5, NOW());

-- ============================================
-- MODULE 6: Exploitation de vulnérabilités
-- ============================================
INSERT INTO Modules (Title, Description, CourseId, OrderIndex, CreatedAt) 
VALUES ('Exploitation de vulnérabilités', 'Exploitation et post-exploitation', @CourseId, 6, NOW());

-- ============================================
-- MODULE 7: Sécurité défensive
-- ============================================
INSERT INTO Modules (Title, Description, CourseId, OrderIndex, CreatedAt) 
VALUES ('Sécurité défensive', 'Protection et défense des systèmes', @CourseId, 7, NOW());

-- Vérification
SELECT 
    c.Title AS Cours,
    COUNT(m.Id) AS NombreModules
FROM Courses c
LEFT JOIN Modules m ON c.Id = m.CourseId
GROUP BY c.Id;

SELECT m.OrderIndex, m.Title AS Module
FROM Modules m
ORDER BY m.OrderIndex;
