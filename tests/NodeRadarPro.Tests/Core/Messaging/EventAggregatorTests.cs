using NodeRadarPro.Core.Messaging;

namespace NodeRadarPro.Tests.Messaging;

public class EventAggregatorTests
{
    private class TestMessageA
    {
        public string Value { get; set; } = string.Empty;
    }

    private class TestMessageB
    {
        public string Dummy { get; set; } = string.Empty;
    }

    [Fact]
    public void Instance_ReturnsSingleton()
    {
        var instance1 = EventAggregator.Instance;
        var instance2 = EventAggregator.Instance;
        Assert.Same(instance1, instance2);
    }

    [Fact]
    public void Publish_TriggersSubscriberForMatchingMessageType()
    {
        var aggregator = new EventAggregator();
        int callCount = 0;
        string receivedValue = string.Empty;

        aggregator.Subscribe<TestMessageA>(msg =>
        {
            callCount++;
            receivedValue = msg.Value;
        });

        aggregator.Publish(new TestMessageA { Value = "Hello" });

        Assert.Equal(1, callCount);
        Assert.Equal("Hello", receivedValue);
    }

    [Fact]
    public void Publish_DoesNotTriggerSubscriberForDifferentMessageType()
    {
        var aggregator = new EventAggregator();
        int callCountA = 0;

        aggregator.Subscribe<TestMessageA>(_ => callCountA++);

        aggregator.Publish(new TestMessageB());

        Assert.Equal(0, callCountA);
    }

    [Fact]
    public void Publish_TriggersMultipleSubscribersForSameMessageType()
    {
        var aggregator = new EventAggregator();
        int callCount1 = 0;
        int callCount2 = 0;

        aggregator.Subscribe<TestMessageA>(_ => callCount1++);
        aggregator.Subscribe<TestMessageA>(_ => callCount2++);

        aggregator.Publish(new TestMessageA());

        Assert.Equal(1, callCount1);
        Assert.Equal(1, callCount2);
    }

    private class SampleSubscriber
    {
        public int InvocationCount { get; private set; }

        public void HandleMessage(TestMessageA msg)
        {
            InvocationCount++;
        }

        public void ThrowingHandler(TestMessageA msg)
        {
            throw new InvalidOperationException("Test exception");
        }
    }

    private static int _staticHandlerCount;
    private static void StaticHandler(TestMessageA msg)
    {
        _staticHandlerCount++;
    }

    [Fact]
    public void WeakSubscription_GetDelegate_InstanceMethod_ReturnsDelegateWhenTargetIsAlive()
    {
        var subscriber = new SampleSubscriber();
        Action<TestMessageA> action = subscriber.HandleMessage;
        var sub = new WeakSubscription(action);

        Assert.True(sub.IsAlive);
        var del = sub.GetDelegate();

        Assert.NotNull(del);
        Assert.Equal(action.Method, del.Method);
        Assert.Same(subscriber, del.Target);

        var actionDel = Assert.IsType<Action<TestMessageA>>(del);
        actionDel(new TestMessageA());
        Assert.Equal(1, subscriber.InvocationCount);
    }

    [Fact]
    public void WeakSubscription_GetDelegate_StaticMethod_ReturnsDelegateWhenTargetIsNull()
    {
        Action<TestMessageA> action = StaticHandler;
        var sub = new WeakSubscription(action);

        Assert.True(sub.IsAlive);
        var del = sub.GetDelegate();

        Assert.NotNull(del);
        Assert.Null(del.Target);
        Assert.Equal(action.Method, del.Method);

        _staticHandlerCount = 0;
        var actionDel = Assert.IsType<Action<TestMessageA>>(del);
        actionDel(new TestMessageA());
        Assert.Equal(1, _staticHandlerCount);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static WeakSubscription CreateSubscriptionWithDeadTarget()
    {
        var subscriber = new SampleSubscriber();
        Action<TestMessageA> action = subscriber.HandleMessage;
        return new WeakSubscription(action);
    }

#pragma warning disable S1215
    private static void ForceGarbageCollection()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
#pragma warning restore S1215

    [Fact]
    public void WeakSubscription_GetDelegate_InstanceMethod_ReturnsNullWhenTargetIsCollected()
    {
        var sub = CreateSubscriptionWithDeadTarget();

        ForceGarbageCollection();

        Assert.False(sub.IsAlive);
        var del = sub.GetDelegate();
        Assert.Null(del);
    }

    [Fact]
    public void WeakSubscription_TryInvoke_ReturnsFalseWhenTargetIsCollected()
    {
        var sub = CreateSubscriptionWithDeadTarget();

        ForceGarbageCollection();

        bool invoked = sub.TryInvoke(new TestMessageA());
        Assert.False(invoked);
    }

    [Fact]
    public void WeakSubscription_TryInvoke_ReturnsFalseWhenDelegateThrowsException()
    {
        var subscriber = new SampleSubscriber();
        Action<TestMessageA> action = subscriber.ThrowingHandler;
        var sub = new WeakSubscription(action);

        bool invoked = sub.TryInvoke(new TestMessageA());
        Assert.False(invoked);
    }

    [Fact]
    public void EventAggregator_Unsubscribe_RemovesSubscriptionForMatchingTargetAndMethod()
    {
        var aggregator = new EventAggregator();
        var subscriber = new SampleSubscriber();
        Action<TestMessageA> action = subscriber.HandleMessage;

        aggregator.Subscribe(action);
        aggregator.Publish(new TestMessageA());
        Assert.Equal(1, subscriber.InvocationCount);

        aggregator.Unsubscribe(action);
        aggregator.Publish(new TestMessageA());
        Assert.Equal(1, subscriber.InvocationCount);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static void RegisterDeadSubscriber(EventAggregator aggregator)
    {
        var tempSubscriber = new SampleSubscriber();
        aggregator.Subscribe<TestMessageA>(tempSubscriber.HandleMessage);
    }

    [Fact]
    public void EventAggregator_Unsubscribe_CleansUpDeadSubscriptions()
    {
        var aggregator = new EventAggregator();
        int activeHandlerCallCount = 0;

        Action<TestMessageA> actionToUnsubscribe = _ => activeHandlerCallCount++;

        RegisterDeadSubscriber(aggregator);
        aggregator.Subscribe(actionToUnsubscribe);

        ForceGarbageCollection();

        aggregator.Unsubscribe(actionToUnsubscribe);
        aggregator.Publish(new TestMessageA());

        Assert.Equal(0, activeHandlerCallCount);
    }

    [Fact]
    public void EventAggregator_Publish_CleansUpDeadSubscriptionsAndInvokesStaticHandlers()
    {
        var aggregator = new EventAggregator();
        _staticHandlerCount = 0;

        RegisterDeadSubscriber(aggregator);
        aggregator.Subscribe<TestMessageA>(StaticHandler);

        ForceGarbageCollection();

        aggregator.Publish(new TestMessageA());
        Assert.Equal(1, _staticHandlerCount);
    }
}
