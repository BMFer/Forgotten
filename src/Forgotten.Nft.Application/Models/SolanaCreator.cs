namespace Forgotten.Nft.Application.Models;

/// <summary>A creator record as reported on-chain, including its verification flag (spec §4.1).</summary>
public sealed record SolanaCreator(string Address, int Share, bool Verified);
