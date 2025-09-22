using Microsoft.AspNetCore.Authorization;

namespace WebApi.Helpers.Authorization
{
	public class OwnerAuthorizationRequirement : IAuthorizationRequirement
	{
		// Bu sınıf, yetkilendirme kuralımızı işaretlemek için boş bir arayüzdür.
		// Tek görevi, "bu bir sahip kontrolü kuralıdır" demektir.
	}
}
