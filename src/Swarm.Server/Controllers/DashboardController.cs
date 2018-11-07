using Microsoft.AspNetCore.Mvc;

namespace Swarm.Server.Controllers
{
    public class DashboardController: Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}