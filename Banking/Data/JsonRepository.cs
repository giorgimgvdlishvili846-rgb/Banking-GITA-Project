using System.Text.Json;
using Banking.Models;

namespace Banking.Data;

public class JsonRepository
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public JsonRepository(string filePath)
    {
        _filePath = filePath;
    }

    public BankData Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                throw new FileNotFoundException($"JSON ფაილი ვერ მოიძებნა: {_filePath}");

            string json = File.ReadAllText(_filePath);
            var data = JsonSerializer.Deserialize<BankData>(json, _options);

            if (data == null || data.Accounts.Count == 0)
                throw new InvalidDataException("JSON ფაილში ანგარიშები არ არის.");

            return data;
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"JSON ფაილის წაკითხვა ვერ მოხერხდა: {ex.Message}", ex);
        }
    }

    public void Save(BankData data)
    {
        try
        {
            string directory = Path.GetDirectoryName(_filePath)!;
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            string json = JsonSerializer.Serialize(data, _options);
            File.WriteAllText(_filePath, json);
        }
        catch (Exception ex)
        {
            throw new IOException($"JSON ფაილის შენახვა ვერ მოხერხდა: {ex.Message}", ex);
        }
    }
}
