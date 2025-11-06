using AutoMapper;
using UserService.Entities.Concrete;
using UserService.Entities.Dto;

namespace UserService.Helpers.Mapping
{
	public class AutoMapperProfile : Profile
	{
		public AutoMapperProfile()
		{
			// ⚡ Immutable record için ConstructUsing kullan - N+1 query sorununu çözer
			CreateMap<User, UserDto>()
				.ConstructUsing(src => new UserDto(
					src.UserId,
					src.Name ?? string.Empty,
					src.Username ?? string.Empty,
					src.Email ?? string.Empty,
					src.UserRoles.Select(ur => ur.Role.Name).FirstOrDefault() ?? "User", // ⚡ Tek seferde çözüm
					src.InsertDate,
					src.IsActive
				));

			CreateMap<UserDto, User>()
				.ForMember(dest => dest.InsertDate,
					opt => opt.MapFrom(src => src.RegistrationDate));
			
			// UpdateProfileDto -> User mapping (profil güncelleme için)
			CreateMap<UpdateProfileDto, User>();
			
			CreateMap<RegisterDto, User>();
			CreateMap<LoginDto, User>();
		}
	}
}
