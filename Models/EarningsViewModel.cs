using CrudDemo.Models;

namespace CrudDemo.Models
{
    /// <summary>
    /// ViewModel pour la page de gains mensuels d'un utilisateur (/Courses/Earnings).
    /// Encapsule le résultat du calcul courant et l'historique des mois précédents.
    /// </summary>
    public class EarningsViewModel
    {
        // ─────────────────────────────────────────────
        // Données du mois en cours
        // ─────────────────────────────────────────────

        /// <summary>
        /// Mois courant (1–12).
        /// </summary>
        public int CurrentMonth { get; set; }

        /// <summary>
        /// Année courante.
        /// </summary>
        public int CurrentYear { get; set; }

        /// <summary>
        /// Libellé du mois courant (ex : « Avril 2026 »).
        /// </summary>
        public string CurrentMonthLabel { get; set; } = string.Empty;

        /// <summary>
        /// Nombre de leçons terminées par l'utilisateur ce mois-ci
        /// (LessonEngagement.IsActive = true, EngagedAt dans le mois courant).
        /// </summary>
        public int LessonsCompleted { get; set; }

        /// <summary>
        /// Nombre total de leçons disponibles sur la plateforme.
        /// Utilisé comme dénominateur dans la formule.
        /// </summary>
        public int TotalLessonsOnPlatform { get; set; }

        /// <summary>
        /// Montant gagné ce mois-ci, en euros, calculé et persisté.
        /// Formule : Math.Min((LessonsCompleted / TotalLessonsOnPlatform) * 5.0, 15.0)
        /// </summary>
        public decimal EarnedAmount { get; set; }

        /// <summary>
        /// Gain maximum autorisé par mois (fixé à 15,00 €).
        /// </summary>
        public decimal MaxMonthlyGain { get; } = 15m;

        /// <summary>
        /// Pourcentage de remplissage de la barre de progression par rapport au gain max (0–100).
        /// </summary>
        public double ProgressPercentage =>
            MaxMonthlyGain == 0m
                ? 0d
                : Math.Min(100d, Math.Round((double)(EarnedAmount / MaxMonthlyGain) * 100d, 2));

        /// <summary>
        /// Pourcentage de leçons terminées par rapport au total (0–100).
        /// </summary>
        public double LessonsProgressPercentage =>
            TotalLessonsOnPlatform == 0
                ? 0d
                : Math.Min(100d, Math.Round((double)LessonsCompleted / TotalLessonsOnPlatform * 100d, 2));

        /// <summary>
        /// Indique si l'utilisateur a atteint le gain maximum du mois.
        /// </summary>
        public bool HasReachedMaxGain => EarnedAmount >= MaxMonthlyGain;

        // ─────────────────────────────────────────────
        // Historique (12 derniers mois)
        // ─────────────────────────────────────────────

        /// <summary>
        /// Historique persisté des gains mensuels (trié du plus récent au plus ancien).
        /// </summary>
        public List<MonthlyEarning> History { get; set; } = new List<MonthlyEarning>();

        /// <summary>
        /// Total des gains cumulés sur tous les mois persistés (leçons uniquement).
        /// </summary>
        public decimal TotalEarned => History.Sum(h => h.EarnedAmount);

        // ─────────────────────────────────────────────
        // Missions rémunérées
        // ─────────────────────────────────────────────

        /// <summary>
        /// Missions actives disponibles sur la plateforme avec l'état de completion de l'utilisateur.
        /// </summary>
        public List<MissionStatus> Missions { get; set; } = new List<MissionStatus>();

        /// <summary>
        /// Total des récompenses approuvées provenant des missions.
        /// </summary>
        public decimal TotalMissionRewards => Missions
            .Where(m => m.CompletionStatus == "approved")
            .Sum(m => m.RewardAwarded);

        /// <summary>
        /// Nombre de missions en attente de validation admin.
        /// </summary>
        public int PendingMissionsCount => Missions.Count(m => m.CompletionStatus == "pending");

        /// <summary>
        /// Grand total : gains leçons + gains missions approuvées.
        /// </summary>
        public decimal GrandTotal => TotalEarned + TotalMissionRewards;
    }

    /// <summary>
    /// Représente une mission avec l'état de complétion de l'utilisateur courant.
    /// </summary>
    public class MissionStatus
    {
        public int MissionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal RewardAmount { get; set; }
        public bool RequiresAdminValidation { get; set; }
        public DateTime? EndsAt { get; set; }

        /// <summary>null = non soumise, "pending", "approved", "rejected"</summary>
        public string? CompletionStatus { get; set; }

        /// <summary>Montant reçu si approuvée, sinon 0.</summary>
        public decimal RewardAwarded { get; set; }

        /// <summary>Id de la complétion (pour l'affichage de la preuve soumise).</summary>
        public int? CompletionId { get; set; }

        /// <summary>Preuve soumise par l'utilisateur.</summary>
        public string? ProofNote { get; set; }

        /// <summary>Note admin (refus ou approbation).</summary>
        public string? AdminNote { get; set; }

        public bool IsSubmitted => CompletionStatus != null;
        public bool IsApproved  => CompletionStatus == "approved";
        public bool IsPending   => CompletionStatus == "pending";
        public bool IsRejected  => CompletionStatus == "rejected";

        /// <summary>Vrai si la mission est expirée (date de fin dépassée).</summary>
        public bool IsExpired => EndsAt.HasValue && EndsAt.Value < DateTime.UtcNow;
    }
}
