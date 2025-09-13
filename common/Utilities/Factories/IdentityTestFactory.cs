using Diiwo.Identity.AspNet.DbContext;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using IdentityRole = Diiwo.Identity.AspNet.Entities.IdentityRole;
using IdentityUser = Diiwo.Identity.AspNet.Entities.IdentityUser;

namespace Diiwo.Common.Tests.Utilities.Factories;

public static class IdentityTestFactory
{
    public static UserManager<IdentityUser> CreateUserManager(AspNetIdentityDbContext context)
    {
        var store = new UserStore<IdentityUser, IdentityRole, AspNetIdentityDbContext, Guid>(context);

        var options = new Mock<Microsoft.Extensions.Options.IOptions<IdentityOptions>>();
        options.Setup(o => o.Value).Returns(new IdentityOptions());

        return new UserManager<IdentityUser>(
            store,
            options.Object,
            new PasswordHasher<IdentityUser>(),
            new[] { new UserValidator<IdentityUser>() },
            new[] { new PasswordValidator<IdentityUser>() },
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            new Mock<IServiceProvider>().Object,
            new Mock<ILogger<UserManager<IdentityUser>>>().Object
        );
    }

    public static RoleManager<IdentityRole> CreateRoleManager(AspNetIdentityDbContext context)
    {
        var store = new RoleStore<IdentityRole, AspNetIdentityDbContext, Guid>(context);
        return new RoleManager<IdentityRole>(
            store,
            new IRoleValidator<IdentityRole>[] { new RoleValidator<IdentityRole>() },
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            new Mock<ILogger<RoleManager<IdentityRole>>>().Object
        );
    }
}
