using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrudDemo.Models
{
    public class Payment
    {
        public int Id { get; set; }
        
        [Required]
        public required string UserId { get; set; }
        
        [Required]
        public int CourseId { get; set; }
        
        [Required]
        public decimal Amount { get; set; }
        
        [Required]
        public required string Currency { get; set; } = "eur";
        
        [Required]
        public required string StripePaymentIntentId { get; set; }
        
        [Required]
        public required string Status { get; set; } // succeeded, pending, failed
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? CompletedAt { get; set; }
        
        [ForeignKey("CourseId")]
        public Course? Course { get; set; }
    }

    public class CourseEnrollment
    {
        public int Id { get; set; }
        
        [Required]
        public required string UserId { get; set; }
        
        [Required]
        public int CourseId { get; set; }
        
        public int? PaymentId { get; set; }
        
        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? ExpiresAt { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        [ForeignKey("CourseId")]
        public Course? Course { get; set; }
        
        [ForeignKey("PaymentId")]
        public Payment? Payment { get; set; }
    }

    public class Subscription
    {
        public int Id { get; set; }
        
        [Required]
        public required string UserId { get; set; }
        
        [Required]
        public required string StripeSubscriptionId { get; set; }
        
        [Required]
        public required string StripeCustomerId { get; set; }
        
        [Required]
        public required string Status { get; set; } // active, canceled, past_due
        
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        
        public DateTime? EndDate { get; set; }
        
        public DateTime? CanceledAt { get; set; }
        
        public bool IsActive { get; set; } = true;
    }
}
