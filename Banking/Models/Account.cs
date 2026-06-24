namespace Banking.Models;

public class Transaction
{
    public DateTime TransactionDate { get; set; } = DateTime.Now;
    public string TransactionType { get; set; } = string.Empty;
    public decimal AmountGEL { get; set; }
    public decimal AmountUSD { get; set; }
    public decimal AmountEUR { get; set; }
}

public class Account
{
    public string CardNumber { get; set; } = string.Empty;
    public string ExpiryDate { get; set; } = string.Empty;
    public string Pin { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "GEL";
    public List<Transaction> Transactions { get; set; } = new();
}

public class BankData
{
    public List<Account> Accounts { get; set; } = new();
    public Dictionary<string, decimal> ExchangeRates { get; set; } = new();
}
