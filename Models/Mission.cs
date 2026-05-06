using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrudDemo.Models
{
    /// <summary>
    /// Mission rémunérée créée par un administrateur.
    /// Un utilisateur qui complète une mission gagne le montant RewardAmount en euros.
    /// </summary>
    public class Mission
    {
        public int Id { get; set; }

        /// <summary>Titre court de la mission (ex : "Terminer le module Python").</summary>
        [Required]
        [MaxLength(200)]
        public required string Title { get; set; }

        /// <summary>Description détaillée expliquant ce que l'utilisateur doit faire.</summary>
        [Required]
        [MaxLength(2000)]
        public required string Description { get; set; }

        /// <summary>Récompense en euros accordée à l'utilisateur qui valide la mission.</summary>
        [Required]
        [Range(0.01, 50.00)]
        [Column(TypeName = "decimal(8,2)")]
        public decimal RewardAmount { get; set; }

        /// <summary>
        /// Nombre maximum d'utilisateurs pouvant valider cette mission.
        /// 0 = illimité.
        /// </summary>
        public int MaxCompletions { get; set; } = 0;

        /// <summary>La mission est visible et accessible aux utilisateurs.</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// La mission nécessite une validation manuelle par un admin (true)
        /// ou est accordée automatiquement lorsque l'utilisateur clique "Je l'ai faite" (false).
        /// </summary>
        public bool RequiresAdminValidation { get; set; } = true;

        /// <summary>Date de début de disponibilité (null = dès maintenant).</summary>
        public DateTime? StartsAt { get; set; }

        /// <summary>Date limite de complétion (null = pas de limite).</summary>
        public DateTime? EndsAt { get; set; }

        /// <summary>Identifiant (email) de l'administrateur qui a créé la mission.</summary>
        [MaxLength(256)]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<UserMissionCompletion> Completions { get; set; } = new List<UserMissionCompletion>();
    }

    /// <summary>
    /// Enregistrement de la complétion (ou de la demande de validation) d'une mission par un utilisateur.
    /// </summary>
    public class UserMissionCompletion
    {
        public int Id { get; set; }

        /// <summary>GUID Identity de l'utilisateur (AspNetUsers.Id).</summary>
        [Required]
        [MaxLength(450)]
        public required string UserId { get; set; }

        [Required]
        public int MissionId { get; set; }

        /// <summary>
        /// Statut de la complétion :
        ///   pending  → l'utilisateur a déclaré avoir fait la mission, en attente de validation admin
        ///   approved → l'admin a validé, la récompense est accordée
        ///   rejected → l'admin a refusé
        /// </summary>
        [MaxLength(20)]
        public string Status { get; set; } = "pending";

        /// <summary>Montant réellement accordé (copie de Mission.RewardAmount au moment de l'approbation).</summary>
        [Column(TypeName = "decimal(8,2)")]
        public decimal RewardAwarded { get; set; } = 0m;

        /// <summary>Date à laquelle l'utilisateur a soumis la demande.</summary>
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Date à laquelle l'admin a approuvé ou refusé (null = pas encore traité).</summary>
        public DateTime? ReviewedAt { get; set; }

        /// <summary>Note optionnelle laissée par l'admin lors de la revue.</summary>
        [MaxLength(500)]
        public string? AdminNote { get; set; }

        /// <summary>Preuve optionnelle soumise par l'utilisateur (URL screenshot, lien, texte libre).</summary>
        [MaxLength(1000)]
        public string? ProofNote { get; set; }

        [ForeignKey("MissionId")]
        public Mission? Mission { get; set; }
    }
}
