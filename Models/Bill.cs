using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CentralMed.Models
{
    public class Bill
    {
        [Key]
        public int BillID { get; set; }
        
        [Required]
        [StringLength(50)]
        public string BillNo { get; set; }
        
        public DateTime Date { get; set; }
        
        [StringLength(255)]
        public string PatientName { get; set; }
        
        [StringLength(255)]
        public string DoctorName { get; set; }
        
        public decimal TotalAmount { get; set; }

        public virtual ICollection<BillItem> BillItems { get; set; }

        public Bill()
        {
            BillItems = new HashSet<BillItem>();
        }
    }
}
