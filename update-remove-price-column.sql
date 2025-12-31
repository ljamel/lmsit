-- ============================================
-- Script pour supprimer la colonne Price de la table Courses
-- La colonne IsFree est conservée pour indiquer si un cours est gratuit ou payant
-- Le prix est maintenant défini lors de l'inscription (fixé à 10€)
-- ============================================

-- Supprimer la colonne Price si elle existe
ALTER TABLE Courses DROP COLUMN IF EXISTS Price;

-- Vérifier la structure de la table
DESCRIBE Courses;
