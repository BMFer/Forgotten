using System.Text.Json;
using System.Text.Json.Serialization;

namespace Forgotten.Nft.Infrastructure.Blockchain.Das;

// Deserialization-only DTOs for the Metaplex DAS `getAsset` JSON-RPC response.
// Everything is nullable/tolerant: provider payloads are untrusted and fields
// are frequently absent (spec §5.4, §8.1).

internal sealed class DasRpcResponse<T>
{
    [JsonPropertyName("result")] public T? Result { get; set; }
    [JsonPropertyName("error")] public DasError? Error { get; set; }
}

internal sealed class DasError
{
    [JsonPropertyName("code")] public int Code { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
}

internal sealed class DasAsset
{
    [JsonPropertyName("interface")] public string? Interface { get; set; }
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("content")] public DasContent? Content { get; set; }
    [JsonPropertyName("compression")] public DasCompression? Compression { get; set; }
    [JsonPropertyName("grouping")] public List<DasGroup>? Grouping { get; set; }
    [JsonPropertyName("creators")] public List<DasCreator>? Creators { get; set; }
    [JsonPropertyName("ownership")] public DasOwnership? Ownership { get; set; }
    [JsonPropertyName("token_info")] public DasTokenInfo? TokenInfo { get; set; }
    [JsonPropertyName("mutable")] public bool? Mutable { get; set; }
    [JsonPropertyName("burnt")] public bool? Burnt { get; set; }
}

internal sealed class DasContent
{
    [JsonPropertyName("json_uri")] public string? JsonUri { get; set; }
    [JsonPropertyName("metadata")] public DasMetadata? Metadata { get; set; }
    [JsonPropertyName("links")] public DasLinks? Links { get; set; }
}

internal sealed class DasMetadata
{
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("symbol")] public string? Symbol { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("token_standard")] public string? TokenStandard { get; set; }
    [JsonPropertyName("attributes")] public List<DasAttribute>? Attributes { get; set; }
}

internal sealed class DasAttribute
{
    [JsonPropertyName("trait_type")] public string? TraitType { get; set; }
    [JsonPropertyName("value")] public JsonElement Value { get; set; }
    [JsonPropertyName("display_type")] public string? DisplayType { get; set; }

    public string ValueAsString() => Value.ValueKind switch
    {
        JsonValueKind.String => Value.GetString() ?? string.Empty,
        JsonValueKind.Number => Value.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Null or JsonValueKind.Undefined => string.Empty,
        _ => Value.GetRawText(),
    };
}

internal sealed class DasLinks
{
    [JsonPropertyName("image")] public string? Image { get; set; }
    [JsonPropertyName("animation_url")] public string? AnimationUrl { get; set; }
    [JsonPropertyName("external_url")] public string? ExternalUrl { get; set; }
}

internal sealed class DasCompression
{
    [JsonPropertyName("compressed")] public bool Compressed { get; set; }
}

internal sealed class DasGroup
{
    [JsonPropertyName("group_key")] public string? GroupKey { get; set; }
    [JsonPropertyName("group_value")] public string? GroupValue { get; set; }
    [JsonPropertyName("verified")] public bool? Verified { get; set; }
}

internal sealed class DasCreator
{
    [JsonPropertyName("address")] public string? Address { get; set; }
    [JsonPropertyName("share")] public int Share { get; set; }
    [JsonPropertyName("verified")] public bool Verified { get; set; }
}

internal sealed class DasOwnership
{
    [JsonPropertyName("owner")] public string? Owner { get; set; }
    [JsonPropertyName("ownership_model")] public string? OwnershipModel { get; set; }
}

internal sealed class DasTokenInfo
{
    [JsonPropertyName("supply")] public long? Supply { get; set; }
    [JsonPropertyName("decimals")] public int? Decimals { get; set; }
    [JsonPropertyName("token_program")] public string? TokenProgram { get; set; }
}
