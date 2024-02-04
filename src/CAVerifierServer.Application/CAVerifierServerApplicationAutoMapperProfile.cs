using AutoMapper;
using CAVerifierServer.Account;
using CAVerifierServer.Application;
using CAVerifierServer.Grains.Grain.ThirdPartyVerification;
using CAVerifierServer.Verifier.Dtos;

namespace CAVerifierServer;

public class CAVerifierServerApplicationAutoMapperProfile : Profile
{
    public CAVerifierServerApplicationAutoMapperProfile()
    {
        CreateMap<CAServer, DidServer>();
        CreateMap<VerifyGoogleTokenGrainDto, VerifyGoogleTokenDto>();
        CreateMap<VerifyAppleTokenGrainDto, VerifyAppleTokenDto>();
        CreateMap<VerifyTelegramTokenGrainDto, VerifyTokenDto<TelegramUserExtraInfo>>()
            .ForMember(t => t.UserExtraInfo, m => m.MapFrom(f => f.TelegramUserExtraInfo));
        CreateMap<VerifyTokenRequestDto, VerifyTokenGrainDto>();
    }
}
