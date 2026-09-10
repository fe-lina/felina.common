namespace Felina.Client;

/// <summary>
/// Resolves a Felina transport by its application configuration alias.
/// The alias is local to the consuming application and is not Felina's c value.
/// </summary>
public interface IStorageClientFactory
{
    IStorageClient GetRequiredClient(string name);
}
