using System.Text.Json.Serialization;

namespace Felina.Contracts;

[JsonConverter(typeof(JsonStringEnumConverter<StorageAggregateScope>))]
public enum StorageAggregateScope
{
    Client = 1,
    Module = 2,
    Workspace = 3
}
