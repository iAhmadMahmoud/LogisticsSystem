using LogisticsSystem.Application.Common.Specifications;
using LogisticsSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace LogisticsSystem.Application.Specifications
{
    public sealed class RefreshTokenByTokenSpecification : BaseSpecification<RefreshToken>
    {
        public RefreshTokenByTokenSpecification(string token)
        {
            AddCriteria(x => x.Token == token);
        }
        
    }
}
