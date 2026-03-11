public class Account
{
    public int Id { get; set; }
    public string Code { get; set; }      // مثال: 1001
    public string Name { get; set; }      // مثال: الصندوق
    public AccountType Type { get; set; } // أصل، خصم، إيراد، مصروف
    public int? ParentId { get; set; }    // الحساب الأب
    public Account Parent { get; set; }
    public ICollection<Account> Children { get; set; } = new List<Account>();
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

public enum AccountType
{
    Asset = 1,      // أصول
    Liability = 2,  // خصوم
    Equity = 3,     // حقوق ملكية
    Revenue = 4,    // إيرادات
    Expense = 5     // مصروفات
}