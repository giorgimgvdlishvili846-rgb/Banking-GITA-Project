using Banking.Models;

namespace Banking.Services;

public static class CardValidator
{
    public static bool IsValidCardNumber(string cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber))
            return false;

        string digits = new(cardNumber.Where(char.IsDigit).ToArray());

        if (digits.Length != 16)
            return false;

        return PassesLuhnCheck(digits);
    }

    public static bool IsValidExpiryDate(string expiryDate)
    {
        if (string.IsNullOrWhiteSpace(expiryDate))
            return false;

        string cleaned = expiryDate.Trim().Replace(" ", "");
        string[] parts = cleaned.Split('/');

        if (parts.Length != 2)
            return false;

        if (!int.TryParse(parts[0], out int month) || !int.TryParse(parts[1], out int year))
            return false;

        if (month is < 1 or > 12)
            return false;

        if (parts[1].Length == 2)
            year += 2000;

        if (year < 2000 || year > 2100)
            return false;

        var expiry = new DateTime(year, month, DateTime.DaysInMonth(year, month), 23, 59, 59);
        return expiry >= DateTime.Now;
    }

    public static bool IsValidPinFormat(string pin)
    {
        return pin.Length == 4 && pin.All(char.IsDigit);
    }

    public static string MaskCardNumber(string cardNumber)
    {
        string digits = new(cardNumber.Where(char.IsDigit).ToArray());
        if (digits.Length < 4)
            return "****";

        return "****" + digits[^4..];
    }

    public static Account? FindAccount(BankData data, string cardNumber, string expiryDate)
    {
        string normalizedCard = new(cardNumber.Where(char.IsDigit).ToArray());
        string normalizedExpiry = expiryDate.Trim();

        return data.Accounts.FirstOrDefault(a =>
            new string(a.CardNumber.Where(char.IsDigit).ToArray()) == normalizedCard &&
            a.ExpiryDate.Trim().Equals(normalizedExpiry, StringComparison.OrdinalIgnoreCase));
    }

    private static bool PassesLuhnCheck(string digits)
    {
        int sum = 0;
        bool alternate = false;

        for (int i = digits.Length - 1; i >= 0; i--)
        {
            int n = digits[i] - '0';

            if (alternate)
            {
                n *= 2;
                if (n > 9)
                    n -= 9;
            }

            sum += n;
            alternate = !alternate;
        }

        return sum % 10 == 0;
    }
}
