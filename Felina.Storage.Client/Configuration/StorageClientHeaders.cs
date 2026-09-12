namespace Felina.Client;

public static class StorageClientHeaders
{
    public const string Client = "X-Felina-Client";
    public const string Key = "X-Felina-Storage-Key";
    public const string AdminClient = "X-Felina-Admin-Client";
    public const string AdminKey = "X-Felina-Admin-Key";
    public const string MaxSizeMegabytes = "X-Felina-Max-Size-MB";
    public const string LegacyMaxSizeMegabytes = "X-razor-max-size";
}
