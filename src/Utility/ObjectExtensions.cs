﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace SMM2SaveEditor.Utility;

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
        return source.AsDictionary(myObjectType, bindingAttr);

    }

    public static IDictionary<string, object> AsDictionary(this object source, Type type, BindingFlags bindingAttr = BindingFlags.Public | BindingFlags.Instance)
    {
        IDictionary<string, object> dict = new Dictionary<string, object>();
        FieldInfo[] fields = type.GetFields(bindingAttr);

        foreach (var info in fields)
        {
            object? value = info.GetValue(source);

            if (value != null) dict.Add(info.Name, value);
        }

        return dict;
    }

    public static string EqualSpacingDefinition(int count)
    {
        string defs = "*";
        for (int i = 1; i < count; i++) defs += ",*";
        return defs;
    }
}