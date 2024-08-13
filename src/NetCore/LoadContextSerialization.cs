#if NET || NETCOREAPP
#pragma warning disable SYSLIB0011 // Type or member is obsolete
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Unicorn.TestAdapter
{
    internal class LoadContextSerialization
    {
        internal static byte[] Serialize(object data)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                new BinaryFormatter().Serialize(ms, data);
                return ms.ToArray();
            }
        }

        internal static T Deserialize<T>(byte[] bytes)
        {
            using (MemoryStream memStream = new MemoryStream())
            {
                memStream.Write(bytes, 0, bytes.Length);
                memStream.Seek(0, SeekOrigin.Begin);
                return (T)new BinaryFormatter().Deserialize(memStream);
            }
        }
    }
}
#pragma warning restore SYSLIB0011 // Type or member is obsolete
#endif