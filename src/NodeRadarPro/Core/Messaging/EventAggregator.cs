using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace NodeRadarPro.Core.Messaging;

public class EventAggregator
{
    private static readonly Lazy<EventAggregator> _instance = new(() => new EventAggregator());
    public static EventAggregator Instance => _instance.Value;

    private readonly ConcurrentDictionary<Type, List<WeakSubscription>> _subscribers = new();

    public void Subscribe<TMessage>(Action<TMessage> action)
    {
        var sub = new WeakSubscription(action);
        _subscribers.AddOrUpdate(typeof(TMessage),
            _ => new List<WeakSubscription> { sub },
            (_, list) =>
            {
                lock (list) { list.Add(sub); }
                return list;
            });
    }

    public void Unsubscribe<TMessage>(Action<TMessage> action)
    {
        if (_subscribers.TryGetValue(typeof(TMessage), out var list))
        {
            lock (list)
            {
                list.RemoveAll(sub =>
                {
                    var del = sub.GetDelegate();
                    return del == null || (del.Target == action.Target && del.Method == action.Method);
                });
            }
        }
    }

    public void Publish<TMessage>(TMessage message)
    {
        if (_subscribers.TryGetValue(typeof(TMessage), out var list))
        {
            List<WeakSubscription> subscribersToInvoke;
            lock (list)
            {
                list.RemoveAll(sub => !sub.IsAlive);
                subscribersToInvoke = new List<WeakSubscription>(list);
            }

            foreach (var sub in subscribersToInvoke)
            {
                sub.TryInvoke(message);
            }
        }
    }
}

public class WeakSubscription
{
    private readonly WeakReference? _targetRef;
    private readonly System.Reflection.MethodInfo _method;
    private readonly Type _delegateType;
    private readonly bool _isStatic;

    public WeakSubscription(Delegate d)
    {
        _targetRef = d.Target != null ? new WeakReference(d.Target) : null;
        _method = d.Method;
        _delegateType = d.GetType();
        _isStatic = d.Method.IsStatic;
    }

    public bool IsAlive => _isStatic || (_targetRef != null && _targetRef.IsAlive && _targetRef.Target != null);

    public Delegate? GetDelegate()
    {
        var target = _targetRef?.Target;
        if (target == null && !_isStatic) return null;
        return Delegate.CreateDelegate(_delegateType, target, _method);
    }

    public bool TryInvoke<TMessage>(TMessage message)
    {
        var target = _targetRef?.Target;
        if (target == null && !_isStatic) return false;
        try
        {
            _method.Invoke(target, new object?[] { message });
            return true;
        }
        catch
        {
            return false;
        }
    }
}
