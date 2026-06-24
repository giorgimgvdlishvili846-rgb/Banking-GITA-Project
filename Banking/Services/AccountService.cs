using Banking.Data;
using Banking.Logging;
using Banking.Models;

namespace Banking.Services;

public class AccountService
{
    private readonly BankData _data;
    private readonly JsonRepository _repository;
    private readonly FileLogger _logger;

    public AccountService(BankData data, JsonRepository repository, FileLogger logger)
    {
        _data = data;
        _repository = repository;
        _logger = logger;
    }

    public Account? CurrentAccount { get; private set; }

    public void SetCurrentAccount(Account account)
    {
        CurrentAccount = account;
    }

    public void ClearSession()
    {
        CurrentAccount = null;
    }

    public bool ValidatePin(string pin)
    {
        if (CurrentAccount == null)
            return false;

        return CurrentAccount.Pin == pin;
    }

    public decimal GetBalance()
    {
        EnsureLoggedIn();
        return CurrentAccount!.Balance;
    }

    public bool Withdraw(decimal amount)
    {
        EnsureLoggedIn();

        if (amount <= 0)
            throw new ArgumentException("თანხა უნდა იყოს დადებითი.");

        if (amount > CurrentAccount!.Balance)
            return false;

        CurrentAccount.Balance -= amount;
        AddTransaction("Withdraw", amount, CurrentAccount.Currency);
        Save();
        _logger.Info($"გამოტანა: {amount} {CurrentAccount.Currency}, ბარათი: {MaskCard(CurrentAccount.CardNumber)}");
        return true;
    }

    public void Deposit(decimal amount)
    {
        EnsureLoggedIn();

        if (amount <= 0)
            throw new ArgumentException("თანხა უნდა იყოს დადებითი.");

        CurrentAccount!.Balance += amount;
        AddTransaction("Deposit", amount, CurrentAccount.Currency);
        Save();
        _logger.Info($"შეტანა: {amount} {CurrentAccount.Currency}, ბარათი: {MaskCard(CurrentAccount.CardNumber)}");
    }

    public List<Transaction> GetLastTransactions(int count = 5)
    {
        EnsureLoggedIn();
        return CurrentAccount!.Transactions
            .OrderByDescending(t => t.TransactionDate)
            .Take(count)
            .ToList();
    }

    public bool ChangePin(string oldPin, string newPin)
    {
        EnsureLoggedIn();

        if (CurrentAccount!.Pin != oldPin)
            return false;

        if (string.IsNullOrWhiteSpace(newPin) || newPin.Length != 4 || !newPin.All(char.IsDigit))
            throw new ArgumentException("ახალი PIN უნდა იყოს 4 ციფრი.");

        CurrentAccount.Pin = newPin;
        AddTransaction("PinChange", 0, CurrentAccount.Currency);
        Save();
        _logger.Info($"PIN შეიცვალა, ბარათი: {MaskCard(CurrentAccount.CardNumber)}");
        return true;
    }

    public decimal ConvertCurrency(string targetCurrency, decimal amount)
    {
        EnsureLoggedIn();

        if (amount <= 0)
            throw new ArgumentException("თანხა უნდა იყოს დადებითი.");

        string sourceCurrency = CurrentAccount!.Currency.ToUpperInvariant();
        targetCurrency = targetCurrency.ToUpperInvariant();

        if (!_data.ExchangeRates.ContainsKey(sourceCurrency) ||
            !_data.ExchangeRates.ContainsKey(targetCurrency))
            throw new ArgumentException("ვალუტა არ არის მხარდაჭერილი.");

        decimal sourceRate = _data.ExchangeRates[sourceCurrency];
        decimal targetRate = _data.ExchangeRates[targetCurrency];
        decimal converted = amount * sourceRate / targetRate;

        AddTransaction($"CurrencyConversion {sourceCurrency}->{targetCurrency}", amount, sourceCurrency);
        Save();
        _logger.Info($"ვალუტის კონვერტაცია: {amount} {sourceCurrency} -> {converted:F2} {targetCurrency}");

        return converted;
    }

    private void AddTransaction(string type, decimal amount, string currency)
    {
        var transaction = new Transaction
        {
            TransactionType = type,
            TransactionDate = DateTime.Now
        };

        switch (currency.ToUpperInvariant())
        {
            case "USD":
                transaction.AmountUSD = amount;
                break;
            case "EUR":
                transaction.AmountEUR = amount;
                break;
            default:
                transaction.AmountGEL = amount;
                break;
        }

        CurrentAccount!.Transactions.Add(transaction);
    }

    private void Save()
    {
        _repository.Save(_data);
    }

    private void EnsureLoggedIn()
    {
        if (CurrentAccount == null)
            throw new InvalidOperationException("ანგარიში არ არის არჩეული.");
    }

    private static string MaskCard(string cardNumber)
    {
        if (cardNumber.Length < 4)
            return "****";

        return "**** **** **** " + cardNumber[^4..];
    }
}
