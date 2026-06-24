using Banking.Data;
using Banking.Logging;
using Banking.Models;
using Banking.Services;

namespace Banking;

class Program
{
    private const int MaxPinAttempts = 3;

    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;

        string baseDir = AppContext.BaseDirectory;
        string jsonPath = Path.Combine(baseDir, "Data", "bank.json");
        string logPath = Path.Combine(baseDir, "Logs", "banking.log");

        var logger = new FileLogger(logPath);
        var repository = new JsonRepository(jsonPath);

        logger.Info("აპლიკაცია გაეშვა.");

        BankData bankData;

        try
        {
            bankData = repository.Load();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"შეცდომა: {ex.Message}");
            logger.Error(ex.Message);
            return;
        }

        var accountService = new AccountService(bankData, repository, logger);

        Console.WriteLine("=== ბანკომატის სიმულატორი ===\n");

        while (true)
        {
            Console.WriteLine("0. გასვლა");
            Console.WriteLine("1. ბარათის შეყვანა");
            Console.Write("აირჩიეთ: ");

            if (!TryReadInt(out int startChoice) || startChoice == 0)
            {
                Console.WriteLine("\nმადლობა, რომ გამოიყენეთ ჩვენს სერვისს!");
                logger.Info("აპლიკაცია დახურულია.");
                break;
            }

            if (startChoice != 1)
            {
                Console.WriteLine("არასწორი არჩევანი.\n");
                continue;
            }

            if (!TryAuthenticate(bankData, accountService, logger))
            {
                accountService.ClearSession();
                Console.WriteLine("\nსისტემიდან გამოგდება. სცადეთ თავიდან.\n");
                continue;
            }

            RunMenu(accountService, logger);
            accountService.ClearSession();
            Console.WriteLine("\n--- დაბრუნდით საწყის ეკრანზე ---\n");
        }
    }

    static bool TryAuthenticate(BankData bankData, AccountService accountService, FileLogger logger)
    {
        Console.Write("\nბარათის ნომერი (16 ციფრი): ");
        string cardNumber = Console.ReadLine()?.Trim() ?? string.Empty;

        if (!CardValidator.IsValidCardNumber(cardNumber))
        {
            Console.WriteLine("შეცდომა: ბარათის ნომერი არასწორია.");
            logger.Warning($"არასწორი ბარათის ნომერი: {CardValidator.MaskCardNumber(cardNumber)}");
            return false;
        }

        Console.Write("ბარათის ვადის გასვლის თარიღი (MM/YY): ");
        string expiryDate = Console.ReadLine()?.Trim() ?? string.Empty;

        if (!CardValidator.IsValidExpiryDate(expiryDate))
        {
            Console.WriteLine("შეცდომა: ბარათის ვადა არასწორია ან უკვე გასულია.");
            logger.Warning($"არასწორი ვადა: {expiryDate}");
            return false;
        }

        Account? account = CardValidator.FindAccount(bankData, cardNumber, expiryDate);

        if (account == null)
        {
            Console.WriteLine("შეცდომა: ბარათი ვერ მოიძებნა.");
            logger.Warning("ბარათი ვერ მოიძებნა.");
            return false;
        }

        for (int attempt = 1; attempt <= MaxPinAttempts; attempt++)
        {
            Console.Write($"PIN კოდი (მცდელობა {attempt}/{MaxPinAttempts}): ");
            string pin = Console.ReadLine()?.Trim() ?? string.Empty;

            if (!CardValidator.IsValidPinFormat(pin))
            {
                Console.WriteLine("შეცდომა: PIN უნდა იყოს 4 ციფრი.");
                logger.Warning($"არასწორი PIN ფორმატი, მცდელობა {attempt}");
                continue;
            }

            accountService.SetCurrentAccount(account);

            if (accountService.ValidatePin(pin))
            {
                Console.WriteLine("\nავტორიზაცია წარმატებულია!\n");
                logger.Info($"წარმატებული ავტორიზაცია: ****{account.CardNumber[^4..]}");
                return true;
            }

            Console.WriteLine("შეცდომა: PIN კოდი არასწორია.");
            logger.Warning($"არასწორი PIN, მცდელობა {attempt}");
        }

        Console.WriteLine("PIN კოდის მცდელობები ამოიწურა.");
        logger.Warning("PIN მცდელობები ამოიწურა.");
        return false;
    }

    static void RunMenu(AccountService accountService, FileLogger logger)
    {
        while (true)
        {
            Console.WriteLine("--- მენიუ ---");
            Console.WriteLine("1. ნაშთის ნახვა");
            Console.WriteLine("2. თანხის გამოტანა");
            Console.WriteLine("3. ბოლო 5 ოპერაცია");
            Console.WriteLine("4. თანხის შეტანა");
            Console.WriteLine("5. PIN კოდის შეცვლა");
            Console.WriteLine("6. ვალუტის კონვერტაცია");
            Console.WriteLine("0. გასვლა (ბარათის ამოღება)");
            Console.Write("აირჩიეთ: ");

            if (!TryReadInt(out int choice))
            {
                Console.WriteLine("არასწორი არჩევანი.\n");
                continue;
            }

            try
            {
                switch (choice)
                {
                    case 0:
                        Console.WriteLine("ბარათი ამოღებულია.");
                        return;
                    case 1:
                        ShowBalance(accountService);
                        break;
                    case 2:
                        Withdraw(accountService);
                        break;
                    case 3:
                        ShowLastTransactions(accountService);
                        break;
                    case 4:
                        Deposit(accountService);
                        break;
                    case 5:
                        ChangePin(accountService);
                        break;
                    case 6:
                        ConvertCurrency(accountService);
                        break;
                    default:
                        Console.WriteLine("არასწორი არჩევანი.\n");
                        continue;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"შეცდომა: {ex.Message}");
                logger.Error(ex.Message);
            }

            Console.WriteLine();
        }
    }

    static void ShowBalance(AccountService accountService)
    {
        decimal balance = accountService.GetBalance();
        string currency = accountService.GetCurrency();
        Console.WriteLine($"\nმიმდინარე ნაშთი: {balance:F2} {currency}");
    }

    static void Withdraw(AccountService accountService)
    {
        Console.Write("გამოსატანი თანხა: ");
        if (!TryReadDecimal(out decimal amount))
        {
            Console.WriteLine("არასწორი თანხა.");
            return;
        }

        string currency = accountService.GetCurrency();
        if (accountService.Withdraw(amount))
            Console.WriteLine($"წარმატებით გამოიტანეთ {amount:F2} {currency}.");
        else
            Console.WriteLine("შეცდომა: საკმარისი თანხა არ გაქვთ.");
    }

    static void Deposit(AccountService accountService)
    {
        Console.Write("შესატანი თანხა: ");
        if (!TryReadDecimal(out decimal amount))
        {
            Console.WriteLine("არასწორი თანხა.");
            return;
        }

        accountService.Deposit(amount);
        Console.WriteLine($"წარმატებით შეიტანეთ {amount:F2} {accountService.GetCurrency()}.");
    }

    static void ShowLastTransactions(AccountService accountService)
    {
        var transactions = accountService.GetLastTransactions(5);

        Console.WriteLine("\n--- ბოლო 5 ოპერაცია ---");

        if (transactions.Count == 0)
        {
            Console.WriteLine("ოპერაციები არ არის.");
            return;
        }

        foreach (var t in transactions)
        {
            string amountText = t.AmountGEL > 0 ? $"{t.AmountGEL:F2} GEL"
                : t.AmountUSD > 0 ? $"{t.AmountUSD:F2} USD"
                : $"{t.AmountEUR:F2} EUR";

            Console.WriteLine($"[{t.TransactionDate:yyyy-MM-dd HH:mm}] {t.TransactionType} | {amountText}");
        }
    }

    static void ChangePin(AccountService accountService)
    {
        Console.Write("მიმდინარე PIN: ");
        string oldPin = Console.ReadLine()?.Trim() ?? string.Empty;

        Console.Write("ახალი PIN (4 ციფრი): ");
        string newPin = Console.ReadLine()?.Trim() ?? string.Empty;

        Console.Write("ახალი PIN-ის დადასტურება: ");
        string confirmPin = Console.ReadLine()?.Trim() ?? string.Empty;

        if (newPin != confirmPin)
        {
            Console.WriteLine("შეცდომა: PIN-ები არ ემთხვევა.");
            return;
        }

        if (accountService.ChangePin(oldPin, newPin))
            Console.WriteLine("PIN კოდი წარმატებით შეიცვალა.");
        else
            Console.WriteLine("შეცდომა: მიმდინარე PIN არასწორია.");
    }

    static void ConvertCurrency(AccountService accountService)
    {
        string currentCurrency = accountService.GetCurrency();
        decimal currentBalance = accountService.GetBalance();

        Console.WriteLine($"\nმიმდინარე ნაშთი: {currentBalance:F2} {currentCurrency}");
        Console.WriteLine("ხელმისაწვდომი ვალუტები: USD, EUR, GEL");
        Console.Write("სასურველი ვალუტა: ");
        string targetCurrency = Console.ReadLine()?.Trim().ToUpperInvariant() ?? string.Empty;

        Console.Write($"გასაკონვერტირებელი თანხა ({currentCurrency}): ");
        if (!TryReadDecimal(out decimal amount))
        {
            Console.WriteLine("არასწორი თანხა.");
            return;
        }

        if (accountService.ConvertCurrency(targetCurrency, amount, out decimal newBalance))
            Console.WriteLine($"კონვერტაცია წარმატებულია. ახალი ნაშთი: {newBalance:F2} {targetCurrency}");
        else
            Console.WriteLine("შეცდომა: საკმარისი თანხა არ გაქვთ.");
    }

    static bool TryReadInt(out int value)
    {
        value = 0;
        string? input = Console.ReadLine()?.Trim();
        return int.TryParse(input, out value);
    }

    static bool TryReadDecimal(out decimal value)
    {
        value = 0;
        string? input = Console.ReadLine()?.Trim();
        return decimal.TryParse(input, out value);
    }
}
