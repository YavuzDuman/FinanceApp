using AutoMapper;
using FinancialStatementService.Entities;

namespace UserService.Helpers.Mapping
{
	public class AutoMapperProfile : Profile
	{
		public AutoMapperProfile()
		{
			CreateMap<FinancialStatement, FinancialStatementDto>();

			CreateMap<FinancialStatementDto, FinancialStatement>().ForMember(dest => dest.Id, opt => opt.Ignore());
		}
	}
}
