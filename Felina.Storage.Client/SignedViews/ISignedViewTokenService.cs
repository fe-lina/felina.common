namespace Felina.Client;

public interface ISignedViewTokenService
{
    string CreateToken(SignedViewCreateRequest request);

    bool TryValidate(
        string token,
        out SignedViewClaims? claims,
        out string? error);
}
