using AutoMapper;
using CleanArc.Application.Contracts.DTOs.User;
using CleanArc.Application.Profiles;
using CleanArc.Domain.Entities.User;

namespace CleanArc.Web.Api.Profiles;

public class UserMappingProfile : AutoMapper.Profile, ICreateMapper<User>
{
    public UserMappingProfile()
    {
        CreateMap<User, GetUserProfileResponse>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id))
            .ReverseMap();
    }

    public void Map(IProfileExpression profile)
    {
        profile.CreateMap<User, GetUserProfileResponse>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id));
    }
}
