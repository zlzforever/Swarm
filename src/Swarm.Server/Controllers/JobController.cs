using System;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Swarm.Basic.Entity;
using Swarm.Core;
using Swarm.Core.Common;
using Swarm.Core.Controllers;
using Swarm.Server.Models;

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