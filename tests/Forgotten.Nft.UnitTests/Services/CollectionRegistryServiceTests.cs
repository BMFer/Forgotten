using Forgotten.Nft.Application.Services;

namespace Forgotten.Nft.UnitTests.Services;

public class CollectionRegistryServiceTests
{
    private const string Collection = "So11111111111111111111111111111111111111112";

    private static CollectionRegistryService Create() => new(new FakeCollectionRepository());

    [Fact]
    public async Task Approve_ThenIsApproved_True()
    {
        var registry = Create();

        var entry = await registry.ApproveAsync(1UL, Collection, "DMC", 7UL, default);

        Assert.True(entry.IsApproved);
        Assert.Equal(7UL, entry.ApprovedByDiscordUserId);
        Assert.NotNull(entry.ApprovedAt);
        Assert.True(await registry.IsApprovedAsync(1UL, Collection, default));
    }

    [Fact]
    public async Task Registry_IsGuildScoped()
    {
        var registry = Create();
        await registry.ApproveAsync(1UL, Collection, "DMC", 7UL, default);

        Assert.True(await registry.IsApprovedAsync(1UL, Collection, default));
        Assert.False(await registry.IsApprovedAsync(2UL, Collection, default));
    }

    [Fact]
    public async Task Remove_DeletesEntry()
    {
        var registry = Create();
        await registry.ApproveAsync(1UL, Collection, "DMC", 7UL, default);

        var removed = await registry.RemoveAsync(1UL, Collection, default);

        Assert.True(removed);
        Assert.False(await registry.IsApprovedAsync(1UL, Collection, default));
    }

    [Fact]
    public async Task Remove_Missing_ReturnsFalse()
    {
        var registry = Create();

        Assert.False(await registry.RemoveAsync(1UL, Collection, default));
    }

    [Fact]
    public async Task InvalidAddress_Throws()
    {
        var registry = Create();

        await Assert.ThrowsAsync<ArgumentException>(
            () => registry.ApproveAsync(1UL, "not-valid", null, 7UL, default));
    }
}
