using Microsoft.AspNetCore.Mvc;

namespace Swarm.Node.Controllers
{
    public class JobController : Controller
    {
        public IActionResult Cron()
        {
            return View();
        }

        public IActionResult CronProc()
        {
            return View();
        }

        public IActionResult Process()
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