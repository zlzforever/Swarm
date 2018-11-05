using Microsoft.AspNetCore.Mvc;

namespace Swarm.Server.Controllers
{
    public class JobController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult CronProc()
        {
            return View();
        }

        public IActionResult State()
        {
            return View();
        }

        public IActionResult Log()
        {
            return View();
        }

        public IActionResult Detail(string id)
        {
            ViewData["JobId"] = id;
            return View();
        }      
    }
}