using Microsoft.AspNetCore.Mvc;

namespace Swarm.Node.Controllers
{
    public class NodeController: Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}