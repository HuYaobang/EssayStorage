using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EssayStorage.Data;
using EssayStorage.Models;
using EssayStorage.Models.Database;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EssayStorage.Controllers
{
    public class DisplayController : Controller
    {
        private ApplicationDbContext db;
        private UserManager<ApplicationUser> userManager;

        public DisplayController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            this.db = db;
            this.userManager = userManager;
        }

        public IActionResult Essay(int essayId)
        {
            Console.WriteLine("@@@@@@@@@@@@@@@@@: " + essayId);
            Essay essay = db.Essays.Where(e => e.Id == essayId).FirstOrDefault();
            if (essay == null)
            {
                return View("essaynotfound");
            }
            //Comment comments = db.Comments.Where
            //essay.Comments = new List<Comment>();
            //essay.Comments.Add(new Comment { Text = "123", CreationDate = DateTime.Now, UserId = userManager.GetUserId(User) });
            //db.Essays.Update(essay);
            //db.SaveChanges();
            return View(essay);
        }
    }
}