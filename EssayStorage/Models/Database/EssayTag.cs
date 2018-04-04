using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EssayStorage.Models.Database
{
    public class EssayTag
    {
        public int EssayId { get; set; }
        public Essay Essay { get; set; }

        public string TagId { get; set; }
        public Tag Tag { get; set; }
    }
}