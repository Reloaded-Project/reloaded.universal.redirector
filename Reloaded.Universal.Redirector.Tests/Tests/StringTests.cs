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
            var text = RandomString(x, RandomStringUpperWithEmoji(x));
            var expected = Strings.GetNonRandomizedHashCode(text);

            for (int y = 0; y < 10; y++) 
                Assert.Equal(expected, Strings.GetNonRandomizedHashCode(text));
        }
    }

    /// <summary>
    /// Test of removing Windows prefixes.
    /// </summary>
    [Theory]
    [InlineData(@"\\?\undercurrents.txt", @"undercurrents.txt")]
    [InlineData(@"\\?\ocean\undercurrents.txt", @"ocean\undercurrents.txt")]
    [InlineData(@"\\.\undercurrents.txt", @"undercurrents.txt")]
    [InlineData(@"\\.\ocean\undercurrents.txt", @"ocean\undercurrents.txt")]
    [InlineData(@"\??\undercurrents.txt", @"undercurrents.txt")]
    [InlineData(@"\??\ocean\undercurrents.txt", @"ocean\undercurrents.txt")]
    public void TrimWindowsPrefixes(string value, string expected)
    {
        Assert.Equal(expected, Strings.TrimWindowsPrefixes(value).ToString());
    }

[Fact]
    public void Vectorised_ToUpper() => Vectorised_ToUpper_Common(RandomStringLower);

    /// <summary>
    /// Tests non-ASCII fallback.
    /// </summary>
    [Fact]
    public void Vectorised_ToUpper_WithEmoji() => Vectorised_ToUpper_Common(RandomStringLowerWithEmoji);

    [Fact]
    public void Vectorised_ToLower() => Vectorised_ToLower_Common(RandomStringUpper);
    
    /// <summary>
    /// Tests non-ASCII fallback.
    /// </summary>
    [Fact]
    public void Vectorised_ToLower_WithEmoji() => Vectorised_ToLower_Common(RandomStringUpperWithEmoji);

    private void Vectorised_ToLower_Common(Func<int, string> getStringWithLength)
    {
        // Note: We run this over such a big value range so this hits all implementations on a supported processor
        const int maxChars = 257;
        Span<char> actualBuf = stackalloc char[maxChars];
        for (int x = 0; x < maxChars; x++)
        {
            var text = getStringWithLength(x);
            var expected = text.ToLowerInvariant();
            TextInfo.ChangeCase<TextInfo.ToLowerConversion>(text, actualBuf);
            var actual = actualBuf[..expected.Length];
            Assert.True(expected.AsSpan().Equals(actual, StringComparison.Ordinal));
        }
    }
    
    private static void Vectorised_ToUpper_Common(Func<int, string> getStringWithLength)
    {
        // Note: We run this over such a big value range so this hits all implementations on a supported processor
        const int maxChars = 257;
        Span<char> actualBuf = stackalloc char[maxChars];
        for (int x = 0; x < maxChars; x++)
        {
            var text = getStringWithLength(x);
            var expected = text.ToUpperInvariant();
            TextInfo.ChangeCase<TextInfo.ToUpperConversion>(text, actualBuf);
            var actual = actualBuf[..expected.Length];
            Assert.True(expected.AsSpan().Equals(actual, StringComparison.Ordinal));
        }
    }

    private static string RandomString(int length, string charSet)
    {
        return new string(Enumerable.Repeat(charSet, length)
            .Select(s => s[_random.Next(s.Length)]).ToArray());
    }
    
    private static string RandomStringUpper(int length) => RandomString(length, "ABCDEFGHIJKLMNOPQRSTUVWXYZ");
    private static string RandomStringLower(int length) => RandomString(length, "abcdefghijklmnopqrstuvwxyz");
    
    private static string RandomStringUpperWithEmoji(int length) => RandomString(length, "ABCDEFGHIJKLMNOPQRSTUVWXYZ⚠️🚦🔺🏒😕🏞🖌🖕🌷☠⛩🍸👳🍠🚦📟💦🚏🌥🏪🌖😱");
    private static string RandomStringLowerWithEmoji(int length) => RandomString(length, "abcdefghijklmnopqrstuvwxyz⚠️🚦🔺🏒😕🏞🖌🖕🌷☠⛩🍸👳🍠🚦📟💦🚏🌥🏪🌖😱");
}