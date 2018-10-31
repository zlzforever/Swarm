using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Swarm.Basic.Entity;
using Swarm.Core;
using Swarm.Core.Common;
using Swarm.Core.Controllers;
using Swarm.Server.Models;

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