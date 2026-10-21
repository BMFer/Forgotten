using Forgotten.Nft.Domain.Entities;

namespace Forgotten.Nft.Application.Abstractions;

/// <summary>Persistence for interactive gallery sessions (spec §9 "NftGallerySessions").</summary>
public interface INftGallerySessionRepository
{
    Task AddAsync(NftGallerySession session, CancellationToken cancellationToken);

    Task<NftGallerySession?> GetAsync(int id, CancellationToken cancellationToken);

    Task UpdateAsync(NftGallerySession session, CancellationToken cancellationToken);
}
