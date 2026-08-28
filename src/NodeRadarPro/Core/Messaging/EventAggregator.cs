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
            List<Action<TMessage>> actionsToInvoke = new();
            lock (list)
            {
                list.RemoveAll(sub => !sub.IsAlive);
                foreach (var sub in list)
                {
                    if (sub.GetDelegate() is Action<TMessage> typedAction)
                    {
                        actionsToInvoke.Add(typedAction);
                    }
                }
            }

            foreach (var action in actionsToInvoke)
            {
                action(message);
            }
        }
    }
}

public class WeakSubscription
{
    private readonly WeakReference _targetRef;
    private readonly System.Reflection.MethodInfo _method;
    private readonly Type _delegateType;

    public WeakSubscription(Delegate d)
    {
        _targetRef = new WeakReference(d.Target);
        _method = d.Method;
        _delegateType = d.GetType();
    }

    public bool IsAlive => _targetRef.Target != null || _method.IsStatic;

    public Delegate? GetDelegate()
    {
        var target = _targetRef.Target;
        if (target == null && !_method.IsStatic) return null;
        return Delegate.CreateDelegate(_delegateType, target, _method);
    }
}
