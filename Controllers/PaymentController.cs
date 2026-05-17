using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CrudDemo.Data;
using CrudDemo.Models;
using CrudDemo.Services;
using Stripe;
using Stripe.Checkout;

namespace CrudDemo.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            ApplicationDbContext context, 
            IConfiguration configuration,
            IEmailService emailService,
            ILogger<PaymentController> logger)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
            _logger = logger;
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

        // Page de paiement AVANT l'inscription (accessible sans authentification)
        [AllowAnonymous]
        public IActionResult PreRegistrationCheckout()
        {
            return View();
        }

        // Créer une session de paiement pour la pré-inscription - Redirection directe vers Stripe
        [AllowAnonymous]
        [HttpPost]
        [HttpGet]
        public async Task<IActionResult> CreatePreRegistrationSession()
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
                                Name = "Accès à Vie - Accès Illimité",
                                Description = 
                                "• 1 heure de visio pour définir votre parcours.\u2028" +
                                "• Garantie satisfait ou remboursé.\u2028" +
                                "• Ajout de contenu personnalisé selon vos demandes.\u2028" +
                                "• Une équipe professionnelle toujours à vos côtés pour vous accompagner tout au long de votre parcours de formation.",
                            },
                            UnitAmount = 24900, // 249 EUR en centimes
                        },
                        Quantity = 1,
                    },
                },
                Mode = "payment",
                BillingAddressCollection = "auto",
                Locale = "fr",
                AllowPromotionCodes = true,
                SuccessUrl = $"{domain}/Payment/PreRegistrationSuccess?session_id={{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"{domain}/Payment/PreRegistrationCancel",
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            return Redirect(session.Url);
        }

        // Succès du paiement de pré-inscription - Créer le compte automatiquement
        [AllowAnonymous]
        public async Task<IActionResult> PreRegistrationSuccess(string session_id)
        {
            var service = new SessionService();
            var session = await service.GetAsync(session_id);

            if (session.PaymentStatus == "paid" || session.Status == "complete")
            {
                // Récupérer l'email depuis le customer Stripe
                // Priorité : CustomerDetails.Email (payment mode) > CustomerEmail > CustomerId API call
                string? email = session.CustomerDetails?.Email
                             ?? session.CustomerEmail;

                if (string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(session.CustomerId))
                {
                    var customerService = new Stripe.CustomerService();
                    var customer = await customerService.GetAsync(session.CustomerId);
                    email = customer.Email;
                }

                if (string.IsNullOrEmpty(email))
                {
                    TempData["Error"] = "Impossible de récupérer votre email. Veuillez contacter le support.";
                    return RedirectToAction("Register", "Account");
                }

                // Stocker les informations de paiement pour créer l'abonnement
                TempData["PaymentSessionId"] = session_id;
                TempData["StripeSubscriptionId"] = session.SubscriptionId ?? session.Id;
                TempData["StripeCustomerId"] = session.CustomerId ?? "";
                TempData["UserEmail"] = email;
                TempData["Success"] = "Paiement réussi ! Votre compte a été créé. Veuillez définir votre mot de passe.";
                
                // Rediriger vers la page de création du compte avec mot de passe aléatoire
                return RedirectToAction("CompleteRegistrationAfterPayment", "Account", new { email = email });
            }

            TempData["Error"] = "Le paiement n'a pas été confirmé. Veuillez réessayer.";
            return RedirectToAction("Register", "Account");
        }

        // Annulation du paiement de pré-inscription
        [AllowAnonymous]
        public IActionResult PreRegistrationCancel()
        {
            TempData["Message"] = "Paiement annulé. Vous pouvez réessayer quand vous le souhaitez.";
            return View();
        }

        // Page de paiement d'abonnement mensuel (pour utilisateurs déjà inscrits)
        [Authorize]
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
                                Name = "Accès à Vie - Accès Illimité",
                                Description = "Accès complet à tous les cours de la plateforme",
                            },
                            UnitAmount = 24900, // 249 EUR en centimes
                        },
                        Quantity = 1,
                    },
                },
                Mode = "payment",
                BillingAddressCollection = "auto",
                Locale = "fr",
                AllowPromotionCodes = true,
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

                // Optimisé: Vérifier si l'abonnement n'existe pas déjà avec AsNoTracking
                var existingSubscription = await _context.Subscriptions
                    .AsNoTracking()
                    .Where(s => s.UserId == userId && s.IsActive)
                    .FirstOrDefaultAsync();

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

                    // Envoyer un email de confirmation d'abonnement
                    try
                    {
                        await _emailService.SendSubscriptionEmailAsync(
                            userId, 
                            userId.Split('@')[0], 
                            "Accès à Vie - Accès Illimité", 
                            249.00m);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erreur lors de l'envoi de l'email d'abonnement à {Email}", userId);
                        // Ne pas bloquer le processus si l'email échoue
                    }
                }

                TempData["Success"] = "Votre accès à vie est activé ! Bienvenue sur la plateforme.";
                return RedirectToAction("Index", "Courses");
            }

            TempData["Error"] = "Le paiement n'a pas été confirmé. Veuillez réessayer ou contacter le support.";
            return RedirectToAction("SubscriptionCheckout");
        }

        // Page d'annulation abonnement
        public IActionResult SubscriptionCancel()
        {
            return View();
        }

        // Page de checkout
        public async Task<IActionResult> Checkout(int courseId)
        {
            // Optimisé: AsNoTracking pour lecture seule + filtre WHERE
            var course = await _context.Courses
                .AsNoTracking()
                .Where(c => c.Id == courseId)
                .FirstOrDefaultAsync();
                
            if (course == null)
                return NotFound();

            // Vérifier si l'utilisateur est déjà inscrit
            var userId = User.Identity?.Name ?? "";
            var existingEnrollment = await _context.CourseEnrollments
                .AsNoTracking()
                .Where(e => e.UserId == userId && e.CourseId == courseId && e.IsActive)
                .FirstOrDefaultAsync();

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
            // Optimisé: AsNoTracking pour lecture seule
            var course = await _context.Courses
                .AsNoTracking()
                .Where(c => c.Id == courseId)
                .FirstOrDefaultAsync();
                
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
                            UnitAmount = 1900, // Prix fixe: 19€ (en centimes)
                        },
                        Quantity = 1,
                    },
                },
                Mode = "payment",
                BillingAddressCollection = "auto",
                Locale = "fr",
                AllowPromotionCodes = true,
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
                Amount = 19.00m, // Prix fixe: 19€
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

                // Optimisé: Mettre à jour le paiement (sans AsNoTracking car on modifie)
                var payment = await _context.Payments
                    .Where(p => p.StripePaymentIntentId == session.PaymentIntentId 
                        || p.StripePaymentIntentId == session.Id)
                    .FirstOrDefaultAsync();

                if (payment != null)
                {
                    payment.Status = "succeeded";
                    payment.CompletedAt = DateTime.UtcNow;
                    payment.StripePaymentIntentId = session.PaymentIntentId ?? session.Id;
                }

                // Optimisé: Vérifier inscription existante avec AsNoTracking
                var existingEnrollment = await _context.CourseEnrollments
                    .AsNoTracking()
                    .Where(e => e.UserId == userId && e.CourseId == courseId)
                    .FirstOrDefaultAsync();

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

                // Optimisé: Lecture seule avec AsNoTracking
                var course = await _context.Courses
                    .AsNoTracking()
                    .Where(c => c.Id == courseId)
                    .FirstOrDefaultAsync();

                // Envoyer un email de confirmation d'inscription au cours
                if (course != null)
                {
                    try
                    {
                        await _emailService.SendSubscriptionEmailAsync(
                            userId, 
                            userId.Split('@')[0], 
                            course.Title, 
                            19.00m);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erreur lors de l'envoi de l'email de confirmation pour le cours {CourseId}", courseId);
                        // Ne pas bloquer le processus si l'email échoue
                    }
                }
                    
                ViewBag.Course = course;
                return View();
            }

            return RedirectToAction("Cancel");
        }

        // Page d'annulation
        public async Task<IActionResult> Cancel(int courseId)
        {
            // Optimisé: AsNoTracking pour lecture seule
            var course = await _context.Courses
                .AsNoTracking()
                .Where(c => c.Id == courseId)
                .FirstOrDefaultAsync();
                
            ViewBag.Course = course;
            return View();
        }

        // Historique des paiements de l'utilisateur
        public async Task<IActionResult> MyPayments()
        {
            var userId = User.Identity?.Name ?? "";
            
            // Optimisé: AsNoTracking + filtre WHERE + projection
            var payments = await _context.Payments
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .Include(p => p.Course)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(payments);
        }

    }
}
