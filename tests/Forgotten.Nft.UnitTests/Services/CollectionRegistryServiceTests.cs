using Forgotten.Nft.Application.Services;

namespace Forgotten.Nft.UnitTests.Services;

public class CollectionRegistryServiceTests
{
    private const string Collection = "So11111111111111111111111111111111111111112";
    private const string Collection2 = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v";

    private static CollectionRegistryService Create() => new(new FakeCollectionRepository(), new NullDomainEventPublisher());

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

    [Fact]
    public async Task FirstApprovedCollection_BecomesDefault()
    {
        var registry = Create();

        var first = await registry.ApproveAsync(1UL, Collection, "FORGOTTEN", 7UL, default);

        Assert.True(first.IsDefault);
        var def = await registry.GetDefaultAsync(1UL, default);
        Assert.NotNull(def);
        Assert.Equal(Collection, def!.CollectionAddress);
    }

    [Fact]
    public async Task SecondApprovedCollection_IsNotDefault()
    {
        var registry = Create();
        await registry.ApproveAsync(1UL, Collection, "FORGOTTEN", 7UL, default);

        var second = await registry.ApproveAsync(1UL, Collection2, "Other", 7UL, default);

        Assert.False(second.IsDefault);
        var def = await registry.GetDefaultAsync(1UL, default);
        Assert.Equal(Collection, def!.CollectionAddress);
    }

    [Fact]
    public async Task SetDefault_SwitchesTheDefault()
    {
        var registry = Create();
        await registry.ApproveAsync(1UL, Collection, "FORGOTTEN", 7UL, default);
        await registry.ApproveAsync(1UL, Collection2, "Other", 7UL, default);

        var ok = await registry.SetDefaultAsync(1UL, Collection2, default);

        Assert.True(ok);
        var def = await registry.GetDefaultAsync(1UL, default);
        Assert.Equal(Collection2, def!.CollectionAddress);
    }

    [Fact]
    public async Task SetDefault_UnapprovedCollection_ReturnsFalse()
    {
        var registry = Create();

        Assert.False(await registry.SetDefaultAsync(1UL, Collection, default));
    }

    [Fact]
    public async Task GetDefault_FallsBackToSoleApproved_WhenNoneFlagged()
    {
        // A guild approved before the IsDefault flag existed: one approved row, none flagged.
        var repo = new FakeCollectionRepository();
        await repo.UpsertAsync(new Forgotten.Nft.Domain.Entities.NftCollection
        {
            GuildId = 1UL,
            Blockchain = "solana",
            CollectionAddress = Collection,
            IsApproved = true,
            IsDefault = false,
        }, default);
        var registry = new CollectionRegistryService(repo, new NullDomainEventPublisher());

        var def = await registry.GetDefaultAsync(1UL, default);

        Assert.NotNull(def);
        Assert.Equal(Collection, def!.CollectionAddress);
    }
}
