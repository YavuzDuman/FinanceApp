using AutoMapper;
using FinancialStatementService.Entities;

namespace UserService.Helpers.Mapping
{
	public class AutoMapperProfile : Profile
	{
		public AutoMapperProfile()
		{
			CreateMap<FinancialStatement, FinancialStatementDto>()
				.ForMember(dest => dest.StatementDate, opt => opt.MapFrom(src => 
					src.StatementDate.Kind == DateTimeKind.Unspecified 
						? DateTime.SpecifyKind(src.StatementDate, DateTimeKind.Utc)
						: src.StatementDate.ToUniversalTime()))
				.ForMember(dest => dest.AnnouncementDate, opt => opt.MapFrom(src => 
					src.AnnouncementDate.HasValue 
						? (src.AnnouncementDate.Value.Kind == DateTimeKind.Unspecified
							? DateTime.SpecifyKind(src.AnnouncementDate.Value, DateTimeKind.Utc)
							: src.AnnouncementDate.Value.ToUniversalTime())
						: (DateTime?)null));

			CreateMap<FinancialStatementDto, FinancialStatement>().ForMember(dest => dest.Id, opt => opt.Ignore());
		}
	}
}
