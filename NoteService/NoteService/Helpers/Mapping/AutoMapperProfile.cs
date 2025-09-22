using AutoMapper;
using NoteService.Entities;

namespace UserService.Helpers.Mapping
{
	public class AutoMapperProfile : Profile
	{
		public AutoMapperProfile()
		{
			CreateMap<NoteDto, Note>()
				.ForMember(dest => dest.NoteId, opt => opt.Ignore())
				.ForMember(dest => dest.UserId, opt => opt.Ignore())
				.ForMember(dest => dest.LastModifiedDate, opt => opt.Ignore());

			// Note'dan NoteDto'ya dönüşüm için:
			CreateMap<Note, NoteDto>();
		}
	}
}
