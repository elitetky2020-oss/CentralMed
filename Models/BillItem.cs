using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CentralMed.Models
{
    public class BillItem
    {
        [Key]
        public int BillItemID { get; set; }
        
        public int BillID { get; set; }
        
        public int ProductID { get; set; }
        
        public int BatchID { get; set; }
        
        public int Qty { get; set; }
        
        public decimal Rate { get; set; }
        
        public decimal Amount { get; set; }
        
        [StringLength(255)]
        public string Scheme { get; set; }

        [ForeignKey("BillID")]
        public virtual Bill Bill { get; set; }
        
        [ForeignKey("ProductID")]
        public virtual Product Product { get; set; }
        
        [ForeignKey("BatchID")]
        public virtual Batch Batch { get; set; }
    }
}
