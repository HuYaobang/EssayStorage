using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EssayStorage.Data;
using EssayStorage.Models;
using EssayStorage.Models.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EssayStorage.Controllers
{
    public class SearchController : Controller
    {
        private ApplicationDbContext db;
        private UserManager<ApplicationUser> userManager;

        public SearchController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            this.db = db;
            this.userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public IActionResult StartSearch(string Data)
        {
            List<Essay> essays = GetEssaysBySearchNameAndContent(Data);
            List<Essay> result = essays.Union(GetEssaysSerachedByName(Data)).ToList();
            ViewData.Add("essays", result);
            return View("SearchResult");
        }

        [HttpGet]
        public IActionResult TagsSearch(string Data)
        {
            var essayTags = db.EssayToTags.Where(e => e.TagId == Data).ToList();
            List<Essay> result = new List<Essay>();
            foreach (var essayTag in essayTags) result.Add(db.Essays.Where(e => e.Id == essayTag.EssayId).FirstOrDefault());
            ViewData.Add("essays", result);
            return View("SearchResult");
        }

        private List<Essay> GetEssaysSerachedByName(string data)
        {
            var comments = db.Comments.Where(с => с.Text.Contains(data)).ToList();
            List<Essay> essays = new List<Essay>();
            foreach (var comment in comments)
                essays.Add(db.Essays.Where(e => e.Id == comment.EssayId).First());
            return essays;
        }

        private List<Essay> GetEssaysBySearchNameAndContent(string data)
        {
            return db.Essays.Where(
                    essay =>
                    essay.Content.Contains(data) ||
                    essay.Name.Contains(data)).ToList();
        }
    }
}