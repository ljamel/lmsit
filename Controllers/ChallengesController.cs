using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CrudDemo.Controllers
{
    public class ChallengesController : Controller
    {
        public IActionResult index()
        {
            return View();
        }
    }
}
