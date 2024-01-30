using AutoMapper;
using CAVerifierServer.Account;
using CAVerifierServer.Application;
using CAVerifierServer.Grains.Grain.ThirdPartyVerification;

namespace CAVerifierServer;

public class CAVerifierServerApplicationAutoMapperProfile : Profile
{
    public CAVerifierServerApplicationAutoMapperProfile()
    {
        CreateMap<CAServer, DidServer>();
        CreateMap<VerifyGoogleTokenGrainDto, VerifyGoogleTokenDto>();
        CreateMap<VerifyAppleTokenGrainDto, VerifyAppleTokenDto>();
        CreateMap<VerifyTokenRequestDto, VerifyTokenGrainDto>();
        CreateMap<VerifyTwitterTokenGrainDto, VerifyTwitterTokenDto>();
    }
}
