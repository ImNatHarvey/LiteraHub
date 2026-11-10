
using System;
using System.Linq;

namespace LiteraHub.Services.Books;

public static class IsbnValidator
{
    public static string Normalize(string? isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn))
            return string.Empty;

        return new string(
            isbn.Trim()
                .Where(c => c != '-' && !char.IsWhiteSpace(c))
                .Select(char.ToUpperInvariant)
                .ToArray());
    }

    public static bool IsValid(string? isbn)
    {
        string value = Normalize(isbn);

        return value.Length switch
        {
            10 => IsValidIsbn10(value),
            13 => IsValidIsbn13(value),
            _ => false
        };
    }

    private static bool IsValidIsbn10(string value)
    {
        int sum = 0;

        for (int i = 0; i < 10; i++)
        {
            int digit;

            if (i == 9 && value[i] == 'X')
            {
                digit = 10;
            }
            else if (value[i] >= '0' && value[i] <= '9')
            {
                digit = value[i] - '0';
            }
            else
            {
                return false;
            }

            sum += digit * (10 - i);
        }

        return sum % 11 == 0;
    }

    private static bool IsValidIsbn13(string value)
    {
        if (!value.All(c => c >= '0' && c <= '9'))
            return false;

        int sum = 0;

        for (int i = 0; i < 13; i++)
        {
            int digit = value[i] - '0';

            sum += digit * (i % 2 == 0 ? 1 : 3);
        }

        return sum % 10 == 0;
    }
}
