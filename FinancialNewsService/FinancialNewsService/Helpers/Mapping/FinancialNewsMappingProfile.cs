using AutoMapper;
using FinancialNewsService.Entities;

namespace FinancialNewsService.Helpers.Mapping
{
	public class FinancialNewsMappingProfile : Profile
	{
		public FinancialNewsMappingProfile()
		{
			CreateMap<FinancialNews, FinancialNewsDto>();
			CreateMap<FinancialNewsDto, FinancialNews>();
		}
	}
}
