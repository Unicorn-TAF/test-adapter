#if NET || NETCOREAPP
using System.Text.Json;

namespace Unicorn.TestAdapter.NetCore
{
    internal class LoadContextSerialization
    {
        internal static string Serialize(object data) => 
            JsonSerializer.Serialize(data);

        internal static T Deserialize<T>(string data) =>
            JsonSerializer.Deserialize<T>(data);
    }
}
#endif