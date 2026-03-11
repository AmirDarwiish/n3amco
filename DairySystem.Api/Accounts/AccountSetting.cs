public class AccountSetting
{
    public int Id { get; set; }
    public string Key { get; set; }   // مثال: "Inventory"
    public int AccountId { get; set; }
    public Account Account { get; set; }
}