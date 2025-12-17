using Microsoft.AspNetCore.Mvc;

namespace CrudDemo.Controllers
{
    public class LegalController : Controller
    {
        public IActionResult TermsOfSale()
        {
            return View();
        }

        public IActionResult FAQ()
        {
            return View();
        }
    }
}
