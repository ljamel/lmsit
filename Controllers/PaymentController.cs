using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CrudDemo.Data;
using CrudDemo.Models;
using Stripe;
using Stripe.Checkout;

namespace CrudDemo.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public PaymentController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

        // Page de paiement d'abonnement mensuel
        public IActionResult SubscriptionCheckout()
        {
            return View();
        }

        // Créer une session de paiement pour l'abonnement
        [HttpPost]
        public async Task<IActionResult> CreateSubscriptionSession()
        {
            var domain = $"{Request.Scheme}://{Request.Host}";
            
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "eur",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Abonnement Mensuel - Accès Illimité",
                                Description = "Accès complet à tous les cours de la plateforme",
                            },
                            UnitAmount = 2999, // 29.99 EUR en centimes
                            Recurring = new SessionLineItemPriceDataRecurringOptions
                            {
                                Interval = "month",
                            }
                        },
                        Quantity = 1,
                    },
                },
                Mode = "subscription",
                SuccessUrl = $"{domain}/Payment/SubscriptionSuccess?session_id={{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"{domain}/Payment/SubscriptionCancel",
                CustomerEmail = User.Identity?.Name,
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            return Redirect(session.Url);
        }

        // Page de succès abonnement
        public async Task<IActionResult> SubscriptionSuccess(string session_id)
        {
            var service = new SessionService();
            var session = await service.GetAsync(session_id);

            if (session.PaymentStatus == "paid" || session.Status == "complete")
            {
                var userId = User.Identity?.Name ?? "";

                // Vérifier si l'abonnement n'existe pas déjà
                var existingSubscription = await _context.Subscriptions
                    .FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive);

                if (existingSubscription == null)
                {
                    // Créer l'abonnement
                    var subscription = new Models.Subscription
                    {
                        UserId = userId,
                        StripeSubscriptionId = session.SubscriptionId ?? session.Id,
                        StripeCustomerId = session.CustomerId ?? "",
                        Status = "active",
                        IsActive = true
                    };
                    _context.Subscriptions.Add(subscription);
                    await _context.SaveChangesAsync();
                }
            }

            TempData["Success"] = "Votre abonnement est activé ! Bienvenue sur la plateforme.";
            return View();
        }

        // Page d'annulation abonnement
        public IActionResult SubscriptionCancel()
        {
            return View();
        }

        // Page de checkout
        public async Task<IActionResult> Checkout(int courseId)
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null)
                return NotFound();

            // Vérifier si l'utilisateur est déjà inscrit
            var userId = User.Identity?.Name ?? "";
            var existingEnrollment = await _context.CourseEnrollments
                .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId && e.IsActive);

            if (existingEnrollment != null)
            {
                TempData["Message"] = "Vous êtes déjà inscrit à ce cours.";
                return RedirectToAction("Details", "Courses", new { id = courseId });
            }

            ViewBag.Course = course;
            return View(course);
        }

        // Créer une session de paiement Stripe
        [HttpPost]
        public async Task<IActionResult> CreateCheckoutSession(int courseId)
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null)
                return NotFound();

            var domain = $"{Request.Scheme}://{Request.Host}";
            
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "eur",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = course.Title,
                                Description = course.Description.Length > 200 
                                    ? course.Description.Substring(0, 200) + "..." 
                                    : course.Description,
                            },
                            UnitAmount = (long)(course.Price * 100), // Stripe utilise les centimes
                        },
                        Quantity = 1,
                    },
                },
                Mode = "payment",
                SuccessUrl = $"{domain}/Payment/Success?session_id={{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"{domain}/Payment/Cancel?courseId={courseId}",
                ClientReferenceId = courseId.ToString(),
                CustomerEmail = User.Identity?.Name,
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            // Enregistrer le paiement en attente
            var payment = new Payment
            {
                UserId = User.Identity?.Name ?? "",
                CourseId = courseId,
                Amount = course.Price,
                Currency = "eur",
                StripePaymentIntentId = session.PaymentIntentId ?? session.Id,
                Status = "pending"
            };
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return Redirect(session.Url);
        }

        // Page de succès
        public async Task<IActionResult> Success(string session_id)
        {
            var service = new SessionService();
            var session = await service.GetAsync(session_id);

            if (session.PaymentStatus == "paid")
            {
                var courseId = int.Parse(session.ClientReferenceId);
                var userId = User.Identity?.Name ?? "";

                // Mettre à jour le paiement
                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.StripePaymentIntentId == session.PaymentIntentId 
                        || p.StripePaymentIntentId == session.Id);

                if (payment != null)
                {
                    payment.Status = "succeeded";
                    payment.CompletedAt = DateTime.UtcNow;
                    payment.StripePaymentIntentId = session.PaymentIntentId ?? session.Id;
                }

                // Créer l'inscription si elle n'existe pas
                var existingEnrollment = await _context.CourseEnrollments
                    .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId);

                if (existingEnrollment == null)
                {
                    var enrollment = new CourseEnrollment
                    {
                        UserId = userId,
                        CourseId = courseId,
                        PaymentId = payment?.Id
                    };
                    _context.CourseEnrollments.Add(enrollment);
                }

                await _context.SaveChangesAsync();

                var course = await _context.Courses.FindAsync(courseId);
                ViewBag.Course = course;
                return View();
            }

            return RedirectToAction("Cancel");
        }

        // Page d'annulation
        public async Task<IActionResult> Cancel(int courseId)
        {
            var course = await _context.Courses.FindAsync(courseId);
            ViewBag.Course = course;
            return View();
        }

        // Historique des paiements de l'utilisateur
        public async Task<IActionResult> MyPayments()
        {
            var userId = User.Identity?.Name ?? "";
            var payments = await _context.Payments
                .Include(p => p.Course)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(payments);
        }
    }
}
