using CapTap.Infrastructure.Services;
using FluentAssertions;

namespace CapTap.Infrastructure.Tests;

public class PasswordServiceTests
{
    private readonly PasswordService _sut = new();

    [Fact]
    public void HashPassword_Should_Not_Store_Plaintext()
    {
        const string password = "SecurePass1!";

        var hash = _sut.HashPassword(password);

        hash.Should().NotBe(password);
        hash.Should().StartWith("argon2id$");
        _sut.VerifyPassword(password, hash).Should().BeTrue();
        _sut.VerifyPassword("WrongPass1!", hash).Should().BeFalse();
    }
}
