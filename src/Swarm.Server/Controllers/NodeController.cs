using Microsoft.AspNetCore.Mvc;

namespace Swarm.Server.Controllers
{
    public class NodeController: Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}