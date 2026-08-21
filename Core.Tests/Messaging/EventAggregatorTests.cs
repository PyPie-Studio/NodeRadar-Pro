using NodeRadarPro.Core.Messaging;

namespace NodeRadarPro.Core.Tests.Messaging;

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

        aggregator.Subscribe<TestMessageA>(msg => callCountA++);

        aggregator.Publish(new TestMessageB());

        Assert.Equal(0, callCountA);
    }

    [Fact]
    public void Publish_TriggersMultipleSubscribersForSameMessageType()
    {
        var aggregator = new EventAggregator();
        int callCount1 = 0;
        int callCount2 = 0;

        aggregator.Subscribe<TestMessageA>(msg => callCount1++);
        aggregator.Subscribe<TestMessageA>(msg => callCount2++);

        aggregator.Publish(new TestMessageA());

        Assert.Equal(1, callCount1);
        Assert.Equal(1, callCount2);
    }
}
