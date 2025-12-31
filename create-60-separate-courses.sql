-- ============================================
-- CRÉATION DE 60 COURS SÉPARÉS
-- Chaque élément = 1 cours avec son propre ID
-- ============================================

-- Supprimer les anciens cours de hacking
DELETE FROM Courses WHERE Title LIKE '%Hacking%' OR Title LIKE '%Cybersécurité%' OR Title LIKE '%cyber%';

-- ============================================
-- CRÉER 60 COURS DISTINCTS
-- ============================================

-- Cours 1
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('Introduction au hacking', 'Découvrez les bases du hacking éthique', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 2
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('Comprendre le Hacking', 'Les concepts fondamentaux du hacking', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 3
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('Introduction au Hacking', 'Vue d''ensemble du monde du hacking', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 4
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('Comprendre les failles', 'Identifier et comprendre les failles de sécurité', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 5
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('Les bases de la cyber à connaitre', 'Fondamentaux essentiels de la cybersécurité', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 6
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('Preparation avant de commencer nos exercices', 'Configuration de votre environnement', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 7
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('Prerequis quiz', 'Testez vos connaissances de base', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 8
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('Les termes et technique du hacking', 'Vocabulaire et techniques essentielles', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 9
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('Les metiers cyber', 'Découvrez les carrières en cybersécurité', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 10
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('Cryptanalyse', 'Maîtrisez les techniques de cryptographie', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 11
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('Chiffrement, encodage et hashage', 'Comprendre les différences', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 12
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('Exercice (encodage)', 'Pratique d''encodage et décodage', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 13
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('Débuté en hacking', 'Premiers pas pratiques dans le hacking', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 14
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('Les outils qu''il faut avoir', 'Votre boîte à outils de hacker', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 15
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('C''est le moment de jouer', 'Premiers exercices pratiques', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 16
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('SSH Accès à un serveur à distance', 'Maîtriser le protocole SSH', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 17
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('Exercice SSH', 'Pratique avec SSH', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 18
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('Première technique d''attaque Exercice travaux pratique', 'Votre première attaque éthique', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 19
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('wordlist pour brute force et fuzzing', 'Utilisation de listes de mots', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 20
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('hydra brute force', 'Maîtriser l''outil Hydra', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 21
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('Exercice hydra ssh', 'Pratique Hydra sur SSH', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 22
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('Activer les fonctions caché de votre téléphone', 'Fonctionnalités avancées mobile', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 23
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('Phase de reconnaissance', 'Techniques de reconnaissance réseau', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 24
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('Nmap scan réseaux', 'Scanner les réseaux avec Nmap', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 25
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('Nmap avancé script NSE Débuté en hacking', 'Nmap avancé avec scripts NSE', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 26
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('Exercice nmap detect login anonymous', 'Détection avec Nmap', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 27
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('Curl lancer et modifier des requêtes via le terminal', 'Maîtriser Curl', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 28
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('Hacking de données', 'Extraction et exploitation de données', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 29
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('API (Application Programming Interface)', 'Comprendre et exploiter les APIs', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 30
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('google hacking dork', 'Techniques avancées avec Google Dorks', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 31
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('OSINT', 'Renseignement d''origine sources ouvertes', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 32
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('OSINT Renseignement d''origine sources ouvertes', 'Introduction aux techniques OSINT', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 33
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('Exercice OSINT', 'Pratique OSINT', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 34
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('Astuce de hacker', 'Techniques et astuces pratiques', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 35
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('Comment obtenir des livres et autre documents gratuitement', 'Ressources gratuites légales', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 36
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('hack site web (sqli, xss, commande injection, exploit...)', 'Exploitation de vulnérabilités web', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 37
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('Bypass des page de connexion — SQL Injection 101', 'Introduction aux injections SQL', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 38
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('Exercice SQLI', 'Pratique SQL Injection', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 39
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('sqlmap semi automatisation sqli', 'Automatiser avec SQLmap', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 40
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('Metasploit recon -> exploit', 'Maîtriser le framework Metasploit', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 41
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('Introduction à Metasploit', 'Premiers pas avec Metasploit', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 42
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('scan d''un site web avec metasploit', 'Reconnaissance avec Metasploit', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 43
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('Exercice : Attaque d''un site WordPress avec Metasploit (module XML-RPC)', 'Exploitation de WordPress', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 44
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('scan wrdpress avec Metasploit', 'Scan approfondi de WordPress', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 45
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('Fichier .rc pour automatiser nos attaques', 'Automatisation avec fichiers rc', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 46
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('hacking android 101', 'Introduction au hacking mobile Android', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 47
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('Réseaux', 'Hacking et sécurité des réseaux', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 48
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('Exercice airckrack-ng attaque wifi', 'Crackage de réseaux WiFi', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 49
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('wireshark capture réseaux', 'Analyse de trafic réseau avec Wireshark', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 50
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('L''attaque de l''homme du milieu MITM (bettercap)', 'Comprendre et réaliser une attaque MITM', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 51
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('Exercice L''attaque de l''homme du milieu MITM', 'Pratique attaque MITM', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 52
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('Exercice TP Wireshark et Betterkap l''association ultime !', 'Wireshark et Bettercap combinés', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 53
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('Programmation pour le Hacking', 'Développer vos propres outils de hacking', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 54
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('Introduction à JavaScript pour le Hacking XSS', 'JavaScript appliqué au hacking web', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 55
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('XSS réfléchi (Reflected XSS)', 'Comprendre et exploiter les XSS réfléchis', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 56
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('Introduction à Python pour le Hacking', 'Python : le langage des hackers', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 57
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('Exercice script python 101', 'Créer vos premiers scripts de hacking', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 58
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('Analyste SOC', 'Devenir analyste en centre opérationnel de sécurité', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 59
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('SOC pour analyser et qualifier les événements de sécurité en temps réel', 'Rôle et missions d''un analyste SOC', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- Cours 60
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES ('Automatisation de pentest', 'Automatiser vos tests d''intrusion', 0, 'Admin', UTC_TIMESTAMP(), UTC_TIMESTAMP());
SET @courseId = LAST_INSERT_ID();
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES (@courseId, 'Module principal', 'Contenu du cours', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES (@moduleId, 'Leçon 1', 'Contenu de la leçon', 1, UTC_TIMESTAMP());

-- ============================================
-- RÉSUMÉ FINAL
-- ============================================
SELECT '✅ 60 COURS CRÉÉS AVEC SUCCÈS!' as Statut;

SELECT 
    CONCAT('/Courses/Details/', Id) as URL,
    Title as Titre
FROM Courses
WHERE Title NOT IN ('Introduction à la Cybersécurité', 'Sécurité des Réseaux')
ORDER BY Id
LIMIT 10;
