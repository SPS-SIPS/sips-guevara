using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace SPSS.App.Features.Auth;

public sealed class LoginAttemptLimiter
{
    private sealed class Attempt { public int Count; public DateTimeOffset Until; }
    private readonly ConcurrentDictionary<string, Attempt> attempts = new();
    private readonly object gate = new();

    public bool IsBlocked(string username) => !TryTake(username);

    private bool TryTake(string username)
    {
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(username.Trim().ToLowerInvariant())));
        lock (gate)
        {
            var now = DateTimeOffset.UtcNow;
            if (attempts.Count >= 10000)
            {
                foreach (var entry in attempts.Where(x => x.Value.Until <= now).ToArray()) attempts.TryRemove(entry.Key, out _);
                if (attempts.Count >= 10000 && !attempts.ContainsKey(key)) return false;
            }
            var attempt = attempts.GetOrAdd(key, _ => new Attempt { Until = now.AddMinutes(15) });
            if (attempt.Until <= now) { attempt.Count = 0; attempt.Until = now.AddMinutes(15); }
            return ++attempt.Count <= 5;
        }
    }
}
