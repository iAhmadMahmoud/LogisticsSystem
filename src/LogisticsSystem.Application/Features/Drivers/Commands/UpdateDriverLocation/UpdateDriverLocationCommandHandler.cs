using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Features.Drivers.Specifications;
using LogisticsSystem.Domain.Entities;
using MediatR;

namespace LogisticsSystem.Application.Features.Drivers.Commands.UpdateDriverLocation
{
    public sealed class UpdateDriverLocationCommandHandler : IRequestHandler<UpdateDriverLocationCommand>
    {
        private readonly IGenericRepository<Driver> _driverRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateDriverLocationCommandHandler(IGenericRepository<Driver> driverRepository, ICurrentUserService currentUserService, IUnitOfWork unitOfWork)
        {
            _driverRepository = driverRepository;
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(UpdateDriverLocationCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            var specification = new DriverByUserIdSpecification(userId);

            var driver = await _driverRepository.FirstOrDefaultAsync(specification,cancellationToken);

            if(driver is null)
            {
                throw new KeyNotFoundException("Driver profile was not found.");
            }

            driver.Latitude = request.Latitude;
            driver.Longitude = request.Longitude;

            _driverRepository.Update(driver);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
