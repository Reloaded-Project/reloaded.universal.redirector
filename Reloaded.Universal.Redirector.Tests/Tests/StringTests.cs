using Reloaded.Universal.Redirector.Lib.Backports.System.Globalization;
using Reloaded.Universal.Redirector.Lib.Utility;

namespace Reloaded.Universal.Redirector.Tests.Tests;

public class StringTests
{
    private static Random _random = new Random();

    // TODO: The tests here could be more diverse. We're only scratching the surface of the barrel.
    
    /// <summary>
    /// Test for hashing long strings.
    /// This is just a baseline quick test.
    /// </summary>
    [Fact]
    public void HashString()
    {
        for (int x = 0; x < 257; x++)
        {
            var text = RandomString(x);
            var expected = Strings.GetNonRandomizedHashCode(text);

            for (int y = 0; y < 10; y++) 
                Assert.Equal(expected, Strings.GetNonRandomizedHashCode(text));
        }
    }

    /// <summary>
    /// Quick test of vectorised ToUpper.
    /// </summary>
    [Fact]
    public void ToUpper()
    {
        const int maxChars = 257;
        Span<char> actualBuf = stackalloc char[maxChars];
        for (int x = 0; x < maxChars; x++)
        {
            var text = RandomStringLower(x);
            var expected = text.ToUpperInvariant();
            TextInfo.ChangeCase<TextInfo.ToUpperConversion>(text, actualBuf);
            Assert.True(expected.AsSpan().Equals(actualBuf.Slice(0, expected.Length), StringComparison.Ordinal));
        }
    }

    private static string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[_random.Next(s.Length)]).ToArray());
    }
    
    private static string RandomStringLower(int length)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[_random.Next(s.Length)]).ToArray());
    }
}