﻿namespace LiteCommerce.DomainModels
{
    public class Product
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public int SupplierID { get; set; }
        public int CategoryID { get; set; }
        public string QuantityPerUnit { get; set; }
        public decimal UnitPrice { get; set; }
        public string Descriptions { get; set; }
        public string PhotoPath { get; set; }
    }
}