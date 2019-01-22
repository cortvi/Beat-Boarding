using System;
using System.Globalization;

public static class Extension
{
	#if UNITY_EDITOR
	public static T ObjectField<T> (T o, string label, bool acceptSceneObjects = false) where T : UnityEngine.Object
	{
		var result = UnityEditor.EditorGUILayout.ObjectField (label, o, typeof(T), acceptSceneObjects);
		return (T) result;
	}
	#endif

	public static bool HasFlag<T> (this T e, T flag) where T : struct, IConvertible
	{
		var value = e.ToInt32 (CultureInfo.InvariantCulture);
		var target = flag.ToInt32 (CultureInfo.InvariantCulture);

		return ((value & target) == target);
	}
}
