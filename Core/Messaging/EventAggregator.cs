using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace NodeRadarPro.Core.Messaging;

public class EventAggregator
{
    private static readonly Lazy<EventAggregator> _instance = new(() => new EventAggregator());
    public static EventAggregator Instance => _instance.Value;

    private readonly ConcurrentDictionary<Type, List<Delegate>> _subscribers = new();

    public void Subscribe<TMessage>(Action<TMessage> action)
    {
        _subscribers.AddOrUpdate(typeof(TMessage),
            _ => new List<Delegate> { action },
            (_, list) => {
                lock (list) { list.Add(action); }
                return list;
            });
    }

    public void Publish<TMessage>(TMessage message)
    {
        if (_subscribers.TryGetValue(typeof(TMessage), out var list))
        {
            Delegate[] delegates;
            lock (list) { delegates = list.ToArray(); }
            foreach (var action in delegates)
            {
                if (action is Action<TMessage> typedAction)
                {
                    typedAction(message);
                }
            }
        }
    }
}
