using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Hediyelik.Models
{
    public class CoverType
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [Display(Name="Cover Type")]
        public string Name { get; set; }
    }
}
