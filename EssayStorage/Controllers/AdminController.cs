using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EssayStorage.Data;
using EssayStorage.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EssayStorage.Controllers
{
    public class AdminController : Controller
    {
        private ApplicationDbContext db;
        private UserManager<ApplicationUser> userManager;

        public AdminController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            this.db = db;
            this.userManager = userManager;
        }

        public async Task<IActionResult> AdminPanel()
        {
            if ((await userManager.GetUserAsync(User)).IsAdmin)
                return View();
            else
                return View("Home/Index");
        }
    }
}