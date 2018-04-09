using System.ComponentModel.DataAnnotations;

namespace EssayStorage.Models.AccountViewModels
{
    public class CreateEssayViewModel
    {
        [Required]
        public string Tags { get; set; }

        [Required]
        [Display(Name = "Название")]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Описание")]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Специализация")]
        public string Specialization { get; set; }

        [Required]
        [Display(Name = "Контент")]
        public string Content { get; set; }
        
        
        [Display(Name = "Id")]
        public int Id { get; set; }
    }
}