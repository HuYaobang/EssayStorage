using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EssayStorage.Models.Database;
using Microsoft.AspNetCore.Identity;

namespace EssayStorage.Models
{
    public class ApplicationUser : IdentityUser
    {
        public ApplicationUser()
        {
            Essays = new List<Essay>();
            LikedComments = new List<UserComment>();
            UserEssayRatings = new List<UserEssayRating>();
        }
        public string PicturePath { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsBlocked { get; set; }

        public virtual ICollection<Essay> Essays { get; set; }
        public virtual ICollection<UserComment> LikedComments { get; set; }
        public virtual ICollection<UserEssayRating> UserEssayRatings { get; set; }
    }
}
