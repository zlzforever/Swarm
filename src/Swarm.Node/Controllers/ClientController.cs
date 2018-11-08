using Microsoft.AspNetCore.Mvc;

namespace Swarm.Node.Controllers
{
    public class ClientController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}