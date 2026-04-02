using System;
using System.Collections.Generic;
using CentralMed.Data;
using CentralMed.Models;

namespace CentralMed.Services
{
    public class BillingService
    {
        public void SaveRecords(List<BillingRecord> records)
        {
            try
            {
                using (var context = new AppDbContext())
                {
                    context.BillingRecords.AddRange(records);
                    context.SaveChanges();
                }
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
            {
                var valErrors = new List<string>();
                foreach (var validationErrors in dbEx.EntityValidationErrors)
                {
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        valErrors.Add(validationError.ErrorMessage);
                    }
                }
                throw new Exception("Validation Error: " + string.Join("; ", valErrors), dbEx);
            }
            catch (Exception ex)
            {
                string trueMessage = ex.Message;
                if (ex.InnerException != null) trueMessage += " | Inner: " + ex.InnerException.Message;
                if (ex.InnerException?.InnerException != null) trueMessage += " | Deep Inner: " + ex.InnerException.InnerException.Message;
                throw new Exception("Error saving records:\n\n" + trueMessage, ex);
            }
        }
    }
}
