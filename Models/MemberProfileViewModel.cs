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
        public int EarnedQuizPoints { get; set; }
        public int TotalQuizPoints { get; set; }
        public double QuizSuccessRate { get; set; }
        public DateTime? LastQuizAttemptAt { get; set; }

        public int TotalQuizCount { get; set; }
        public int CompletedQuizCount { get; set; }
        public bool IsCertificateEligible { get; set; }
    }
}
