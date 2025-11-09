#if NET7_0
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace African_Beauty_Trading.CoreApp
{
    public static class SessionExtensions
    {
        public static void SetObjectAsJson(this ISession session, string key, object value)
        {
            session.SetString(key, JsonSerializer.Serialize(value));
        }

        public static T GetObjectFromJson<T>(this ISession session, string key)
        {
            var json = session.GetString(key);
            if (json == null) return default!;
            return JsonSerializer.Deserialize<T>(json)!;
        }
    }
}
#endif
