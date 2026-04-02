using System;
using System.Collections.Generic;
using System.Linq;
using CentralMed.Data;
using CentralMed.Models;

namespace CentralMed.Services
{
    public class ProductService
    {
        public void AddProduct(Product product)
        {
            try
            {
                using (var context = new AppDbContext())
                {
                    context.Products.Add(product);
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error adding product.", ex);
            }
        }

        public List<Product> GetAllProducts()
        {
            try
            {
                using (var context = new AppDbContext())
                {
                    return context.Products.Include("Batches").ToList();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving all products.", ex);
            }
        }

        public List<Product> SearchProducts(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return GetAllProducts();

            try
            {
                using (var context = new AppDbContext())
                {
                    return context.Products
                        .Include("Batches")
                        .Where(p => p.DrugName.Contains(keyword) || p.Manufacturer.Contains(keyword))
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error searching products.", ex);
            }
        }
    }
}
