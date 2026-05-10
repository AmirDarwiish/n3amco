using n3amco.Api;
using Microsoft.EntityFrameworkCore;
public interface IJournalService
{
    Task CreateSaleEntryAsync(Sale sale, bool isPaid);
    Task CreatePurchaseEntryAsync(int supplierId, decimal amount, string reference, bool isPaid = false);
    Task CreateStockAdjustmentEntryAsync(StockAdjustment adjustment, decimal cost);
    Task CreatePaymentEntryAsync(int customerId, decimal amount, string reference);
    Task CreateMilkCollectionEntryAsync(MilkCollection milk);
}

public class JournalService : IJournalService
{
    private readonly ApplicationDbContext _context;

    public JournalService(ApplicationDbContext context)
    {
        _context = context;
    }

    private async Task<int> GetAccountIdAsync(string key)
    {
        var setting = await _context.AccountSettings
            .FirstOrDefaultAsync(x => x.Key == key);

        if (setting == null)
            throw new InvalidOperationException($"AccountSetting غير موجود: {key}");

        return setting.AccountId;
    }

    private void ValidateEntry(JournalEntry entry)
    {
        var totalDebit = entry.Lines.Sum(l => l.Debit);
        var totalCredit = entry.Lines.Sum(l => l.Credit);
        if (totalDebit != totalCredit)
            throw new InvalidOperationException(
                $"القيد غير متوازن — مدين: {totalDebit}, دائن: {totalCredit}");
    }

    public async Task CreateSaleEntryAsync(Sale sale, bool isPaid)
    {
        var cashAccount = await GetAccountIdAsync("Cash");
        var receivableAccount = await GetAccountIdAsync("CustomerReceivable");
        var salesRevenueAccount = await GetAccountIdAsync("SalesRevenue");
        var cogsAccount = await GetAccountIdAsync("COGS");
        var inventoryAccount = await GetAccountIdAsync("Inventory");

        var entry = new JournalEntry
        {
            Date = sale.CreatedAt,
            Description = $"قيد بيع — فاتورة #{sale.Id}",
            Reference = $"SALE-{sale.Id}",
            Type = JournalEntryType.Sale,
            CreatedBy = "system",
            CreatedAt = DateTime.UtcNow,
            Lines = new List<JournalLine>
        {
            new JournalLine
            {
                // لو دفع نقدي → الصندوق، لو آجل → ذمم العملاء
                AccountId = isPaid ? cashAccount : receivableAccount,
                Debit = sale.TotalAmount,
                Credit = 0,
                Notes = isPaid ? "نقدي" : $"عميل #{sale.CustomerId}"
            },
            new JournalLine
            {
                AccountId = salesRevenueAccount,
                Debit = 0,
                Credit = sale.TotalAmount,
                Notes = $"فاتورة #{sale.Id}"
            },
            new JournalLine
            {
                AccountId = cogsAccount,
                Debit = sale.TotalCost,
                Credit = 0,
                Notes = $"تكلفة فاتورة #{sale.Id}"
            },
            new JournalLine
            {
                AccountId = inventoryAccount,
                Debit = 0,
                Credit = sale.TotalCost,
                Notes = $"إخراج مخزون فاتورة #{sale.Id}"
            }
        }
        };

        ValidateEntry(entry);
        _context.JournalEntries.Add(entry);
        await _context.SaveChangesAsync();
    }
    public async Task CreatePurchaseEntryAsync(int supplierId, decimal amount, string reference, bool isPaid = false)
    {
        var inventoryAccount = await GetAccountIdAsync("Inventory");
        var supplierAccount = await GetAccountIdAsync("SupplierPayable");
        var cashAccount = await GetAccountIdAsync("Cash");

        var entry = new JournalEntry
        {
            Date = DateTime.UtcNow,
            Description = $"قيد شراء — {reference}",
            Reference = reference,
            Type = JournalEntryType.Purchase,
            CreatedBy = "system",
            CreatedAt = DateTime.UtcNow,
            Lines = new List<JournalLine>
            {
                new JournalLine
                {
                    AccountId = inventoryAccount,
                    Debit = amount,
                    Credit = 0,
                    Notes = reference
                },
                new JournalLine
                {
                    AccountId = isPaid ? cashAccount : supplierAccount,
                    Debit = 0,
                    Credit = amount,
                    Notes = isPaid ? "نقدي" : $"مورد #{supplierId}"
                }
            }
        };

        ValidateEntry(entry);
        _context.JournalEntries.Add(entry);
        await _context.SaveChangesAsync();
    }

    public async Task CreateStockAdjustmentEntryAsync(StockAdjustment adjustment, decimal cost)
    {
        var inventoryAccount = await GetAccountIdAsync("Inventory");
        var adjustmentAccount = await GetAccountIdAsync("StockAdjustment");

        var isAdd = adjustment.Type == AdjustmentType.Add;

        var entry = new JournalEntry
        {
            Date = adjustment.CreatedAt,
            Description = $"قيد تسوية مخزون — {adjustment.Reason}",
            Reference = $"ADJ-{adjustment.Id}",
            Type = JournalEntryType.StockAdjustment,
            CreatedBy = adjustment.CreatedBy,
            CreatedAt = DateTime.UtcNow,
            Lines = new List<JournalLine>
            {
                new JournalLine
                {
                    AccountId = inventoryAccount,
                    Debit  = isAdd ? cost : 0,
                    Credit = isAdd ? 0 : cost,
                    Notes = adjustment.Reason
                },
                new JournalLine
                {
                    AccountId = adjustmentAccount,
                    Debit  = isAdd ? 0 : cost,
                    Credit = isAdd ? cost : 0,
                    Notes = adjustment.Reason
                }
            }
        };

        ValidateEntry(entry);
        _context.JournalEntries.Add(entry);
        await _context.SaveChangesAsync();
    }

    public async Task CreatePaymentEntryAsync(int customerId, decimal amount, string reference)
    {
        var cashAccount = await GetAccountIdAsync("Cash");
        var receivableAccount = await GetAccountIdAsync("CustomerReceivable");

        var entry = new JournalEntry
        {
            Date = DateTime.UtcNow,
            Description = $"قيد تحصيل — عميل #{customerId}",
            Reference = reference,
            Type = JournalEntryType.Payment,
            CreatedBy = "system",
            CreatedAt = DateTime.UtcNow,
            Lines = new List<JournalLine>
            {
                new JournalLine
                {
                    AccountId = cashAccount,
                    Debit = amount,
                    Credit = 0,
                    Notes = $"عميل #{customerId}"
                },
                new JournalLine
                {
                    AccountId = receivableAccount,
                    Debit = 0,
                    Credit = amount,
                    Notes = reference
                }
            }
        };

        ValidateEntry(entry);
        _context.JournalEntries.Add(entry);
        await _context.SaveChangesAsync();
    }
    public async Task CreateMilkCollectionEntryAsync(MilkCollection milk)
    {
        var inventoryAccount = await GetAccountIdAsync("Inventory");
        var supplierAccount = await GetAccountIdAsync("SupplierPayable");

        var entry = new JournalEntry
        {
            Date = milk.CreatedAt,
            Description = $"قيد تحصيل لبن — مورد #{milk.SupplierId}",
            Reference = $"MILK-{milk.Id}",
            Type = JournalEntryType.Purchase,
            CreatedBy = "system",
            CreatedAt = DateTime.UtcNow,
            Lines = new List<JournalLine>
        {
            new JournalLine
            {
                AccountId = inventoryAccount,
                Debit = milk.TotalPrice,
                Credit = 0,
                Notes = $"استلام لبن — مورد #{milk.SupplierId}"
            },
            new JournalLine
            {
                AccountId = supplierAccount,
                Debit = 0,
                Credit = milk.TotalPrice,
                Notes = $"MILK-{milk.Id}"
            }
        }
        };

        ValidateEntry(entry);
        _context.JournalEntries.Add(entry);
        await _context.SaveChangesAsync();
    }
}