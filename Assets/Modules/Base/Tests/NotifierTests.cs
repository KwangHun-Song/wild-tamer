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
        private Notifier _notifier;

        [SetUp]
        public void SetUp()
        {
            _notifier = new Notifier();
        }

        [Test]
        public void Subscribe_And_Notify_CallsListener()
        {
            var listener = new TestListener();
            _notifier.Subscribe(listener);

            _notifier.Notify<ITestEvent>(l => l.OnEvent("hello"));

            Assert.AreEqual("hello", listener.LastMessage);
            Assert.AreEqual(1, listener.CallCount);
        }

        [Test]
        public void Notify_WithoutSubscribers_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                _notifier.Notify<ITestEvent>(l => l.OnEvent("hello"));
            });
        }

        [Test]
        public void Unsubscribe_StopsReceivingNotifications()
        {
            var listener = new TestListener();
            _notifier.Subscribe(listener);
            _notifier.Unsubscribe(listener);

            _notifier.Notify<ITestEvent>(l => l.OnEvent("hello"));

            Assert.AreEqual(0, listener.CallCount);
        }

        [Test]
        public void MultipleListeners_AllReceiveNotification()
        {
            var listener1 = new TestListener();
            var listener2 = new TestListener();
            _notifier.Subscribe(listener1);
            _notifier.Subscribe(listener2);

            _notifier.Notify<ITestEvent>(l => l.OnEvent("broadcast"));

            Assert.AreEqual("broadcast", listener1.LastMessage);
            Assert.AreEqual("broadcast", listener2.LastMessage);
        }

        [Test]
        public void MultiListener_ReceivesBothEventTypes()
        {
            var listener = new MultiListener();
            _notifier.Subscribe(listener);

            _notifier.Notify<ITestEvent>(l => l.OnEvent("test"));
            _notifier.Notify<IOtherEvent>(l => l.OnOther(42));

            Assert.AreEqual("test", listener.LastMessage);
            Assert.AreEqual(42, listener.LastValue);
        }

        [Test]
        public void DuplicateSubscribe_NotifiesOnlyOnce()
        {
            var listener = new TestListener();
            _notifier.Subscribe(listener);
            _notifier.Subscribe(listener);

            _notifier.Notify<ITestEvent>(l => l.OnEvent("once"));

            Assert.AreEqual(1, listener.CallCount);
        }

        [Test]
        public void Unsubscribe_WithoutSubscribe_DoesNotThrow()
        {
            var listener = new TestListener();

            Assert.DoesNotThrow(() =>
            {
                _notifier.Unsubscribe(listener);
            });
        }

        [Test]
        public void Notify_UnsubscribeDuringIteration_DoesNotThrow()
        {
            var listener1 = new TestListener();
            var listenerThatUnsubscribes = new UnsubscribingListener(_notifier);
            _notifier.Subscribe(listenerThatUnsubscribes);
            _notifier.Subscribe(listener1);

            Assert.DoesNotThrow(() =>
            {
                _notifier.Notify<ITestEvent>(l => l.OnEvent("safe"));
            });
        }

        [Test]
        public void Unsubscribe_OneOfMultiple_OthersStillReceive()
        {
            var listener1 = new TestListener();
            var listener2 = new TestListener();
            _notifier.Subscribe(listener1);
            _notifier.Subscribe(listener2);

            _notifier.Unsubscribe(listener1);
            _notifier.Notify<ITestEvent>(l => l.OnEvent("remaining"));

            Assert.AreEqual(0, listener1.CallCount);
            Assert.AreEqual("remaining", listener2.LastMessage);
        }

        [Test]
        public void Notify_DifferentEventType_DoesNotCallUnrelatedListeners()
        {
            var listener = new TestListener();
            _notifier.Subscribe(listener);

            _notifier.Notify<IOtherEvent>(l => l.OnOther(99));

            Assert.AreEqual(0, listener.CallCount);
        }

        private class UnsubscribingListener : ITestEvent
        {
            private readonly Notifier _notifier;

            public UnsubscribingListener(Notifier notifier)
            {
                _notifier = notifier;
            }

            public void OnEvent(string message)
            {
                _notifier.Unsubscribe(this);
            }
        }
    }
}
