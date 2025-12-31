-- ============================================
-- NETTOYAGE COMPLET ET CRÉATION PROPRE
-- ============================================

-- 1. SUPPRIMER tous les anciens cours de hacking
DELETE FROM Courses WHERE Id IN (4, 5, 6, 7);
DELETE FROM Courses WHERE Title LIKE '%Hacking%';
DELETE FROM Courses WHERE Title LIKE '%Cybersécurité%';

-- 2. CRÉER le cours principal
INSERT INTO Courses (Title, Description, IsFree, CreatedBy, CreatedAt, UpdatedAt)
VALUES (
    'Formation Complète - Hacking Éthique et Cybersécurité',
    'Maîtrisez le hacking éthique de A à Z : reconnaissance, exploitation, OSINT, programmation et plus encore.',
    0,
    'Admin',
    UTC_TIMESTAMP(),
    UTC_TIMESTAMP()
);

SET @courseId = LAST_INSERT_ID();

-- ============================================
-- 3. CRÉER TOUS LES MODULES (chaque ligne = 1 module)
-- ============================================
INSERT INTO Modules (CourseId, Title, Description, OrderIndex, CreatedAt) VALUES
-- Introduction au hacking (module titre)
(@courseId, 'Introduction au hacking', 'Section d''introduction au hacking éthique', 1, UTC_TIMESTAMP()),
(@courseId, 'Comprendre le Hacking', 'Les concepts fondamentaux du hacking', 2, UTC_TIMESTAMP()),
(@courseId, 'Introduction au Hacking', 'Vue d''ensemble du monde du hacking', 3, UTC_TIMESTAMP()),
(@courseId, 'Comprendre les failles', 'Identifier les vulnérabilités', 4, UTC_TIMESTAMP()),
(@courseId, 'Les bases de la cyber à connaitre', 'Fondamentaux de cybersécurité', 5, UTC_TIMESTAMP()),
(@courseId, 'Preparation avant de commencer nos exercices', 'Setup de l''environnement', 6, UTC_TIMESTAMP()),
(@courseId, 'Prerequis quiz', 'Test de connaissances', 7, UTC_TIMESTAMP()),
(@courseId, 'Les termes et technique du hacking', 'Vocabulaire technique', 8, UTC_TIMESTAMP()),
(@courseId, 'Les metiers cyber', 'Les carrières en cybersécurité', 9, UTC_TIMESTAMP()),

-- Cryptanalyse (module titre)
(@courseId, 'Cryptanalyse', 'Techniques de cryptographie', 10, UTC_TIMESTAMP()),
(@courseId, 'Chiffrement, encodage et hashage', 'Comprendre les différences', 11, UTC_TIMESTAMP()),
(@courseId, 'Exercice (encodage)', 'Pratique encodage', 12, UTC_TIMESTAMP()),

-- Débuté en hacking (module titre)
(@courseId, 'Débuté en hacking', 'Premiers pas pratiques', 13, UTC_TIMESTAMP()),
(@courseId, 'Les outils qu''il faut avoir', 'Boîte à outils hacker', 14, UTC_TIMESTAMP()),
(@courseId, 'C''est le moment de jouer', 'Exercices pratiques', 15, UTC_TIMESTAMP()),
(@courseId, 'SSH Accès à un serveur à distance', 'Maîtriser SSH', 16, UTC_TIMESTAMP()),
(@courseId, 'Exercice SSH', 'Pratique SSH', 17, UTC_TIMESTAMP()),
(@courseId, 'Première technique d''attaque Exercice travaux pratique', 'Première attaque', 18, UTC_TIMESTAMP()),
(@courseId, 'wordlist pour brute force et fuzzing', 'Utilisation de wordlists', 19, UTC_TIMESTAMP()),
(@courseId, 'hydra brute force', 'Outil Hydra', 20, UTC_TIMESTAMP()),
(@courseId, 'Exercice hydra ssh', 'Pratique Hydra', 21, UTC_TIMESTAMP()),
(@courseId, 'Activer les fonctions caché de votre téléphone', 'Fonctionnalités mobiles', 22, UTC_TIMESTAMP()),

-- Phase de reconnaissance (module titre)
(@courseId, 'Phase de reconnaissance', 'Reconnaissance réseau', 23, UTC_TIMESTAMP()),
(@courseId, 'Nmap scan réseaux', 'Scanner avec Nmap', 24, UTC_TIMESTAMP()),
(@courseId, 'Nmap avancé script NSE Débuté en hacking', 'Nmap avancé', 25, UTC_TIMESTAMP()),
(@courseId, 'Exercice nmap detect login anonymous', 'Détection Nmap', 26, UTC_TIMESTAMP()),
(@courseId, 'Curl lancer et modifier des requêtes via le terminal', 'Maîtriser Curl', 27, UTC_TIMESTAMP()),

-- Hacking de données (module titre)
(@courseId, 'Hacking de données', 'Exploitation de données', 28, UTC_TIMESTAMP()),
(@courseId, 'API (Application Programming Interface)', 'Exploiter les APIs', 29, UTC_TIMESTAMP()),
(@courseId, 'google hacking dork', 'Google Dorks', 30, UTC_TIMESTAMP()),

-- OSINT (module titre)
(@courseId, 'OSINT', 'Renseignement sources ouvertes', 31, UTC_TIMESTAMP()),
(@courseId, 'OSINT Renseignement d''origine sources ouvertes', 'Techniques OSINT', 32, UTC_TIMESTAMP()),
(@courseId, 'Exercice OSINT', 'Pratique OSINT', 33, UTC_TIMESTAMP()),

-- Astuce de hacker (module titre)
(@courseId, 'Astuce de hacker', 'Astuces pratiques', 34, UTC_TIMESTAMP()),
(@courseId, 'Comment obtenir des livres et autre documents gratuitement', 'Ressources gratuites', 35, UTC_TIMESTAMP()),

-- hack site web (module titre)
(@courseId, 'hack site web (sqli, xss, commande injection, exploit...)', 'Exploitation web', 36, UTC_TIMESTAMP()),
(@courseId, 'Bypass des page de connexion — SQL Injection 101', 'SQL Injection', 37, UTC_TIMESTAMP()),
(@courseId, 'Exercice SQLI', 'Pratique SQLI', 38, UTC_TIMESTAMP()),
(@courseId, 'sqlmap semi automatisation sqli', 'SQLmap', 39, UTC_TIMESTAMP()),

-- Metasploit (module titre)
(@courseId, 'Metasploit recon -> exploit', 'Framework Metasploit', 40, UTC_TIMESTAMP()),
(@courseId, 'Introduction à Metasploit', 'Débuter avec Metasploit', 41, UTC_TIMESTAMP()),
(@courseId, 'scan d''un site web avec metasploit', 'Scan Metasploit', 42, UTC_TIMESTAMP()),
(@courseId, 'Exercice : Attaque d''un site WordPress avec Metasploit (module XML-RPC)', 'Exploit WordPress', 43, UTC_TIMESTAMP()),
(@courseId, 'scan wrdpress avec Metasploit', 'Scan WordPress', 44, UTC_TIMESTAMP()),
(@courseId, 'Fichier .rc pour automatiser nos attaques', 'Automatisation', 45, UTC_TIMESTAMP()),
(@courseId, 'hacking android 101', 'Hacking mobile', 46, UTC_TIMESTAMP()),

-- Réseaux (module titre)
(@courseId, 'Réseaux', 'Sécurité réseaux', 47, UTC_TIMESTAMP()),
(@courseId, 'Exercice airckrack-ng attaque wifi', 'Crack WiFi', 48, UTC_TIMESTAMP()),
(@courseId, 'wireshark capture réseaux', 'Wireshark', 49, UTC_TIMESTAMP()),
(@courseId, 'L''attaque de l''homme du milieu MITM (bettercap)', 'Attaque MITM', 50, UTC_TIMESTAMP()),
(@courseId, 'Exercice L''attaque de l''homme du milieu MITM', 'Pratique MITM', 51, UTC_TIMESTAMP()),
(@courseId, 'Exercice TP Wireshark et Betterkap l''association ultime !', 'TP Wireshark', 52, UTC_TIMESTAMP()),

-- Programmation (module titre)
(@courseId, 'Programmation pour le Hacking', 'Coder vos outils', 53, UTC_TIMESTAMP()),
(@courseId, 'Introduction à JavaScript pour le Hacking XSS', 'JavaScript hacking', 54, UTC_TIMESTAMP()),
(@courseId, 'XSS réfléchi (Reflected XSS)', 'XSS réfléchi', 55, UTC_TIMESTAMP()),
(@courseId, 'Introduction à Python pour le Hacking', 'Python hacking', 56, UTC_TIMESTAMP()),
(@courseId, 'Exercice script python 101', 'Scripts Python', 57, UTC_TIMESTAMP()),

-- Analyste SOC (module titre)
(@courseId, 'Analyste SOC', 'Analyste SOC', 58, UTC_TIMESTAMP()),
(@courseId, 'SOC pour analyser et qualifier les événements de sécurité en temps réel', 'Analyse SOC', 59, UTC_TIMESTAMP()),
(@courseId, 'Automatisation de pentest', 'Automatisation', 60, UTC_TIMESTAMP());

-- ============================================
-- 4. CRÉER UNE LEÇON PAR MODULE
-- ============================================
INSERT INTO Lessons (ModuleId, Title, Description, OrderIndex, CreatedAt)
SELECT 
    m.Id,
    CONCAT('Leçon : ', m.Title),
    CONCAT('Contenu du module ', m.Title),
    1,
    UTC_TIMESTAMP()
FROM Modules m
WHERE m.CourseId = @courseId;

-- ============================================
-- 5. RÉSUMÉ FINAL
-- ============================================
SELECT 
    '✅ COURS CRÉÉ AVEC SUCCÈS!' as Statut,
    c.Id as CourseId,
    c.Title as Titre,
    COUNT(DISTINCT m.Id) as Modules,
    COUNT(l.Id) as Lecons
FROM Courses c
LEFT JOIN Modules m ON c.Id = m.CourseId
LEFT JOIN Lessons l ON m.Id = l.ModuleId
WHERE c.Id = @courseId
GROUP BY c.Id;

-- Afficher les 10 premiers modules
SELECT 
    CONCAT('Module #', m.OrderIndex) as Numero,
    m.Title as Titre,
    CONCAT('/Courses/Details/', c.Id) as URL_Cours
FROM Modules m
JOIN Courses c ON m.CourseId = c.Id
WHERE c.Id = @courseId
ORDER BY m.OrderIndex
LIMIT 10;
