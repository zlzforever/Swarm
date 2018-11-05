using Microsoft.AspNetCore.Mvc;

namespace Swarm.Server.Controllers
{
    public class ClientController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}