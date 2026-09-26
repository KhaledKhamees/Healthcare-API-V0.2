using FluentAssertions;
using Healthcare_API_V0._2_Infrastructure.Identity;

namespace Healthcare_API_V0._2.Tests.Infrastructure.Identity;

public sealed class RefreshTokenGeneratorTests
{
    [Fact]
    public void GenerateRefreshToken_ReturnsNonEmptyToken()
    {
        var sut = new RefreshTokenGenerator();
        sut.GenerateRefreshToken().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GenerateRefreshToken_ProducesUniqueTokens()
    {
        var sut = new RefreshTokenGenerator();
        var tokens = Enumerable.Range(0, 100).Select(_ => sut.GenerateRefreshToken()).ToList();
        tokens.Distinct().Should().HaveCount(100);
    }

    [Fact]
    public void GenerateRefreshToken_UsesUrlSafeBase64WithoutPadding()
    {
        var sut = new RefreshTokenGenerator();
        foreach (var _ in Enumerable.Range(0, 20))
        {
            var token = sut.GenerateRefreshToken();
            token.Should().NotContain("+").And.NotContain("/").And.NotContain("=");
            token.Length.Should().Be(43, "32 random bytes base64url-encoded without padding is 43 chars");
        }
    }
}
