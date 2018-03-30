using EssayStorage.Models.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EssayStorage.Models.ClientsModels
{
    public class ClientComment
    {
        public int Id { get; set; }
        public int? ParentId { get; set; }
        public string Text { get; set; }
        public DateTime CreationDate { get; set; }
        public string UserPicturePath { get; set; }
        public int LikesCount { get; set; }
        public string UserName { get; set; }
    }
}
