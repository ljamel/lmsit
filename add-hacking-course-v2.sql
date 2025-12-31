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

-- Créer tous les modules (chaque tiret = 1 module)
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES
(@courseId, 'Introduction au hacking', 'Découvrez les bases du hacking éthique', 1, UTC_TIMESTAMP()),
(@courseId, 'Comprendre le Hacking', 'Introduction aux concepts fondamentaux', 2, UTC_TIMESTAMP()),
(@courseId, 'Introduction au Hacking', 'Vue d''ensemble du monde du hacking', 3, UTC_TIMESTAMP()),
(@courseId, 'Comprendre les failles', 'Identifier et comprendre les failles de sécurité', 4, UTC_TIMESTAMP()),
(@courseId, 'Les bases de la cyber à connaitre', 'Fondamentaux essentiels de la cybersécurité', 5, UTC_TIMESTAMP()),
(@courseId, 'Preparation avant de commencer nos exercices', 'Configuration de votre environnement', 6, UTC_TIMESTAMP()),
(@courseId, 'Prerequis quiz', 'Testez vos connaissances de base', 7, UTC_TIMESTAMP()),
(@courseId, 'Les termes et technique du hacking', 'Vocabulaire et techniques essentielles', 8, UTC_TIMESTAMP()),
(@courseId, 'Les metiers cyber', 'Découvrez les carrières en cybersécurité', 9, UTC_TIMESTAMP()),
(@courseId, 'Cryptanalyse', 'Maîtrisez les techniques de cryptographie', 10, UTC_TIMESTAMP()),
(@courseId, 'Chiffrement, encodage et hashage', 'Comprendre les différences', 11, UTC_TIMESTAMP()),
(@courseId, 'Exercice (encodage)', 'Pratique d''encodage et décodage', 12, UTC_TIMESTAMP()),
(@courseId, 'Débuté en hacking', 'Premiers pas pratiques', 13, UTC_TIMESTAMP()),
(@courseId, 'Les outils qu''il faut avoir', 'Votre boîte à outils de hacker', 14, UTC_TIMESTAMP()),
(@courseId, 'C''est le moment de jouer', 'Premiers exercices pratiques', 15, UTC_TIMESTAMP()),
(@courseId, 'SSH Accès à un serveur à distance', 'Maîtriser le protocole SSH', 16, UTC_TIMESTAMP()),
(@courseId, 'Exercice SSH', 'Pratique SSH', 17, UTC_TIMESTAMP()),
(@courseId, 'Première technique d''attaque Exercice travaux pratique', 'Votre première attaque éthique', 18, UTC_TIMESTAMP()),
(@courseId, 'wordlist pour brute force et fuzzing', 'Utilisation de listes de mots', 19, UTC_TIMESTAMP()),
(@courseId, 'hydra brute force', 'Maîtriser l''outil Hydra', 20, UTC_TIMESTAMP()),
(@courseId, 'Exercice hydra ssh', 'Pratique Hydra sur SSH', 21, UTC_TIMESTAMP()),
(@courseId, 'Activer les fonctions caché de votre téléphone', 'Fonctionnalités avancées mobile', 22, UTC_TIMESTAMP()),
(@courseId, 'Phase de reconnaissance', 'Techniques de reconnaissance', 23, UTC_TIMESTAMP()),
(@courseId, 'Nmap scan réseaux', 'Scanner les réseaux avec Nmap', 24, UTC_TIMESTAMP()),
(@courseId, 'Nmap avancé script NSE Débuté en hacking', 'Utilisation avancée de Nmap', 25, UTC_TIMESTAMP()),
(@courseId, 'Exercice nmap detect login anonymous', 'Détection de connexions anonymes', 26, UTC_TIMESTAMP()),
(@courseId, 'Curl lancer et modifier des requêtes via le terminal', 'Maîtriser Curl', 27, UTC_TIMESTAMP()),
(@courseId, 'Hacking de données', 'Extraction et exploitation de données', 28, UTC_TIMESTAMP()),
(@courseId, 'API (Application Programming Interface)', 'Comprendre et exploiter les APIs', 29, UTC_TIMESTAMP()),
(@courseId, 'google hacking dork', 'Techniques avancées avec Google Dorks', 30, UTC_TIMESTAMP()),
(@courseId, 'OSINT', 'Renseignement d''origine sources ouvertes', 31, UTC_TIMESTAMP()),
(@courseId, 'OSINT Renseignement d''origine sources ouvertes', 'Introduction aux techniques OSINT', 32, UTC_TIMESTAMP()),
(@courseId, 'Exercice OSINT', 'Pratique OSINT', 33, UTC_TIMESTAMP()),
(@courseId, 'Astuce de hacker', 'Techniques et astuces pratiques', 34, UTC_TIMESTAMP()),
(@courseId, 'Comment obtenir des livres et autre documents gratuitement', 'Ressources gratuites légales', 35, UTC_TIMESTAMP()),
(@courseId, 'hack site web (sqli, xss, commande injection, exploit...)', 'Exploitation de vulnérabilités web', 36, UTC_TIMESTAMP()),
(@courseId, 'Bypass des page de connexion — SQL Injection 101', 'Introduction aux injections SQL', 37, UTC_TIMESTAMP()),
(@courseId, 'Exercice SQLI', 'Pratique SQL Injection', 38, UTC_TIMESTAMP()),
(@courseId, 'sqlmap semi automatisation sqli', 'Automatiser avec SQLmap', 39, UTC_TIMESTAMP()),
(@courseId, 'Metasploit recon -> exploit', 'Maîtriser Metasploit', 40, UTC_TIMESTAMP()),
(@courseId, 'Introduction à Metasploit', 'Premiers pas avec Metasploit', 41, UTC_TIMESTAMP()),
(@courseId, 'scan d''un site web avec metasploit', 'Reconnaissance avec Metasploit', 42, UTC_TIMESTAMP()),
(@courseId, 'Exercice : Attaque d''un site WordPress avec Metasploit (module XML-RPC)', 'Exploitation de WordPress', 43, UTC_TIMESTAMP()),
(@courseId, 'scan wrdpress avec Metasploit', 'Scan approfondi de WordPress', 44, UTC_TIMESTAMP()),
(@courseId, 'Fichier .rc pour automatiser nos attaques', 'Automatisation avec fichiers rc', 45, UTC_TIMESTAMP()),
(@courseId, 'hacking android 101', 'Introduction au hacking mobile', 46, UTC_TIMESTAMP()),
(@courseId, 'Réseaux', 'Hacking et sécurité des réseaux', 47, UTC_TIMESTAMP()),
(@courseId, 'Exercice airckrack-ng attaque wifi', 'Crackage de réseaux WiFi', 48, UTC_TIMESTAMP()),
(@courseId, 'wireshark capture réseaux', 'Analyse de trafic réseau', 49, UTC_TIMESTAMP()),
(@courseId, 'L''attaque de l''homme du milieu MITM (bettercap)', 'Comprendre et réaliser une attaque MITM', 50, UTC_TIMESTAMP()),
(@courseId, 'Exercice L''attaque de l''homme du milieu MITM', 'Pratique MITM', 51, UTC_TIMESTAMP()),
(@courseId, 'Exercice TP Wireshark et Betterkap l''association ultime !', 'Wireshark et Bettercap combinés', 52, UTC_TIMESTAMP()),
(@courseId, 'Programmation pour le Hacking', 'Développer vos outils de hacking', 53, UTC_TIMESTAMP()),
(@courseId, 'Introduction à JavaScript pour le Hacking XSS', 'JavaScript appliqué au hacking', 54, UTC_TIMESTAMP()),
(@courseId, 'XSS réfléchi (Reflected XSS)', 'Exploiter les XSS réfléchis', 55, UTC_TIMESTAMP()),
(@courseId, 'Introduction à Python pour le Hacking', 'Python : le langage des hackers', 56, UTC_TIMESTAMP()),
(@courseId, 'Exercice script python 101', 'Créer vos scripts de hacking', 57, UTC_TIMESTAMP()),
(@courseId, 'Analyste SOC', 'Devenir analyste SOC', 58, UTC_TIMESTAMP()),
(@courseId, 'SOC pour analyser et qualifier les événements de sécurité en temps réel', 'Rôle d''un analyste SOC', 59, UTC_TIMESTAMP()),
(@courseId, 'Automatisation de pentest', 'Automatiser vos tests d''intrusion', 60, UTC_TIMESTAMP());

-- Ajouter une leçon par défaut à chaque module
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt)
SELECT Id, CONCAT('Contenu - ', Title), CONCAT('Leçon pour le module : ', Title), 1, UTC_TIMESTAMP()
FROM Modules
WHERE CourseId = @courseId;

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
