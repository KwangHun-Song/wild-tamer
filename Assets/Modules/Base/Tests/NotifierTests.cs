using System;
using NUnit.Framework;

namespace Base.Tests
{
    public interface ITestEvent : IListener
    {
        void OnEvent(string message);
    }

    public interface IOtherEvent : IListener
    {
        void OnOther(int value);
    }

    public class TestListener : ITestEvent
    {
        public string LastMessage { get; private set; }
        public int CallCount { get; private set; }

        public void OnEvent(string message)
        {
            LastMessage = message;
            CallCount++;
        }
    }

    public class MultiListener : ITestEvent, IOtherEvent
    {
        public string LastMessage { get; private set; }
        public int LastValue { get; private set; }

        public void OnEvent(string message)
        {
            LastMessage = message;
        }

        public void OnOther(int value)
        {
            LastValue = value;
        }
    }

    public class NotifierTests
    {
        private Notifier notifier;

        [SetUp]
        public void SetUp()
        {
            notifier = new Notifier();
        }

        [Test]
        public void Subscribe_And_Notify_CallsListener()
        {
            var listener = new TestListener();
            notifier.Subscribe(listener);

            notifier.Notify<ITestEvent>(l => l.OnEvent("hello"));

            Assert.AreEqual("hello", listener.LastMessage);
            Assert.AreEqual(1, listener.CallCount);
        }

        [Test]
        public void Notify_WithoutSubscribers_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                notifier.Notify<ITestEvent>(l => l.OnEvent("hello"));
            });
        }

        [Test]
        public void Unsubscribe_StopsReceivingNotifications()
        {
            var listener = new TestListener();
            notifier.Subscribe(listener);
            notifier.Unsubscribe(listener);

            notifier.Notify<ITestEvent>(l => l.OnEvent("hello"));

            Assert.AreEqual(0, listener.CallCount);
        }

        [Test]
        public void MultipleListeners_AllReceiveNotification()
        {
            var listener1 = new TestListener();
            var listener2 = new TestListener();
            notifier.Subscribe(listener1);
            notifier.Subscribe(listener2);

            notifier.Notify<ITestEvent>(l => l.OnEvent("broadcast"));

            Assert.AreEqual("broadcast", listener1.LastMessage);
            Assert.AreEqual("broadcast", listener2.LastMessage);
        }

        [Test]
        public void MultiListener_ReceivesBothEventTypes()
        {
            var listener = new MultiListener();
            notifier.Subscribe(listener);

            notifier.Notify<ITestEvent>(l => l.OnEvent("test"));
            notifier.Notify<IOtherEvent>(l => l.OnOther(42));

            Assert.AreEqual("test", listener.LastMessage);
            Assert.AreEqual(42, listener.LastValue);
        }

        [Test]
        public void DuplicateSubscribe_NotifiesOnlyOnce()
        {
            var listener = new TestListener();
            notifier.Subscribe(listener);
            notifier.Subscribe(listener);

            notifier.Notify<ITestEvent>(l => l.OnEvent("once"));

            Assert.AreEqual(1, listener.CallCount);
        }

        [Test]
        public void Unsubscribe_WithoutSubscribe_DoesNotThrow()
        {
            var listener = new TestListener();

            Assert.DoesNotThrow(() =>
            {
                notifier.Unsubscribe(listener);
            });
        }

        [Test]
        public void Notify_UnsubscribeDuringIteration_DoesNotThrow()
        {
            var listener1 = new TestListener();
            var listenerThatUnsubscribes = new UnsubscribingListener(notifier);
            notifier.Subscribe(listenerThatUnsubscribes);
            notifier.Subscribe(listener1);

            Assert.DoesNotThrow(() =>
            {
                notifier.Notify<ITestEvent>(l => l.OnEvent("safe"));
            });
        }

        [Test]
        public void Unsubscribe_OneOfMultiple_OthersStillReceive()
        {
            var listener1 = new TestListener();
            var listener2 = new TestListener();
            notifier.Subscribe(listener1);
            notifier.Subscribe(listener2);

            notifier.Unsubscribe(listener1);
            notifier.Notify<ITestEvent>(l => l.OnEvent("remaining"));

            Assert.AreEqual(0, listener1.CallCount);
            Assert.AreEqual("remaining", listener2.LastMessage);
        }

        [Test]
        public void Notify_DifferentEventType_DoesNotCallUnrelatedListeners()
        {
            var listener = new TestListener();
            notifier.Subscribe(listener);

            notifier.Notify<IOtherEvent>(l => l.OnOther(99));

            Assert.AreEqual(0, listener.CallCount);
        }

        private class UnsubscribingListener : ITestEvent
        {
            private readonly Notifier notifier;

            public UnsubscribingListener(Notifier notifier)
            {
                this.notifier = notifier;
            }

            public void OnEvent(string message)
            {
                notifier.Unsubscribe(this);
            }
        }
    }
}
