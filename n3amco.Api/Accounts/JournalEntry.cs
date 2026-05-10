public class JournalEntry
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; }
    public string Reference { get; set; }   // مثال: SALE-001
    public JournalEntryType Type { get; set; }
    public bool IsPosted { get; set; } = false; // راجعه المحاسب؟
    public string CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<JournalLine> Lines { get; set; } = new List<JournalLine>();
}

public class JournalLine
{
    public int Id { get; set; }
    public int JournalEntryId { get; set; }
    public int AccountId { get; set; }
    public decimal Debit { get; set; }   // مدين
    public decimal Credit { get; set; }  // دائن
    public string Notes { get; set; }
    public JournalEntry JournalEntry { get; set; }
    public Account Account { get; set; }
}

public enum JournalEntryType
{
    Sale = 1,           // بيع
    Purchase = 2,       // شراء
    StockAdjustment = 3,// تسوية مخزون
    Payment = 4,        // دفعة
    Manual = 5          // يدوي من المحاسب
}