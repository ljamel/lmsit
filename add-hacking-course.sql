-- ============================================
-- Script SQL pour ajouter le cours de Hacking Éthique
-- Chaque tiret = 1 module (page détails séparée)
-- ============================================

-- Supprimer le cours existant s'il existe (nettoyage)
DELETE FROM Courses WHERE Title = 'Formation Hacking Éthique et Cybersécurité';

-- 1. Insérer le cours principal
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES (
    'Formation Hacking Éthique et Cybersécurité',
    'Formation complète en hacking éthique : de la reconnaissance à l''exploitation, en passant par l''analyse de sécurité, OSINT, et programmation pour le hacking.',
    0,
    'Admin',
    UTC_TIMESTAMP(),
    UTC_TIMESTAMP()
);

SET @courseId = LAST_INSERT_ID();

-- ============================================
-- MODULE 1: Introduction au hacking
-- ============================================
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt)
VALUES (@courseId, 'Introduction au hacking', 'Découvrez les bases du hacking éthique et de la cybersécurité', 1, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();

INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES
(@moduleId, 'Comprendre le Hacking', 'Introduction aux concepts fondamentaux du hacking éthique', 1, UTC_TIMESTAMP()),
(@moduleId, 'Introduction au Hacking', 'Vue d''ensemble du monde du hacking et de ses applications', 2, UTC_TIMESTAMP()),
(@moduleId, 'Comprendre les failles', 'Apprenez à identifier et comprendre les failles de sécurité', 3, UTC_TIMESTAMP()),
(@moduleId, 'Les bases de la cyber à connaître', 'Fondamentaux essentiels de la cybersécurité', 4, UTC_TIMESTAMP()),
(@moduleId, 'Preparation avant de commencer nos exercices', 'Configuration de votre environnement de travail', 5, UTC_TIMESTAMP()),
(@moduleId, 'Prerequis quiz', 'Testez vos connaissances de base', 6, UTC_TIMESTAMP()),
(@moduleId, 'Les termes et technique du hacking', 'Vocabulaire et techniques essentielles', 7, UTC_TIMESTAMP()),
(@moduleId, 'Les metiers cyber', 'Découvrez les carrières en cybersécurité', 8, UTC_TIMESTAMP());

-- ============================================
-- MODULE 2: Cryptanalyse
-- ============================================
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt)
VALUES (@courseId, 'Cryptanalyse', 'Maîtrisez les techniques de cryptographie et cryptanalyse', 2, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();

INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES
(@moduleId, 'Chiffrement, encodage et hashage', 'Comprendre les différences entre chiffrement, encodage et hashage', 1, UTC_TIMESTAMP()),
(@moduleId, 'Exercice (encodage)', 'Pratique : techniques d''encodage et de décodage', 2, UTC_TIMESTAMP());

-- ============================================
-- MODULE 3: Débuté en hacking
-- ============================================
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt)
VALUES (@courseId, 'Débuté en hacking', 'Premiers pas pratiques dans le hacking éthique', 3, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();

INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES
(@moduleId, 'Les outils qu''il faut avoir', 'Configuration de votre boîte à outils de hacker', 1, UTC_TIMESTAMP()),
(@moduleId, 'C''est le moment de jouer', 'Premiers exercices pratiques', 2, UTC_TIMESTAMP()),
(@moduleId, 'SSH Accès à un serveur à distance', 'Maîtriser le protocole SSH', 3, UTC_TIMESTAMP()),
(@moduleId, 'Exercice SSH', 'Pratique : connexion et administration via SSH', 4, UTC_TIMESTAMP()),
(@moduleId, 'Première technique d''attaque Exercice travaux pratique', 'Mise en pratique de votre première attaque éthique', 5, UTC_TIMESTAMP()),
(@moduleId, 'wordlist pour brute force et fuzzing', 'Utilisation de listes de mots pour les attaques', 6, UTC_TIMESTAMP()),
(@moduleId, 'hydra brute force', 'Maîtriser l''outil Hydra pour le brute force', 7, UTC_TIMESTAMP()),
(@moduleId, 'Exercice hydra ssh', 'Pratique : attaque brute force sur SSH avec Hydra', 8, UTC_TIMESTAMP()),
(@moduleId, 'Activer les fonctions caché de votre téléphone', 'Découvrir les fonctionnalités avancées de votre mobile', 9, UTC_TIMESTAMP());

-- ============================================
-- MODULE 4: Phase de reconnaissance
-- ============================================
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt)
VALUES (@courseId, 'Phase de reconnaissance', 'Techniques de reconnaissance et scan de réseaux', 4, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();

INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES
(@moduleId, 'Nmap scan réseaux', 'Introduction à Nmap pour scanner les réseaux', 1, UTC_TIMESTAMP()),
(@moduleId, 'Nmap avancé script NSE Débuté en hacking', 'Utilisation avancée de Nmap avec les scripts NSE', 2, UTC_TIMESTAMP()),
(@moduleId, 'Exercice nmap detect login anonymous', 'Pratique : détection de connexions anonymes avec Nmap', 3, UTC_TIMESTAMP()),
(@moduleId, 'Curl lancer et modifier des requêtes via le terminal', 'Maîtriser Curl pour manipuler les requêtes HTTP', 4, UTC_TIMESTAMP());

-- ============================================
-- MODULE 5: Hacking de données
-- ============================================
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt)
VALUES (@courseId, 'Hacking de données', 'Techniques d''extraction et d''exploitation de données', 5, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();

INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES
(@moduleId, 'API (Application Programming Interface)', 'Comprendre et exploiter les APIs', 1, UTC_TIMESTAMP()),
(@moduleId, 'google hacking dork', 'Techniques avancées de recherche avec Google Dorks', 2, UTC_TIMESTAMP());

-- ============================================
-- MODULE 6: OSINT
-- ============================================
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt)
VALUES (@courseId, 'OSINT', 'Renseignement d''origine sources ouvertes', 6, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();

INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES
(@moduleId, 'OSINT Renseignement d''origine sources ouvertes', 'Introduction aux techniques OSINT', 1, UTC_TIMESTAMP()),
(@moduleId, 'Exercice OSINT', 'Pratique : recherche et analyse OSINT', 2, UTC_TIMESTAMP());

-- ============================================
-- MODULE 7: Astuce de hacker
-- ============================================
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt)
VALUES (@courseId, 'Astuce de hacker', 'Techniques et astuces pratiques', 7, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();

INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES
(@moduleId, 'Comment obtenir des livres et autre documents gratuitement', 'Ressources légales pour l''apprentissage gratuit', 1, UTC_TIMESTAMP());

-- ============================================
-- MODULE 8: hack site web (sqli, xss, commande injection, exploit...)
-- ============================================
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt)
VALUES (@courseId, 'hack site web (sqli, xss, commande injection, exploit...)', 'Techniques d''exploitation de vulnérabilités web', 8, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();

INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES
(@moduleId, 'Bypass des page de connexion — SQL Injection 101', 'Introduction aux injections SQL', 1, UTC_TIMESTAMP()),
(@moduleId, 'Exercice SQLI', 'Pratique : exploitation de failles SQL Injection', 2, UTC_TIMESTAMP()),
(@moduleId, 'sqlmap semi automatisation sqli', 'Utilisation de SQLmap pour automatiser les injections SQL', 3, UTC_TIMESTAMP());

-- ============================================
-- MODULE 9: Metasploit recon -> exploit
-- ============================================
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt)
VALUES (@courseId, 'Metasploit recon -> exploit', 'Maîtriser le framework Metasploit', 9, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();

INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES
(@moduleId, 'Introduction à Metasploit', 'Premiers pas avec le framework Metasploit', 1, UTC_TIMESTAMP()),
(@moduleId, 'scan d''un site web avec metasploit', 'Techniques de reconnaissance avec Metasploit', 2, UTC_TIMESTAMP()),
(@moduleId, 'Exercice : Attaque d''un site WordPress avec Metasploit (module XML-RPC)', 'Pratique : exploitation de WordPress', 3, UTC_TIMESTAMP()),
(@moduleId, 'scan wrdpress avec Metasploit', 'Scan approfondi de sites WordPress', 4, UTC_TIMESTAMP()),
(@moduleId, 'Fichier .rc pour automatiser nos attaques', 'Automatisation avec les fichiers de ressources', 5, UTC_TIMESTAMP()),
(@moduleId, 'hacking android 101', 'Introduction au hacking mobile Android', 6, UTC_TIMESTAMP());

-- ============================================
-- MODULE 10: Réseaux
-- ============================================
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt)
VALUES (@courseId, 'Réseaux', 'Hacking et sécurité des réseaux', 10, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();

INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES
(@moduleId, 'Exercice airckrack-ng attaque wifi', 'Pratique : crackage de réseaux WiFi', 1, UTC_TIMESTAMP()),
(@moduleId, 'wireshark capture réseaux', 'Analyse de trafic réseau avec Wireshark', 2, UTC_TIMESTAMP()),
(@moduleId, 'L''attaque de l''homme du milieu MITM (bettercap)', 'Comprendre et réaliser une attaque MITM', 3, UTC_TIMESTAMP()),
(@moduleId, 'Exercice L''attaque de l''homme du milieu MITM', 'Pratique : mise en œuvre d''une attaque MITM', 4, UTC_TIMESTAMP()),
(@moduleId, 'Exercice TP Wireshark et Betterkap l''association ultime !', 'Combinaison de Wireshark et Bettercap', 5, UTC_TIMESTAMP());

-- ============================================
-- MODULE 11: Programmation pour le Hacking
-- ============================================
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt)
VALUES (@courseId, 'Programmation pour le Hacking', 'Développer vos propres outils de hacking', 11, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();

INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES
(@moduleId, 'Introduction à JavaScript pour le Hacking XSS', 'JavaScript appliqué au hacking web', 1, UTC_TIMESTAMP()),
(@moduleId, 'XSS réfléchi (Reflected XSS)', 'Comprendre et exploiter les XSS réfléchis', 2, UTC_TIMESTAMP()),
(@moduleId, 'Introduction à Python pour le Hacking', 'Python : le langage des hackers', 3, UTC_TIMESTAMP()),
(@moduleId, 'Exercice script python 101', 'Pratique : création de vos premiers scripts de hacking', 4, UTC_TIMESTAMP());

-- ============================================
-- MODULE 12: Analyste SOC
-- ============================================
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt)
VALUES (@courseId, 'Analyste SOC', 'Devenir analyste en centre opérationnel de sécurité', 12, UTC_TIMESTAMP());
SET @moduleId = LAST_INSERT_ID();

INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt) VALUES
(@moduleId, 'SOC pour analyser et qualifier les événements de sécurité en temps réel', 'Rôle et missions d''un analyste SOC', 1, UTC_TIMESTAMP()),
(@moduleId, 'Automatisation de pentest', 'Automatiser vos tests d''intrusion', 2, UTC_TIMESTAMP());

-- ============================================
-- Résumé de l'insertion
-- ============================================
SELECT 
    '✅ Cours de Hacking Éthique créé avec succès !' as Status,
    @courseId as CourseId,
    COUNT(DISTINCT m.Id) as NombreModules,
    COUNT(l.Id) as NombreLecons
FROM Courses c
LEFT JOIN Modules m ON c.Id = m.CourseId
LEFT JOIN Lessons l ON m.Id = l.ModuleId
WHERE c.Id = @courseId
GROUP BY c.Id;
