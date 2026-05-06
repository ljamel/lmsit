using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrudDemo.Models
{
    /// <summary>
    /// Représente les gains mensuels calculés d'un utilisateur.
    /// Formule : gain = Math.Min((LessonsCompleted / (double)TotalLessonsForMonth) * 5.0, 15.0)
    /// Le gain max est de 15 € par mois.
    /// </summary>
    public class MonthlyEarning
    {
        public int Id { get; set; }

        /// <summary>
        /// Identifiant de l'utilisateur (AspNetUsers.Id).
        /// </summary>
        [Required]
        public required string UserId { get; set; }

        /// <summary>
        /// Mois concerné (1-12).
        /// </summary>
        [Required]
        [Range(1, 12)]
        public int Month { get; set; }

        /// <summary>
        /// Année concernée (ex: 2026).
        /// </summary>
        [Required]
        public int Year { get; set; }

        /// <summary>
        /// Nombre de leçons terminées par l'utilisateur au cours du mois.
        /// Une leçon est considérée terminée lorsqu'un LessonEngagement actif (IsActive=true)
        /// existe pour cet utilisateur et ce mois.
        /// </summary>
        public int LessonsCompleted { get; set; } = 0;

        /// <summary>
        /// Nombre total de leçons disponibles sur la plateforme au moment du calcul.
        /// Utilisé comme dénominateur dans la formule.
        /// </summary>
        public int TotalLessonsForMonth { get; set; } = 0;

        /// <summary>
        /// Montant calculé en euros (max 15,00 €).
        /// Formule : Math.Min((LessonsCompleted / TotalLessonsForMonth) * 5.0, 15.0)
        /// Stocké en base pour persistance.
        /// </summary>
        [Column(TypeName = "decimal(10,2)")]
        public decimal EarnedAmount { get; set; } = 0m;

        /// <summary>
        /// Date et heure du dernier calcul (UTC).
        /// </summary>
        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    }
}
