using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DiscordArchitect.Services;
using Moq;
using Xunit;

namespace DiscordArchitect.Tests.UnitTests;

public class PermissionPlannerTests
{
    [Fact]
    public async Task GrantCategoryRoleAsync_AddsExpectedOverwrite()
    {
        var category = new Mock<ICategoryChannel>(MockBehavior.Strict);
        var role = new Mock<IRole>(MockBehavior.Strict);

        category
            .Setup(c => c.AddPermissionOverwriteAsync(
                role.Object,
                It.Is<OverwritePermissions>(p =>
                    p.ViewChannel == PermValue.Allow &&
                    p.SendMessages == PermValue.Allow),
                null))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var sut = new PermissionPlanner();
        await sut.GrantCategoryRoleAsync(category.Object, role.Object);

        category.Verify();
    }

    [Fact]
    public async Task EnsureBotAccessAsync_AllowsBotViewManageSend()
    {
        var category = new Mock<ICategoryChannel>(MockBehavior.Strict);
        var botUser = new Mock<IUser>(MockBehavior.Strict); // změna tady

        category
            .Setup(c => c.AddPermissionOverwriteAsync(
                botUser.Object,
                It.Is<OverwritePermissions>(p =>
                    p.ViewChannel == PermValue.Allow &&
                    p.ManageChannel == PermValue.Allow &&
                    p.SendMessages == PermValue.Allow),
                null))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var sut = new PermissionPlanner();
        await sut.EnsureBotAccessAsync(category.Object, botUser.Object);

        category.Verify();
    }

    [Fact]
    public async Task ApplyEveryoneToggleAsync_DenyWhenDisabled()
    {
        var category = new Mock<ICategoryChannel>(MockBehavior.Strict);
        var guild = new Mock<IGuild>(MockBehavior.Strict);
        var everyone = new Mock<IRole>(MockBehavior.Strict);

        guild.Setup(g => g.EveryoneRole).Returns(everyone.Object);

        category
            .Setup(c => c.AddPermissionOverwriteAsync(
                everyone.Object,
                It.Is<OverwritePermissions>(p => p.ViewChannel == PermValue.Deny),
                null))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var sut = new PermissionPlanner();
        await sut.ApplyEveryoneToggleAsync(category.Object, guild.Object, everyoneAccess: false);

        category.Verify();
    }

}
