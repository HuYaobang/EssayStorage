using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EssayStorage.Models.Database
{
    public class UserEssayRating
    {
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public int EssayId { get; set; }
        public Essay Essay { get; set; }

        public double Rating { get; set; }
    }
}
