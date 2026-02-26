namespace CrudDemo.Models
{
    public class MemberProfileViewModel
    {
        public Subscription? Subscription { get; set; }

        public string Email { get; set; } = string.Empty;

        public decimal CurrentSubscriptionPriceEur { get; set; }

        public string? OrientationRole { get; set; }
        public string? OrientationDescription { get; set; }
        public string? OrientationCourse { get; set; }

        public int QuizAttempts { get; set; }
        public int QuizCorrectAnswers { get; set; }
        public double QuizSuccessRate { get; set; }
        public DateTime? LastQuizAttemptAt { get; set; }
    }
}
