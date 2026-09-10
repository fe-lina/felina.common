using System.Text.Json.Serialization;

namespace Felina.Contracts;

[JsonConverter(typeof(JsonStringEnumConverter<StorageStatsNodeType>))]
public enum StorageStatsNodeType
{
    Workspace = 1,
    Directory = 2
}
