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
            List<Essay> essays = db.Essays
                .Where
                (
                    essay =>
                    essay.Content.Contains(Data) ||
                    essay.Name.Contains(Data)
                )
                .ToList();

            var comments = db.Comments
                .Where
                (
                    comment =>
                    comment.Text.Contains(Data)
                )
                .ToList();

            List<Essay> essays_ = new List<Essay>();
            foreach(var comment in comments)
            {
                essays_.Add
                    (
                        db.Essays
                        .Where
                        (
                            essay =>
                            essay.Id == comment.EssayId
                        )
                        .First()
                    );
            }

            List<Essay> result = essays.Union(essays_).ToList();
            ViewData.Add("essays", result);
            return View("SearchResult");
        }
    }
}