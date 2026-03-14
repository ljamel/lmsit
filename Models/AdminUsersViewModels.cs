using System.ComponentModel.DataAnnotations;

namespace CrudDemo.Models
{
    public class AdminUserQuizResultRowViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public string? OrientationRole { get; set; }
        public int QuizId { get; set; }
        public string QuizQuestion { get; set; } = string.Empty;
        public string CourseTitle { get; set; } = string.Empty;
        public string LessonTitle { get; set; } = string.Empty;
        public int Attempts { get; set; }
        public int CorrectAnswers { get; set; }
        public double SuccessRate { get; set; }
        public DateTime? LastAttemptAt { get; set; }
        public string CompetencyLevel { get; set; } = "Non évalué";
    }

    public class AdminUserRowViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? UserName { get; set; }

        public bool HasSubscription { get; set; }
        public bool IsActiveSubscription { get; set; }
        public string? SubscriptionStatus { get; set; }
        public DateTime? SubscriptionStartDate { get; set; }
        public string? StripeSubscriptionId { get; set; }

        public int CoursesTracked { get; set; }
        public int QuizAttempts { get; set; }
        public int QuizCorrectAnswers { get; set; }
        public int QuizLessonsTracked { get; set; }
        public int CommentsCount { get; set; }
        public DateTime? LastActivityAt { get; set; }
        public DateTime? SortDate { get; set; }
    }

    public class AdminBulkEmailViewModel
    {
        [Required(ErrorMessage = "L'objet est requis.")]
        [StringLength(200, ErrorMessage = "L'objet ne peut pas dépasser 200 caractères.")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le message est requis.")]
        [StringLength(5000, ErrorMessage = "Le message ne peut pas dépasser 5000 caractères.")]
        public string Message { get; set; } = string.Empty;

        public bool OnlyActiveSubscribers { get; set; }
        public bool OnlyNonSubscribers { get; set; }
    }
}
