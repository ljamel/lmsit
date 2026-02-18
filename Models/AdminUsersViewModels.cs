namespace CrudDemo.Models
{
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
    }
}
