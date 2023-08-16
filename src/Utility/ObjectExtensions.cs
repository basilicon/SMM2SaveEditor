using System;
using System.Collections.Generic;
using System.Reflection;

namespace SMM2Level.Utility;

public static class ObjectExtensions
{
    public static T ToObject<T>(this IDictionary<string, object> source)
        where T : class, new()
    {
        var someObject = new T();
        var someObjectType = typeof(T);

        foreach (var item in source)
        {
            someObjectType.GetProperty(item.Key).SetValue(someObject, item.Value, null);
        }

        return someObject;
    }

    public static void ToObject<T>(this T obj, IDictionary<string, object> source)
        where T : class
    {
        Type type = typeof(T);

        foreach (var item in source)
        {
            type.GetProperty(item.Key).SetValue(obj, item.Value, null);
        }
    }

    public static IDictionary<string, object> AsDictionary<T>(this T source, BindingFlags bindingAttr = BindingFlags.Public)
        where T : class
    {
        Type myObjectType = typeof(T);
        IDictionary<string, object> dict = new Dictionary<string, object>();
        PropertyInfo[] properties = myObjectType.GetProperties();
        foreach (var info in properties)
        {
            dict.Add(info.Name, info.GetValue(source));
        }
        return dict;

    }
}