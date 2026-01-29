using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace CrudDemo.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
    {
        try
        {
            var smtpHost = _configuration["EmailSettings:SmtpHost"];
            var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
            var smtpUsername = _configuration["EmailSettings:SmtpUsername"];
            var smtpPassword = _configuration["EmailSettings:SmtpPassword"];
            var fromEmail = _configuration["EmailSettings:FromEmail"];
            var fromName = _configuration["EmailSettings:FromName"];

            // Retirer tous les espaces du mot de passe (Gmail copie parfois avec des espaces)
            if (!string.IsNullOrEmpty(smtpPassword))
            {
                smtpPassword = smtpPassword.Replace(" ", "");
            }

            _logger.LogInformation("Configuration SMTP: Host={Host}, Port={Port}, Username={Username}", 
                smtpHost, smtpPort, smtpUsername);

            // Accepter tous les certificats SSL (pour localhost/développement)
            ServicePointManager.ServerCertificateValidationCallback = (_, _, _, _) => true;

            using var smtpClient = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                Timeout = 30000,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail ?? smtpUsername!, fromName ?? "Ingenius cyber"),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };

            mailMessage.To.Add(toEmail);

            _logger.LogInformation("Tentative d'envoi d'email à {Email} via {SmtpHost}:{SmtpPort}", toEmail, smtpHost, smtpPort);
            
            await smtpClient.SendMailAsync(mailMessage);
            
            _logger.LogInformation("✅ Email envoyé avec succès à {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erreur lors de l'envoi de l'email à {Email}. Message: {Message}", toEmail, ex.Message);
            // Ne pas throw pour ne pas bloquer l'inscription/paiement
        }
    }

    public async Task SendRegistrationEmailAsync(string toEmail, string userName)
    {
        var subject = "Bienvenue dans la communautés Ingenius !";
        var body = $@"
<!DOCTYPE html>
<html>
<head>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Bienvenue chez ingenius !</h1>
        </div>
        <div class='content'>
            
           

            <p>Bienvenue,</p>

            <p>Merci de vous être inscrit à notre cours de cybersécurité.</p>


            <p>
            🎁 <strong>Bonus exclusif</strong><br />
            Nous vous offrons une sélection des livres pour bien commencer la  cybersécurité en PDF à télécharger ici :<br />
            <a href='https://drive.google.com/file/d/1paB0tcQ3KvTabqdLVV4eo3wdRG_iezi2/view?usp=sharing'>CCNA les bases du réseaux</a>
            <a href='https://drive.google.com/file/d/1AzGbAi_tZYaLApg-9ioJbHAOfkao3dNL/view?usp=sharing'>LPIC les bases de Linux</a>
            <a href='https://docs.google.com/document/d/125qy1y56yMGLpjicOo6iudWR0a42itXenYFop2bqfdo/edit?usp=sharing'>Initiation à la cyber</a>
            </p>

            <p>
            Imaginez pouvoir comprendre le fonctionnement des systèmes d’information, identifier leurs failles et maîtriser les bases de la cybersécurité.<br />
            Ce qui était un rêve peut aujourd’hui devenir une réalité.
            </p>

            <h3>Avec notre cours, vous allez :</h3>

            <ul>
            <li>
            <p><strong>Maîtriser les bases</strong> : même si vous débutez, nous vous accompagnons pas à pas avec des explications claires et accessibles.</p>
            </li>
            <li>
            <p><strong>Développer des compétences pratiques</strong> : des connaissances concrètes et applicables immédiatement.</p>
            </li>
            <li>
            <p><strong>Apprendre avec des experts</strong> : un contenu conçu par des professionnels passionnés par la cybersécurité.</p>
            </li>
            <li>
            <p><strong>Rejoindre une communauté active</strong> : échangez, posez vos questions et progressez avec d’autres apprenants motivés.</p>
            </li>
            </ul>

            <p>
            🚀 <strong>Accès exclusif</strong>
            </p>
            <p>Vous avez maintenant accès à tout le contenu du cours. Via la lien <a href='https://progfacil.fr/Account/Login'>https://progfacil.fr/Account/Login</a> Bonne formation !</p>

            <p>
            Nous avons hâte de vous voir commencer cette aventure passionnante avec nous.<br />
            Il s’agit d’une opportunité d’accès anticipé exclusive.
            </p>

            <p>À très bientôt,</p>

            <p>
            <strong>L’équipe Ingenius Cyber</strong><br />
            📧 Contact : <a href='mailto:djamallamri@yahoo.fr'>djamallamri@yahoo.fr</a>
            </p>



        </div>
        <div class='footer'>
            <p>© 2026 ingenius - Tous droits réservés</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendSubscriptionEmailAsync(string toEmail, string userName, string courseName, decimal amount)
    {
        var subject = "Confirmation de votre abonnement - Ingenius";
        var body = $@"
<!DOCTYPE html>
<html>
<head>

</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Merci pour votre abonnement !</h1>
        </div>
        <div class='content'>
            <h2>Bonjour,</h2>
            <p>Votre paiement a été traité avec succès !</p>
            <div class='details'>
                <h3>Détails de votre abonnement :</h3>
                <p><strong>Cours :</strong> {courseName}</p>
                <p><strong>Montant :</strong> {amount:C}</p>
                <p><strong>Date :</strong> {DateTime.Now:dd/MM/yyyy HH:mm}</p>
            </div>
            <p>Vous avez maintenant accès à tout le contenu du cours. Via la lien <a href='https://progfacil.fr/Account/Login'>https://progfacil.fr/Account/Login</a> Bonne formation !</p>
        </div>
        <div class='footer'>
            <p>© 2026 Ingenius cyber - Tous droits réservés</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(toEmail, subject, body);
    }
}
