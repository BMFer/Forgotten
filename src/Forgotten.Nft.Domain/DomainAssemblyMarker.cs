namespace Forgotten.Nft.Domain;

/// <summary>
/// Marker type used to locate the Domain assembly (e.g. from architecture tests).
/// The Domain layer must never reference Discord.Net or any infrastructure concern
/// (spec §3: "Domain entities must not depend on Discord.Net.").
/// Phase 1 populates this assembly with entities, enums, value objects, and events.
/// </summary>
public sealed class DomainAssemblyMarker
{
}
