# Suppression du Prix des Cours - Documentation des Changements

## Vue d'ensemble
La propriété `Price` a été supprimée du modèle `Course`. Désormais, les cours sont simplement marqués comme **gratuits** ou **payants** via la propriété `IsFree`.

## Prix fixe pour les cours payants
- **Prix d'inscription : 10,00 €** (défini lors du paiement)
- Ce prix est fixe et géré au niveau de l'inscription/paiement, pas au niveau du cours lui-même

## Modifications effectuées

### 1. Modèle de données
**Fichier : `Models/Course.cs`**
- ❌ Supprimé : `public decimal Price { get; set; } = 0;`
- ✅ Conservé : `public bool IsFree { get; set; } = true;`
  - `true` = cours gratuit
  - `false` = cours payant (10€)

### 2. Migration de base de données
**Migration créée : `20251231100312_RemovePriceFromCourse.cs`**
- Supprime la colonne `Price` de la table `Courses`
- Migration appliquée avec succès : `dotnet ef database update`

### 3. Vues modifiées

#### `Views/AdminCourses/Create.cshtml`
- Simplifié le formulaire pour n'avoir qu'un switch "Cours gratuit"
- Retiré le champ de saisie du prix

#### `Views/Payment/Checkout.cshtml`
- Prix affiché en dur : **10,00 €**
- Plus de référence à `@Model.Price`

### 4. Contrôleurs modifiés

#### `Controllers/PaymentController.cs`
- **Ligne ~175** : `UnitAmount = 1000` (10€ en centimes) au lieu de `course.Price * 100`
- **Ligne ~195** : `Amount = 10.00m` au lieu de `course.Price`

### 5. Données de test (SeedData)
**Fichier : `Data/SeedData.cs`**
- Retiré toutes les références à `Price = 0`
- Les cours sont maintenant créés avec seulement `IsFree = true/false`

### 6. Fichiers SQL mis à jour
Tous les scripts SQL ont été mis à jour pour retirer la colonne `Price` :

| Fichier | Changement |
|---------|-----------|
| `add-hacking-course.sql` | Retiré `Price` des INSERT |
| `add-hacking-course-v2.sql` | Retiré `Price` des INSERT |
| `cleanup-and-create-hacking-course.sql` | Retiré `Price` des INSERT |
| `create-60-separate-courses.sql` | Retiré `Price` de tous les INSERT (60 cours) |
| `hacking-course-complete.sql` | Retiré `Price` des INSERT |
| `init-database.sql` | Retiré colonne `Price` de CREATE TABLE + INSERT |
| `apply-indexes.sql` | Retiré `Price` de l'index INCLUDE |

**Nouveau script créé :** `update-remove-price-column.sql`
```sql
ALTER TABLE Courses DROP COLUMN IF EXISTS Price;
```

## Compilation et Tests
✅ **Build réussi** : `dotnet build` - 0 erreurs, 0 warnings
✅ **Migration appliquée** avec succès
✅ **Base de données mise à jour**

## Comment utiliser maintenant

### Créer un cours gratuit
```csharp
var course = new Course
{
    Title = "Mon cours gratuit",
    Description = "Description...",
    IsFree = true,  // ← Cours gratuit
    CreatedBy = "admin@example.com"
};
```

### Créer un cours payant
```csharp
var course = new Course
{
    Title = "Mon cours payant",
    Description = "Description...",
    IsFree = false,  // ← Cours payant (10€)
    CreatedBy = "admin@example.com"
};
```

### Vérifier si un cours est gratuit ou payant
```csharp
if (course.IsFree)
{
    // Accès gratuit
}
else
{
    // Rediriger vers paiement (10€)
}
```

## Avantages de ce changement

1. ✅ **Simplicité** : Plus besoin de gérer les prix individuels
2. ✅ **Cohérence** : Un seul prix pour tous les cours payants
3. ✅ **Facilité de maintenance** : Changement du prix en un seul endroit (PaymentController)
4. ✅ **Moins d'erreurs** : Pas de risque de prix incohérents dans la base de données

## Notes importantes

- Le prix fixe (10€) est défini dans `PaymentController.cs`
- Pour modifier le prix, changez les valeurs dans `PaymentController` :
  - `UnitAmount = 1000` (prix en centimes)
  - `Amount = 10.00m` (prix en euros)
- Les anciennes migrations avec `Price` sont conservées pour l'historique
- La nouvelle migration `RemovePriceFromCourse` supprime définitivement la colonne
