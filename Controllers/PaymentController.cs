using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CrudDemo.Data;
using CrudDemo.Models;
using CrudDemo.Services;
using Stripe;
using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

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

        // Créer une session de paiement pour la pré-inscription - Redirection directe vers Stripe avec essai de 3 jours et frais de 1 euro
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
                    // Ligne 1 : L'abonnement annuel de 249 euros avec essai de 3 jours
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "eur",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Abonnement Annuel - Accès Illimité",
                                Description = "Accès complet aux formations et laboratoires. Essai de 3 jours, puis 249,00 EUR par an. Annulable à tout moment.",
                            },
                            UnitAmount = 24900, // 249 EUR en centimes
                            Recurring = new SessionLineItemPriceDataRecurringOptions
                            {
                                Interval = "year",
                                IntervalCount = 1
                            }
                        },
                        Quantity = 1,
                    },
                    // Ligne 2 : Le prélèvement immédiat de 1 euro pour la validation de l'essai
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "eur",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Frais de validation et d'ouverture de l'essai",
                                Description = "Prélèvement immédiat de 1,00 EUR pour l'activation sécurisée de vos 3 jours d'accès.",
                            },
                            UnitAmount = 100 // 1 EUR en centimes (paiement unique, pas de récurrence)
                        },
                        Quantity = 1,
                    }
                },
                Mode = "subscription", // Obligatoire pour combiner abonnement et frais uniques
                SubscriptionData = new SessionSubscriptionDataOptions
                {
                    TrialPeriodDays = 3 // Application des 3 jours d'essai sur la partie abonnement
                },
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

            // Comme un montant de 1 euro a été facturé, le statut passe à "paid"
            if (session.PaymentStatus == "paid" || session.Status == "complete")
            {
                // Récupérer l'email depuis le customer Stripe
                string? email = session.CustomerDetails?.Email ?? session.CustomerEmail;

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

                // Validation de sécurité : l'ID d'abonnement doit être présent
                if (string.IsNullOrEmpty(session.SubscriptionId))
                {
                    _logger.LogCritical("Erreur critique : La session d'abonnement {SessionId} n'a pas généré de SubscriptionId.", session_id);
                    TempData["Error"] = "Le type de configuration de paiement retourné par Stripe est invalide. Veuillez contacter le support.";
                    return RedirectToAction("Register", "Account");
                }

                // Stocker les informations de paiement pour créer l'abonnement
                TempData["PaymentSessionId"] = session_id;
                TempData["StripeSubscriptionId"] = session.SubscriptionId;
                TempData["StripeCustomerId"] = session.CustomerId ?? "";
                TempData["UserEmail"] = email;
                TempData["Success"] = "Paiement de 1 euro validé. Votre période d'essai de 3 jours a commencé. Veuillez définir votre mot de passe.";

                return RedirectToAction("CompleteRegistrationAfterPayment", "Account", new { email = email });
            }

            TempData["Error"] = "Le prélèvement initial de validation a échoué. Veuillez réessayer.";
            return RedirectToAction("Register", "Account");
        }

        // Annulation du paiement de pré-inscription
        [AllowAnonymous]
        public IActionResult PreRegistrationCancel()
        {
            TempData["Message"] = "Paiement annulé. Vous pouvez réessayer quand vous le souhaitez.";
            return View();
        }

        // Page de paiement d'abonnement annuel (pour utilisateurs déjà inscrits)
        [Authorize]
        public IActionResult SubscriptionCheckout()
        {
            return View();
        }

        // Créer une session de paiement pour l'abonnement annuel post-inscription avec 3 jours d'essai et prélèvement de 1 euro
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
                                Name = "Abonnement Annuel - Accès Illimité avec accompagnement personnalisé via Discord",
                                Description = "Accès complet à tous les cours de la plateforme. pendant 3 jours, puis 249,00 EUR par an.",
                            },
                            UnitAmount = 24900,
                            Recurring = new SessionLineItemPriceDataRecurringOptions
                            {
                                Interval = "year",
                                IntervalCount = 1
                            }
                        },
                        Quantity = 1,
                    },
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "eur",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Frais de validation et d'ouverture de l'essai",
                                Description = "Prélèvement immédiat de 1,00 EUR pour l'activation sécurisée de vos 3 jours d'accès.",
                            },
                            UnitAmount = 100
                        },
                        Quantity = 1,
                    }
                },
                Mode = "subscription",
                SubscriptionData = new SessionSubscriptionDataOptions
                {
                    TrialPeriodDays = 3
                },
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

                var existingSubscription = await _context.Subscriptions
                    .AsNoTracking()
                    .Where(s => s.UserId == userId && s.IsActive)
                    .FirstOrDefaultAsync();

                if (existingSubscription == null)
                {
                    if (string.IsNullOrEmpty(session.SubscriptionId))
                    {
                        _logger.LogCritical("Erreur critique : L'activation de l'essai annuel pour {UserId} a échoué car le SubscriptionId est nul.", userId);
                        TempData["Error"] = "Une erreur technique est survenue lors de la configuration de votre abonnement.";
                        return RedirectToAction("SubscriptionCheckout");
                    }

                    var subscription = new Models.Subscription
                    {
                        UserId = userId,
                        StripeSubscriptionId = session.SubscriptionId,
                        StripeCustomerId = session.CustomerId ?? "",
                        Status = "trialing", // L'abonnement est considéré en cours d'essai chez Stripe
                        IsActive = true
                    };
                    _context.Subscriptions.Add(subscription);
                    await _context.SaveChangesAsync();

                    try
                    {
                        await _emailService.SendSubscriptionEmailAsync(
                            userId,
                            userId.Split('@')[0],
                            "Abonnement Annuel - Période d'essai lancée (Prélèvement de 1 EUR)",
                            249.00m);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erreur lors de l'envoi de l'email d'abonnement à {Email}", userId);
                    }
                }

                TempData["Success"] = "Le prélèvement de 1 euro a réussi. Vos 3 jours d'essai sont actifs !";
                return RedirectToAction("Index", "Courses");
            }

            TempData["Error"] = "Le paiement de validation a échoué. Veuillez réessayer.";
            return RedirectToAction("SubscriptionCheckout");
        }

        // Page d'annulation abonnement
        public IActionResult SubscriptionCancel()
        {
            return View();
        }

        // Page de checkout pour un cours individuel
        public async Task<IActionResult> Checkout(int courseId)
        {
            var course = await _context.Courses
                .AsNoTracking()
                .Where(c => c.Id == courseId)
                .FirstOrDefaultAsync();

            if (course == null)
                return NotFound();

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

        // Créer une session de paiement Stripe pour un cours individuel (Achat à l'unité de 19€)
        [HttpPost]
        public async Task<IActionResult> CreateCheckoutSession(int courseId)
        {
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
                            UnitAmount = 1900,
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

            var payment = new Payment
            {
                UserId = User.Identity?.Name ?? "",
                CourseId = courseId,
                Amount = 19.00m,
                Currency = "eur",
                StripePaymentIntentId = session.PaymentIntentId ?? session.Id,
                Status = "pending"
            };
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return Redirect(session.Url);
        }

        // Page de succès pour un cours individuel
        public async Task<IActionResult> Success(string session_id)
        {
            var service = new SessionService();
            var session = await service.GetAsync(session_id);

            if (session.PaymentStatus == "paid")
            {
                var courseId = int.Parse(session.ClientReferenceId);
                var userId = User.Identity?.Name ?? "";

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

                var course = await _context.Courses
                    .AsNoTracking()
                    .Where(c => c.Id == courseId)
                    .FirstOrDefaultAsync();

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
                    }
                }

                ViewBag.Course = course;
                return View();
            }

            return RedirectToAction("Cancel");
        }

        // Page d'annulation pour un cours individuel
        public async Task<IActionResult> Cancel(int courseId)
        {
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