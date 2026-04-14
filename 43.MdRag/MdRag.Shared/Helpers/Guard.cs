using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace MdRag.Shared.Helpers;

/// <summary>
/// Lightweight argument guard helpers used throughout the solution.
/// Avoids taking a dependency on a full guard library.
/// </summary>
public static class Guard
{
    public static string NotNullOrWhiteSpace(
    string? value,
    [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value must not be null or whitespace.", paramName);
        return value;
    }

    public static T NotNull<T>(
        T? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : class
    {
        if (value is null)
            throw new ArgumentNullException(paramName);
        return value;
    }

    public static int Positive(
        int value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(paramName, value, "Value must be positive.");
        return value;
    }
}
