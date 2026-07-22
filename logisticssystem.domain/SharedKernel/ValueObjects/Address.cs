using LogisticsSystem.Domain.Common.Exceptions;
using LogisticsSystem.Domain.Common.ValueObjects;

namespace LogisticsSystem.Domain.SharedKernel.ValueObjects
{
    public sealed class Address : ValueObject
    {
        public string Street { get; }
        public string City { get; }
        public string State { get; }
        public string Country { get; }
        public string PostalCode { get; }
        public Address
            (
            string street,
            string city,
            string state,
            string country,
            string postalCode
            )
        {
            if(string.IsNullOrWhiteSpace(street))
                throw new DomainException("Street is required.");
            if(string.IsNullOrWhiteSpace(city))
                throw new DomainException("City is required.");
            if(string.IsNullOrWhiteSpace(country))
                throw new DomainException("Country is required.");

            Street = street.Trim();
            City = city.Trim();
            State = state.Trim();
            Country = country.Trim();
            PostalCode = postalCode.Trim();


        }
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Street;
            yield return City;
            yield return State;
            yield return Country;
            yield return PostalCode;
        }
    }
}
