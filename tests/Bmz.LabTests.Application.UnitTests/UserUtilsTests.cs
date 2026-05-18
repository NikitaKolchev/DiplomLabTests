using Bmz.LabTests.Application;
using FluentAssertions;

namespace Bmz.LabTests.Application.UnitTests;

public sealed class UserUtilsTests
{
    [Theory]
    [InlineData("Иван Иванов", "Ivan.Ivanov")]
    [InlineData("Тестовый Пользователь", "Testovyj.Polzovatel")]
    public void Transliterate_ShouldConvertCyrillicToLatin(string input, string expected)
    {
        var result = UserUtils.Transliterate(input);
        result.Should().Be(expected);
    }

    [Fact]
    public void Transliterate_ShouldReplaceSpacesWithDots_AndRemoveOtherWhitespace()
    {
        var result = UserUtils.Transliterate("Иван\t Иванов\nПетров");
        result.Should().Be("Ivan.IvanovPetrov");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(32)]
    public void GeneratePassword_ShouldReturnRequestedLength(int length)
    {
        var password = UserUtils.GeneratePassword(length);
        password.Should().HaveLength(length);
    }

    [Fact]
    public void GeneratePassword_ShouldOnlyUseAllowedCharacters()
    {
        const string allowed = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*";

        var password = UserUtils.GeneratePassword(100);
        password.All(c => allowed.Contains(c)).Should().BeTrue();
    }
}

