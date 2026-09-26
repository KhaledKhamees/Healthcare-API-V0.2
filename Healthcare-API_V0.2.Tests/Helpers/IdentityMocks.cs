using Healthcare_API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Healthcare_API_V0._2.Tests.Helpers;

public static class IdentityMocks
{
    public static Mock<UserManager<ApplicationUser>> MockUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var mock = new Mock<UserManager<ApplicationUser>>(
            store.Object,
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<IPasswordHasher<ApplicationUser>>(),
            Array.Empty<IUserValidator<ApplicationUser>>(),
            Array.Empty<IPasswordValidator<ApplicationUser>>(),
            Mock.Of<ILookupNormalizer>(),
            Mock.Of<IdentityErrorDescriber>(),
            Mock.Of<IServiceProvider>(),
            NullLogger<UserManager<ApplicationUser>>.Instance);
        return mock;
    }

    public static Mock<RoleManager<IdentityRole<Guid>>> MockRoleManager()
    {
        var store = new Mock<IRoleStore<IdentityRole<Guid>>>();
        var mock = new Mock<RoleManager<IdentityRole<Guid>>>(
            store.Object,
            Array.Empty<IRoleValidator<IdentityRole<Guid>>>(),
            Mock.Of<ILookupNormalizer>(),
            Mock.Of<IdentityErrorDescriber>(),
            NullLogger<RoleManager<IdentityRole<Guid>>>.Instance);
        return mock;
    }

    public static ApplicationUser NewUser(string email) =>
        new() { Id = Guid.NewGuid(), UserName = email, Email = email };

    public static string Sha256Hex(string raw)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash);
    }
}
