using Discord;
using Discord.Interactions;
using Forgotten.Bot.Commands;

namespace Forgotten.Nft.IntegrationTests;

/// <summary>
/// Guards the fail-closed authorization on admin commands (spec §6.5, §16.9/§16.15):
/// the module must require Administrator, be guild-only, and carry a default-permission
/// gate. These attributes are what Discord.Net enforces server-side.
/// </summary>
public class AdminCommandGuardTests
{
    private static readonly Type Module = typeof(NftAdminCommands);

    [Fact]
    public void AdminModule_RequiresAdministratorPermission()
    {
        var attributes = Module
            .GetCustomAttributes(typeof(RequireUserPermissionAttribute), inherit: true)
            .Cast<RequireUserPermissionAttribute>();

        Assert.Contains(attributes, a => a.GuildPermission == GuildPermission.Administrator);
    }

    [Fact]
    public void AdminModule_IsGuildOnly()
    {
        var attributes = Module
            .GetCustomAttributes(typeof(RequireContextAttribute), inherit: true)
            .Cast<RequireContextAttribute>();

        Assert.Contains(attributes, a => a.Contexts.HasFlag(ContextType.Guild));
    }

    [Fact]
    public void AdminModule_HasDefaultMemberPermissionGate()
    {
        Assert.NotEmpty(Module.GetCustomAttributes(typeof(DefaultMemberPermissionsAttribute), inherit: true));
    }
}
