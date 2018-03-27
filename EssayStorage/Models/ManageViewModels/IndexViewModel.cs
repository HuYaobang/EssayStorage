using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EssayStorage.Models.ManageViewModels
{
    public class IndexViewModel
    {
        public string Username { get; set; }

        public bool IsEmailConfirmed { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        public string StatusMessage { get; set; }

        public string UserInfo { get; set; }

        public string PicturePath { get; set; }
    }
}
