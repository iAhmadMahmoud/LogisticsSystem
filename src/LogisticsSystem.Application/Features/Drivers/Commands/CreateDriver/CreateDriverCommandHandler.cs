using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Models.Authentication;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using MediatR;

namespace LogisticsSystem.Application.Features.Drivers.Commands.CreateDriver
{
    public sealed class CreateDriverCommandHandler : IRequestHandler<CreateDriverCommand, Guid>
    {
        private readonly IIdentityService _identityService;
        private readonly IGenericRepository<Driver> _driverRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateDriverCommandHandler(IIdentityService identityService, IGenericRepository<Driver> driverRepository, IUnitOfWork unitOfWork)
        {
            _identityService = identityService;
            _driverRepository = driverRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(CreateDriverCommand request, CancellationToken cancellationToken)
        {
            var identityRequest = new CreateDriverIdentityRequest
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                UserName = request.Username,
                Email = request.Email,
                Password = request.Password,
            };

            var userId = await _identityService.CreateDriverAsync(identityRequest,cancellationToken);

            var driver = new Driver
            {
                UserId = userId,
                LicenseNumber = request.LicenseNumber,
                Status = DriverStatus.Offline
            };

            await _driverRepository.AddAsync(driver,cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return driver.Id;
        }
    }
}
