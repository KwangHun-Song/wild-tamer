 using NUnit.Framework;

namespace Base.Tests
{
    public class TestEnum : EnumLike<TestEnum>
    {
        public static readonly TestEnum Alpha = new(0, "Alpha");
        public static readonly TestEnum Beta = new(1, "Beta");
        public static readonly TestEnum Gamma = new(2, "Gamma");

        private TestEnum(int value, string name) : base(value, name) { }
    }

    public class EnumLikeTests
    {
        [Test]
        public void Equals_SameValue_ReturnsTrue()
        {
            Assert.IsTrue(TestEnum.Alpha.Equals(TestEnum.Alpha));
        }

        [Test]
        public void Equals_DifferentValue_ReturnsFalse()
        {
            Assert.IsFalse(TestEnum.Alpha.Equals(TestEnum.Beta));
        }

        [Test]
        public void Equals_Null_ReturnsFalse()
        {
            Assert.IsFalse(TestEnum.Alpha.Equals(null));
        }

        [Test]
        public void OperatorEquals_SameValue_ReturnsTrue()
        {
            Assert.IsTrue(TestEnum.Alpha == TestEnum.Alpha);
        }

        [Test]
        public void OperatorEquals_DifferentValue_ReturnsFalse()
        {
            Assert.IsFalse(TestEnum.Alpha == TestEnum.Beta);
        }

        [Test]
        public void OperatorNotEquals_DifferentValue_ReturnsTrue()
        {
            Assert.IsTrue(TestEnum.Alpha != TestEnum.Beta);
        }

        [Test]
        public void OperatorEquals_BothNull_ReturnsTrue()
        {
            TestEnum a = null;
            TestEnum b = null;
            Assert.IsTrue(a == b);
        }

        [Test]
        public void OperatorEquals_OneNull_ReturnsFalse()
        {
            TestEnum a = null;
            Assert.IsTrue(a != TestEnum.Alpha);
        }

        [Test]
        public void GetHashCode_SameValue_SameHash()
        {
            Assert.AreEqual(TestEnum.Alpha.GetHashCode(), TestEnum.Alpha.GetHashCode());
        }

        [Test]
        public void GetHashCode_DifferentValue_DifferentHash()
        {
            Assert.AreNotEqual(TestEnum.Alpha.GetHashCode(), TestEnum.Beta.GetHashCode());
        }

        [Test]
        public void ToString_ReturnsName()
        {
            Assert.AreEqual("Alpha", TestEnum.Alpha.ToString());
            Assert.AreEqual("Beta", TestEnum.Beta.ToString());
        }

        [Test]
        public void Value_ReturnsCorrectInt()
        {
            Assert.AreEqual(0, TestEnum.Alpha.Value);
            Assert.AreEqual(1, TestEnum.Beta.Value);
            Assert.AreEqual(2, TestEnum.Gamma.Value);
        }

        [Test]
        public void CompareTo_LessThan_ReturnsNegative()
        {
            Assert.Less(TestEnum.Alpha.CompareTo(TestEnum.Beta), 0);
        }

        [Test]
        public void CompareTo_GreaterThan_ReturnsPositive()
        {
            Assert.Greater(TestEnum.Gamma.CompareTo(TestEnum.Alpha), 0);
        }

        [Test]
        public void CompareTo_Equal_ReturnsZero()
        {
            Assert.AreEqual(0, TestEnum.Alpha.CompareTo(TestEnum.Alpha));
        }

        [Test]
        public void CompareTo_Null_ReturnsPositive()
        {
            Assert.Greater(TestEnum.Alpha.CompareTo(null), 0);
        }

        [Test]
        public void Equals_Object_SameValue_ReturnsTrue()
        {
            object obj = TestEnum.Alpha;
            Assert.IsTrue(TestEnum.Alpha.Equals(obj));
        }

        [Test]
        public void Equals_Object_DifferentType_ReturnsFalse()
        {
            object obj = "Alpha";
            Assert.IsFalse(TestEnum.Alpha.Equals(obj));
        }
    }
}
