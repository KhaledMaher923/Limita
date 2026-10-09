using MediatR;

namespace Limita.Application.Features.AppInfo.GetAppInformation;

public sealed record GetAppInformationQuery : IRequest<AppInformationDto>;
public sealed record AppInformationDto(string Name, string Version, string CustomerServicePhone, string SupportEmail);

public sealed class GetAppInformationHandler : IRequestHandler<GetAppInformationQuery, AppInformationDto>
{
    public Task<AppInformationDto> Handle(GetAppInformationQuery request, CancellationToken ct) =>
        Task.FromResult(new AppInformationDto("Limita E-mobile Banking", "9.0.2", "19008989", "support@limita.example"));
}
