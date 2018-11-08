using Microsoft.AspNetCore.Mvc;

namespace Swarm.Node.Controllers
{
    public class DashboardController: Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}