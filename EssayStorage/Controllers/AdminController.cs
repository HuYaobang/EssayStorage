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

        [HttpPost]
        public async Task<bool> SetAdmin(string userId)
        {
            ApplicationUser user = db.Users.Where(u => u.Id == userId).FirstOrDefault();
            user.IsAdmin = true;
            db.Users.Update(user);
            await db.SaveChangesAsync();
            return true;
        }

        [HttpPost]
        public async Task<bool> SetBlock(string userId)
        {
            ApplicationUser user = db.Users.Where(u => u.Id == userId).FirstOrDefault();
            user.IsBlocked = !user.IsBlocked;
            db.Users.Update(user);
            await db.SaveChangesAsync();
            return true;
        }

        public IActionResult GetUserEssays(string userId)
        {
            var essays = db.Essays.Where(e => e.UserId == userId).ToList();
            ViewData.Add("essays", essays);
            return View();
        }

        public async Task<IActionResult> AdminPanel()
        {
            if ((await userManager.GetUserAsync(User)).IsAdmin)
            {
                List<ApplicationUser> users = db.Users.ToList();
                ViewData.Add("users", users);
                return View();
            }                
            else
                return View("Home/Index");
        }
    }
}