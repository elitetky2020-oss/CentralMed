using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CentralMed.Models
{
    public class Batch
    {
        [Key]
        public int BatchID { get; set; }
        
        public int ProductID { get; set; }
        
        [Required]
        [StringLength(100)]
        public string BatchNo { get; set; }
        
        public DateTime ExpiryDate { get; set; }
        
        public decimal PurchaseRate { get; set; }
        
        public decimal SaleRate { get; set; }
        
        public int StockQty { get; set; }

        [ForeignKey("ProductID")]
        public virtual Product Product { get; set; }

        public virtual ICollection<BillItem> BillItems { get; set; }

        public Batch()
        {
            BillItems = new HashSet<BillItem>();
        }
    }
}
