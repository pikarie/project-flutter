using System;
using System.Collections.Generic;

namespace ProjectFlutter;

public static class EventBus
{
	private static readonly Dictionary<Type, List<Delegate>> _subscribers = new();

	public static void Subscribe<T>(Action<T> callback)
	{
		var type = typeof(T);
		if (!_subscribers.TryGetValue(type, out var list))
		{
			list = new List<Delegate>();
			_subscribers[type] = list;
		}
		list.Add(callback);
	}

	public static void Unsubscribe<T>(Action<T> callback)
	{
		if (_subscribers.TryGetValue(typeof(T), out var list))
			list.Remove(callback);
	}

	public static void Publish<T>(T evt)
	{
		if (!_subscribers.TryGetValue(typeof(T), out var list)) return;
		// ToArray() snapshot prevents issues if a callback modifies the list
		foreach (var cb in list.ToArray())
			((Action<T>)cb)?.Invoke(evt);
	}
}
