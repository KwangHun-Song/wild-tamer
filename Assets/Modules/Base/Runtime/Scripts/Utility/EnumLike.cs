using System;

namespace Base
{
    public abstract class EnumLike<T> : IEquatable<T>, IComparable<T> where T : EnumLike<T>
    {
        public int Value { get; }
        public string Name { get; }

        protected EnumLike(int value, string name)
        {
            Value = value;
            Name = name;
        }

        public bool Equals(T other)
        {
            return other != null && Value == other.Value;
        }

        public int CompareTo(T other)
        {
            if (other == null)
            {
                return 1;
            }

            return Value.CompareTo(other.Value);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as T);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return Name;
        }

        public static bool operator ==(EnumLike<T> left, EnumLike<T> right)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.Equals(right as T);
        }

        public static bool operator !=(EnumLike<T> left, EnumLike<T> right)
        {
            return !(left == right);
        }
    }
}
