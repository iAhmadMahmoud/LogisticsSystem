namespace LogisticsSystem.Domain.Common.ValueObjects
{
    public abstract class ValueObject
    {
        protected abstract IEnumerable<object?> GetEqualityComponents();

        public override bool Equals(object? obj)
        {
            if(obj is not ValueObject other)
                return false;

            return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();

            foreach (var component in GetEqualityComponents())
            {
                hashCode.Add(component);
            }

            return hashCode.ToHashCode();
        }

        public static bool operator ==(ValueObject left, ValueObject right) =>Equals(left, right);
        public static bool operator !=(ValueObject left,ValueObject right) => !Equals(left, right);
    }
}
