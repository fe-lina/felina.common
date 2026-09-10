using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Felina.Client;

internal sealed class SignedViewTokenService(
    IOptions<SignedViewOptions> options) : ISignedViewTokenService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly SignedViewOptions _options = options.Value;

    public string CreateToken(SignedViewCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(_options.SigningKey))
            throw new InvalidOperationException("Felina Storage signed-view SigningKey is not configured.");

        if (string.IsNullOrWhiteSpace(request.Uid) && string.IsNullOrWhiteSpace(request.Ruid))
            throw new ArgumentException("A uid or ruid is required.", nameof(request));

        var now = DateTimeOffset.UtcNow;
        var expiry = request.ExpiresIn ??
            TimeSpan.FromMinutes(Math.Clamp(_options.DefaultExpiryMinutes, 1, 1440));

        var claims = new SignedViewClaims
        {
            Uid = request.Uid,
            Ruid = request.Ruid,
            Parent = request.Parent,
            Kind = request.Kind,
            Created = now,
            Expires = now.Add(expiry),
            Query = request.Query
        };

        var payload = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(claims, JsonOptions));
        var signature = Sign(payload);
        return $"{payload}.{signature}";
    }

    public bool TryValidate(
        string token,
        out SignedViewClaims? claims,
        out string? error)
    {
        claims = null;
        error = null;

        if (string.IsNullOrWhiteSpace(_options.SigningKey))
        {
            error = "Signed-view SigningKey is not configured.";
            return false;
        }

        var parts = token?.Split('.', 2);
        if (parts is not { Length: 2 })
        {
            error = "Signed-view token is malformed.";
            return false;
        }

        var expected = Sign(parts[0]);
        if (!FixedTimeEquals(expected, parts[1]))
        {
            error = "Signed-view token signature is invalid.";
            return false;
        }

        try
        {
            claims = JsonSerializer.Deserialize<SignedViewClaims>(
                Base64UrlDecode(parts[0]),
                JsonOptions);
        }
        catch (Exception ex) when (ex is JsonException or FormatException)
        {
            error = "Signed-view token payload is invalid.";
            return false;
        }

        if (claims is null || (string.IsNullOrWhiteSpace(claims.Uid) && string.IsNullOrWhiteSpace(claims.Ruid)))
        {
            error = "Signed-view token does not contain a uid or ruid.";
            return false;
        }

        if (DateTimeOffset.UtcNow > claims.Expires)
        {
            error = "Signed-view token has expired.";
            return false;
        }

        return true;
    }

    private string Sign(string payload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.SigningKey));
        return Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
    }

    private static bool FixedTimeEquals(string expected, string actual)
    {
        var expectedBytes = Encoding.ASCII.GetBytes(expected);
        var actualBytes = Encoding.ASCII.GetBytes(actual);
        return expectedBytes.Length == actualBytes.Length &&
            CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value
            .Replace('-', '+')
            .Replace('_', '/');

        padded += (padded.Length % 4) switch
        {
            2 => "==",
            3 => "=",
            0 => string.Empty,
            _ => throw new FormatException("Invalid base64url length.")
        };

        return Convert.FromBase64String(padded);
    }
}
