using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EssayStorage.Models.Database;
using Microsoft.AspNetCore.Identity;

namespace EssayStorage.Models
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public ApplicationUser()
        {
            Essays = new List<Essay>();
            LikedComments = new List<UserComment>();
        }

        public string UserInfo { get; set; }
        public string PicturePath { get; set; }
        public bool IsAdmin { get; set; }

        public virtual ICollection<Essay> Essays { get; set; }
        public virtual ICollection<UserComment> LikedComments { get; set; }
    }
}
