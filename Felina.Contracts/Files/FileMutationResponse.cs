namespace Felina.Contracts;

public sealed class FileMutationResponse
{
    public bool Deleted { get; set; }
    public bool Restored { get; set; }
    public bool Force { get; set; }
}
