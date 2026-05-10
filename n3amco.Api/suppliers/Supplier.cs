namespace n3amco.Api.suppliers
{
    public class Supplier
    {
        public int Id { get; set; }

        // Basic
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }

        // Operational
        public decimal? DefaultPricePerKg { get; set; }
        public decimal? DefaultFatPercentage { get; set; }

        // Financial
        public decimal OpeningBalance { get; set; }
        public decimal CurrentBalance { get; set; } 
        public decimal? CreditLimit { get; set; }

        public string PaymentTerms { get; set; } = "";

        // Status
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        // Relations
        public ICollection<MilkCollection> MilkCollections { get; set; } = new List<MilkCollection>();
        public ICollection<SupplierPayment> Payments { get; set; } = new List<SupplierPayment>();
        public ICollection<SupplierLedger> Ledger { get; set; } = new List<SupplierLedger>();
    }
}
