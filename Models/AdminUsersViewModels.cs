namespace CrudDemo.Models
{
    public class AdminUserQuizResultRowViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string? UserName { get; set; }
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
}
