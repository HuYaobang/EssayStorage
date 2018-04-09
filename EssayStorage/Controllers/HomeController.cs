using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EssayStorage.Models;
using EssayStorage.Data;
using Microsoft.AspNetCore.Identity;
using EssayStorage.Models.Database;

namespace EssayStorage.Controllers
{

    public class HomeController : Controller
    {
        private ApplicationDbContext db;
        private UserManager<ApplicationUser> userManager;

        public HomeController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            this.db = db;
            this.userManager = userManager;
        }

        public IActionResult Best()
        {
            List<Essay> essays = db.Essays.OrderByDescending(essay => essay.AverageRating).ToList();
            ViewData.Add("essays", essays);
            return View("Index");
        }

        public IActionResult Index()
        {
            List<Essay> essays = db.Essays.ToList();
            essays.Sort(new Essay.DateComparer());
            ViewData.Add("essays", essays);
            return View("Index");
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        public List<Tag> GetTagsCloud()
        {
            return db.Tags.OrderByDescending(t => t.Frequency).Skip(1).Take(20).ToList();
        }
    }
}
