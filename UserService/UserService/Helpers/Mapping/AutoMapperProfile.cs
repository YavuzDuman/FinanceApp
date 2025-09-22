using AutoMapper;
using UserService.Entities.Concrete;
using UserService.Entities.Dto;

namespace UserService.Helpers.Mapping
{
	public class AutoMapperProfile : Profile
	{
		public AutoMapperProfile()
		{
			CreateMap<User, UserDto>()
				.ForMember(dest => dest.RoleName,
					opt => opt.MapFrom(src =>
						src.UserRoles.FirstOrDefault() != null
							? src.UserRoles.FirstOrDefault().Role.Name
							: null));

			CreateMap<UserDto, User>();
			CreateMap<RegisterDto, User>();
			CreateMap<LoginDto, User>();
		}
	}
}
