using AutoMapper;
using CAVerifierServer.Account;

namespace CAVerifierServer.Grains;

public class CAVerifierServerGrainsAutoMapperProfile : Profile
{
    public CAVerifierServerGrainsAutoMapperProfile()
    {
        CreateMap<GoogleUserInfoDto, GoogleUserExtraInfo>();
        CreateMap<TwitterUserInfo, TwitterUserExtraInfo>();
    }
}