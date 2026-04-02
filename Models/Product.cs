using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CentralMed.Models
{
    public class Product
    {
        [Key]
        public int ProductID { get; set; }
        
        [Required]
        [StringLength(255)]
        public string DrugName { get; set; }
        
        [StringLength(255)]
        public string Manufacturer { get; set; }

        public virtual ICollection<Batch> Batches { get; set; }
        public virtual ICollection<BillItem> BillItems { get; set; }

        public Product()
        {
            Batches = new HashSet<Batch>();
            BillItems = new HashSet<BillItem>();
        }
    }
}
