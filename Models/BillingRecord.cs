using System;
using System.ComponentModel.DataAnnotations;

namespace CentralMed.Models
{
    public class BillingRecord
    {
        [Key]
        public int Id { get; set; }
        public string Patient { get; set; }
        public string Prescriber { get; set; }
        public string BillNo { get; set; }
        public DateTime BillDate { get; set; }
        
        public int Qty { get; set; }
        public string DrugName { get; set; }
        public string Schedule { get; set; }
        public string Manufacturer { get; set; }
        public string BatchNo { get; set; }
        public string ExpiryDate { get; set; }
        public decimal Rate { get; set; }
        public decimal Discount { get; set; }
        public decimal Amount { get; set; }
    }
}
