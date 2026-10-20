using System.Collections.Concurrent;
using Forgotten.Bot.Configuration;
using Microsoft.Extensions.Options;

namespace Forgotten.Bot.Services;

/// <summary>
/// Lightweight per-user, per-command fixed cooldown to rate-limit slash commands
/// (spec §16.10). In-memory and best-effort — sufficient to blunt spam without a
/// distributed store.
/// </summary>
public sealed class CommandRateLimiter
{
    private readonly ConcurrentDictionary<string, long> _lastInvocation = new();
    private readonly long _cooldownMs;

    public CommandRateLimiter(IOptions<PresentationOptions> options)
        => _cooldownMs = Math.Max(0, options.Value.CommandCooldownSeconds) * 1000L;

    public bool TryAcquire(ulong userId, string command, out TimeSpan retryAfter)
    {
        retryAfter = TimeSpan.Zero;
        if (_cooldownMs <= 0)
        {
            return true;
        }

        var key = $"{userId}:{command}";
        var now = Environment.TickCount64;

        while (true)
        {
            if (_lastInvocation.TryGetValue(key, out var last))
            {
                var elapsed = now - last;
                if (elapsed < _cooldownMs)
                {
                    retryAfter = TimeSpan.FromMilliseconds(_cooldownMs - elapsed);
                    return false;
                }

                if (_lastInvocation.TryUpdate(key, now, last))
                {
                    return true;
                }
            }
            else if (_lastInvocation.TryAdd(key, now))
            {
                return true;
            }
        }
    }
}
