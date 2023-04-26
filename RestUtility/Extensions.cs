using System.Globalization;
using System.Text.RegularExpressions;

namespace RestUtility;

/// <summary>
/// Extension methods.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Determines if the input string contains the comparand.
    /// </summary>
    /// <param name="input">Input string.</param>
    /// <param name="comparand">String to compare with the input.</param>
    /// <param name="comparison">Comparison to use for the string search.</param>
    /// <returns>True if the string was found in the input; otherwise false.</returns>
    public static bool Contains(this string input, string comparand, StringComparison comparison = StringComparison.Ordinal)
    {
        return input.IndexOf(comparand, comparison) != -1;
    }

    /// <summary>
    /// Determines if a value is any of the specified arguments.
    /// </summary>
    /// <typeparam name="T">Template type of the value.</typeparam>
    /// <param name="value">The value to compare.</param>
    /// <param name="args">The expected values to compare against.</param>
    /// <returns>True if value equals any of the expected comparison values.</returns>
    public static bool EqualsAnyOf<T>(this IEquatable<T> value, params T[] args)
    {
        return args.Any(value.Equals);
    }

    /// <summary>
    /// Determines if a value is any of the specified arguments.
    /// </summary>
    /// <param name="value">The string value to compare.</param>
    /// <param name="comparison">The string comparison to use.</param>
    /// <param name="args">The expected values to compare against.</param>
    /// <returns>True if value equals any of the expected comparison values.</returns>
    public static bool EqualsAnyOf(this string value, StringComparison comparison = StringComparison.Ordinal, params string[] args)
    {
        return args.Any(v => value.Equals(v, comparison));
    }

    /// <summary>
    /// Compares two IComparable<typeparamref name="T"/> objects to determine whether the specified value is within the
    /// inclusive min/max range.
    /// </summary>
    /// <typeparam name="T">Type of value.</typeparam>
    /// <param name="value">Value to compare.</param>
    /// <param name="min">Inclusive min boundary.</param>
    /// <param name="max">Inclusive max boundary.</param>
    /// <returns>True if value is within the range; otherwise false.</returns>
    public static bool IsWithinRange<T>(this IComparable<T> value, T min, T max)
    {
        return value.CompareTo(min) >= 0 && value.CompareTo(max) <= 0;
    }

    /// <summary>
    /// Compares two IComparable<typeparamref name="T"/> objects to determine whether the specified value is within the
    /// exclusive min/max range.
    /// </summary>
    /// <typeparam name="T">Type of value.</typeparam>
    /// <param name="value">Value to compare.</param>
    /// <param name="min">Exclusive min boundary.</param>
    /// <param name="max">Exclusive max boundary.</param>
    /// <returns>True if value is within the range; otherwise false.</returns>
    public static bool IsBetweenRange<T>(this IComparable<T> value, T min, T max)
    {
        return value.CompareTo(min) > 0 && value.CompareTo(max) < 0;
    }

    /// <summary>
    /// Strips surrounding double quotes from an input string.
    /// </summary>
    /// <param name="input">Input string.</param>
    /// <returns>Input string with the surrounding double quotes removed.</returns>
    public static string StripDoubleQuotes(this string input)
    {
        return input.Length >= 2 && input[0] == input[input.Length - 1] && input[0] == '"'
            ? input.Substring(1, input.Length - 2)
            : input;
    }

    /// <summary>
    /// Tokenizes the string into string arguments based on spaces or values in quotes
    /// similar to the way command line arguments are processed.
    /// </summary>
    /// <param name="input">Input string.</param>
    /// <returns>Command line tokens.</returns>
    public static IEnumerable<string> TokenizeParams(this string input)
    {
        var inQuotes = false;
        return input.Split(c =>
            {
                if (c == '\"')
                    inQuotes = !inQuotes;
                return !inQuotes && c == ' ';
            }).Select(arg => arg.Trim().StripDoubleQuotes())
            .Where(arg => !string.IsNullOrEmpty(arg));
    }

    /// <summary>
    /// Splits a string up into tokens using the predicate func as a controller.
    /// </summary>
    /// <param name="input">Input string.</param>
    /// <param name="controller">Controller function.</param>
    /// <returns>IEnumerable collection of string tokens.</returns>
    public static IEnumerable<string> Split(this string input, Func<char, bool> controller)
    {
        var n = 0;
        for (var c = 0; c < input.Length; c++)
        {
            if (!controller(input[c]))
                continue;
            yield return input.Substring(n, c - n);
            n = c + 1;
        }

        yield return input.Substring(n);
    }

    /// <summary>
    /// Returns the next element from the enumerator.
    /// </summary>
    /// <param name="enumerator">Object enumerator.</param>
    /// <typeparam name="T"></typeparam>
    /// <returns>Next element.</returns>
    public static T? Next<T>(this IEnumerator<T> enumerator)
    {
        return enumerator.MoveNext() ? enumerator.Current : default;
    }

    /// <summary>
    /// Returns the remaining sequence of elements from the enumerator.
    /// </summary>
    /// <param name="enumerator">Object enumerator.</param>
    /// <typeparam name="T"></typeparam>
    /// <returns>Remaining sequence of elements.</returns>
    public static IEnumerable<T> Remaining<T>(this IEnumerator<T> enumerator)
    {
        while (enumerator.MoveNext())
            yield return enumerator.Current;
    }

    /// <summary>
    /// Determines if the next enumerator element is a certain value after
    /// moving the enumerator to the next position.
    /// </summary>
    /// <param name="enumerator">Object enumerator.</param>
    /// <param name="other">Value to compare.</param>
    /// <typeparam name="T"></typeparam>
    /// <returns>True if the next enumerator element is equal to the specified value; otherwise false.</returns>
    public static bool NextEquals<T>(this IEnumerator<T> enumerator, T other)
    {
        T? obj = enumerator.Next();
        if (obj is null)
            return ReferenceEquals(other, null);
        return obj.Equals(other);
    }

    /// <summary>
    /// Determines if the next enumerator string element is equal to the specified
    /// value using a specific string comparision.
    /// </summary>
    /// <param name="enumerator">String enumerator.</param>
    /// <param name="other">Value to compare.</param>
    /// <param name="comparison">String comparison.</param>
    /// <returns>True if the next enumerator element is equal to the specified string; otherwise false.</returns>
    public static bool NextEquals(this IEnumerator<string> enumerator, string? other, StringComparison comparison)
    {
        var str = enumerator.Next();
        if (str is null)
            return other is null;
        return str.Equals(other, comparison);
    }

    /// <summary>
    /// Restricts the input to the inclusive min and max values specified.
    /// </summary>
    /// <param name="input">Input value.</param>
    /// <param name="min">Inclusive minimum value.</param>
    /// <param name="max">Inclusive maximum value.</param>
    /// <typeparam name="T"></typeparam>
    /// <returns>Resultant value restricted to the min and max value range.</returns>
    public static T RestrictValue<T>(this T input, T min, T max)
        where T : struct, IComparable<T>
    {
        if (input.CompareTo(min) < 0)
            return min;
        if (input.CompareTo(max) > 0)
            return max;
        return input;
    }

    /// <summary>
    /// Translates the byte sequence to a hexadecimal string sequence.
    /// </summary>
    /// <param name="bytes">Input bytes.</param>
    /// <param name="uppercase">True to set the output to uppercase; false for lowercase.</param>
    /// <returns>The hexadecimal string sequence to represent the input byte sequence.</returns>
    public static string ToHex(this IEnumerable<byte> bytes, bool uppercase)
        => string.Concat(bytes.Select(b => b.ToString(uppercase ? "X2" : "x2", CultureInfo.InvariantCulture)).ToArray());

    /// <summary>
    /// Translates the byte sequence to a hexadecimal string sequence in lowercase.
    /// </summary>
    /// <param name="bytes">Input bytes.</param>
    /// <returns>The hexadecimal string sequence to represent the input byte sequence.</returns>
    public static string ToLowerHex(this IEnumerable<byte> bytes) => bytes.ToHex(false);

    /// <summary>
    /// Translates the byte sequence to a hexadecimal string sequence in uppercase.
    /// </summary>
    /// <param name="bytes">Input bytes.</param>
    /// <returns>The hexadecimal string sequence to represent the input byte sequence.</returns>
    public static string ToUpperHex(this IEnumerable<byte> bytes) => bytes.ToHex(true);

    public static bool SequenceMatches<T>(this IReadOnlyList<T> array, IReadOnlyList<T> pattern, int startIndex)
    {
        if (array is null)
            throw new ArgumentNullException(nameof(array));
        if (pattern is null)
            throw new ArgumentNullException(nameof(pattern));
        if (startIndex >= array.Count)
            throw new ArgumentOutOfRangeException(nameof(startIndex), "Start index must be within the array boundaries.");

        if (pattern.Count > array.Count - startIndex)
            return false;

        for (var i = 0; i < pattern.Count; i++)
        {
            if (!Equals(array[startIndex + i], pattern[i]))
                return false;
        }

        return true;
    }

    public static int FindIndexOfSequence<T>(this T[] array, T[] pattern, int offset, int count = -1)
    {
        if (array is null)
            throw new ArgumentNullException(nameof(array));
        if (pattern is null)
            throw new ArgumentNullException(nameof(pattern));
        if (offset > array.Length)
            throw new ArgumentOutOfRangeException(nameof(offset), "Offset is greater than the length of the array.");
        if (offset + count > array.Length)
            throw new ArgumentOutOfRangeException(nameof(count), "Count from the specified offset must be within the array boundaries.");

        if (pattern.Length == 0)
            return 0;

        if (pattern.Length > array.Length)
            return -1;

        if (count == -1)
            count = array.Length - offset;

        for (int i = offset, cnt = 0; i < array.Length && cnt < count; i++, cnt++)
        {
            if (array.SequenceMatches(pattern, i))
                return i;
        }

        return -1;
    }

    public static bool ContainsAllExpectedValues(this string[] values, string[] expectedValues, out IReadOnlyCollection<string> missingValues)
    {
        // We sort the arrays so that we can compare more efficiently in O(N) time.
        Array.Sort(values, StringComparer.InvariantCulture);
        Array.Sort(expectedValues, StringComparer.InvariantCulture);

        int i = 0, j = 0;
        var missing = new List<string>(expectedValues.Length);
        while (i < expectedValues.Length && j < values.Length)
        {
            var result = string.Compare(expectedValues[i], values[j], StringComparison.InvariantCulture);
            if (result < 0)
            {
                // Advance pointer within the expected values array because the
                // current value is lexicographically greater than the current value in comparison.
                // (This means that the current expected value will not be found later in the input values array
                // due to the fact that we're comparing the sequences in sorted orders.)
                missing.Add(expectedValues[i]);
                i++;
            }
            else if (result > 0)
            {
                // Advance pointer within the input values array because the
                // current expected value is lexicographically greater than the current value in comparison.
                j++;
            }
            else // Equal
            {
                i++;
                j++;
            }
        }

        // We ran out of comparisons to make, so if there are any other remaining expected values
        // yet to be compared, it's because they were never found.
        while (i < expectedValues.Length)
        {
            missing.Add(expectedValues[i]);
            i++;
        }

        missingValues = missing;
        return missing.Count == 0;
    }

    public static string GetMixedHexadecimalString(this byte[]? bytes)
    {
        if (bytes is null)
            return string.Empty;

        return string.Concat(
            bytes.Select(b => b is >= 20 and < 127 && !char.IsControl((char)b)
                ? $"{(char)b}"
                : $"\\x{b:X2}").ToArray()
        );
    }

    public static string GetFullHexadecimalString(this byte[]? buffer)
    {
        if (buffer is null)
            return string.Empty;

        return string.Concat(buffer.Select(b => $"\\x{b:X2}").ToArray());
    }

    public static string UnescapeString(this string? input)
    {
        return Regex.Unescape(input ?? string.Empty);
    }
}
