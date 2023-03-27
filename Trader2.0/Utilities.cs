using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Trader20
{
    public static class Utilities
    {
	    internal enum ConnectionState
	    {
		    Server,
		    Client,
		    Local,
		    Unknown
	    }
	    internal static ConnectionState GetConnectionState()
	    {
			if (ZNet.instance == null) return ConnectionState.Local;
		    if (ZNet.instance.IsServer() && ZNet.instance.IsDedicated()) //server
		    {
			    return ConnectionState.Server;
		    }

		    if (!ZNet.instance.IsServer() && !ZNet.instance.IsDedicated()) //client
		    {
			    return ConnectionState.Client;
		    }

		    if (ZNet.instance.IsServer() && !ZNet.instance.IsDedicated()) //Local
		    {
			    return ConnectionState.Local;
		    }

		    return ConnectionState.Unknown;
	    }
        internal static AssetBundle? LoadAssetBundle(string bundleName)
        {
            var resource = typeof(Trader20).Assembly.GetManifestResourceNames().Single
                (s => s.EndsWith(bundleName));
            using var stream = typeof(Trader20).Assembly.GetManifestResourceStream(resource);
            return AssetBundle.LoadFromStream(stream);
        }

        internal static void LoadAssets(AssetBundle? bundle, ZNetScene zNetScene)
        {
            var tmp = bundle?.LoadAllAssets();
            if (zNetScene.m_prefabs.Count <= 0) return;
            if (tmp == null) return;
            foreach (var o in tmp)
            {
                var obj = (GameObject)o;
                zNetScene.m_prefabs.Add(obj);
                var hashcode = obj.GetHashCode();
                zNetScene.m_namedPrefabs.Add(hashcode, obj);
            }
        }
        public static int seed = 0;
        internal static T? CopyChildrenComponents<T, TU>(this Component comp, TU other) where T : Component
		{
			IEnumerable<FieldInfo> finfos = comp.GetType().GetFields(BindingFlags);
			foreach (var finfo in finfos)
			{
				finfo.SetValue(comp, finfo.GetValue(other));
			}
			return comp as T;
		}
		private const BindingFlags BindingFlags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.GetField;
		private static T? GetCopyOf<T>(this Component comp, T other) where T : Component
		{
			Type type = comp.GetType();
			if (type != other.GetType()) return null; // type mis-match

			List<Type> derivedTypes = new List<Type>();
			Type derived = type.BaseType;
			while (derived != null)
			{
				if (derived == typeof(MonoBehaviour))
				{
					break;
				}
				derivedTypes.Add(derived);
				derived = derived.BaseType;
			}

			IEnumerable<PropertyInfo> pinfos = type.GetProperties(BindingFlags);

			foreach (Type derivedType in derivedTypes)
			{
				pinfos = pinfos.Concat(derivedType.GetProperties(BindingFlags));
			}

			pinfos = from property in pinfos
					 where !(type == typeof(Rigidbody) && property.Name == "inertiaTensor") // Special case for Rigidbodies inertiaTensor which isn't catched for some reason.
					 where !property.CustomAttributes.Any(attribute => attribute.AttributeType == typeof(ObsoleteAttribute))
					 select property;
			foreach (var pinfo in pinfos)
			{
				if (pinfo.CanWrite)
				{
					if (pinfos.Any(e => e.Name == $"shared{char.ToUpper(pinfo.Name[0])}{pinfo.Name.Substring(1)}"))
					{
						continue;
					}
					try
					{
						pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
					}
					catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
				}
			}

			IEnumerable<FieldInfo> finfos = type.GetFields(BindingFlags);

			foreach (var finfo in finfos)
			{

				foreach (Type derivedType in derivedTypes)
				{
					if (finfos.Any(e => e.Name == $"shared{char.ToUpper(finfo.Name[0])}{finfo.Name.Substring(1)}"))
					{
						continue;
					}
					finfos = finfos.Concat(derivedType.GetFields(BindingFlags));
				}
			}

			foreach (var finfo in finfos)
			{
				finfo.SetValue(comp, finfo.GetValue(other));
			}

			finfos = from field in finfos
					 where field.CustomAttributes.Any(attribute => attribute.AttributeType == typeof(ObsoleteAttribute))
					 select field;
			foreach (var finfo in finfos)
			{
				finfo.SetValue(comp, finfo.GetValue(other));
			}

			return comp as T;
		}
		public static T? AddComponentcc<T>(this GameObject go, T toAdd) where T : Component
		{
			return go.AddComponent(toAdd.GetType()).GetCopyOf(toAdd) as T;
		}
		
		public static Component CopyComponent(Component original, GameObject destination)
		{
			System.Type type = original.GetType();
			Component copy = destination.AddComponent(type);
			// Copied fields can be restricted with BindingFlags
			System.Reflection.FieldInfo[] fields = type.GetFields(); 
			foreach (System.Reflection.FieldInfo field in fields)
			{
				field.SetValue(copy, field.GetValue(original));
			}
			return copy;
		}
		
		public static  T? CopyComponent<T>(T original, GameObject destination) where T : Component
		{
			System.Type type = original.GetType();
			Component copy = destination.AddComponent(type);
			System.Reflection.FieldInfo[] fields = type.GetFields();
			foreach (System.Reflection.FieldInfo field in fields)
			{
				field.SetValue(copy, field.GetValue(original));
			}
			return copy as T;
		}
		
		public static bool ChangeKey<TKey, TValue>(this IDictionary<TKey, TValue> dict, 
			TKey oldKey, TKey newKey)
		{
			TValue value;
			if (!dict.TryGetValue(oldKey, out value))
				return false;

			dict.Remove(oldKey);
			dict[newKey] = value;  // or dict.Add(newKey, value) depending on ur comfort
			return true;
		}
    }
}