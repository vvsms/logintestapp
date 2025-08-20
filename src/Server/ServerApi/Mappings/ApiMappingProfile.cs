using AutoMapper;
using ServerApi.Infrastructure.Identity;
using SharedDtos;

namespace ServerApi.Mappings;

public class ApiMappingProfile : Profile
{
    public ApiMappingProfile()
    {
        CreateMap<RegisterRequest, ApplicationUser>()
            .ForMember(d => d.UserName, o => o.MapFrom(s => s.Email));
    }
}