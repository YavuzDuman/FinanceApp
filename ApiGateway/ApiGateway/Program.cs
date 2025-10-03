using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

// Ocelot yaplandrma dosyasn (ocelot.json) ekleyin
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// JWT Claim eşleştirmesini temizle (İYİ UYGULAMA)
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

// ----------------------------------------------------------------------------------
// BÜYÜK DEĞİŞİKLİK: JWT Authentication konfigürasyonu
// Default scheme yerine Ocelot'ın kullanacağı adlandırılmış bir şema ("OcelotAuthScheme") eklenir
// ----------------------------------------------------------------------------------

// builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme) yerine:
builder.Services.AddAuthentication()
	.AddJwtBearer("OcelotAuthScheme", options => // *** ADLANDIRILMIŞ ŞEMA KULLANIYORUZ ***
	{
		// ... Mevcut ayarlarınız
		options.IncludeErrorDetails = true;
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuerSigningKey = true,
			// Key'i configuration'dan alıyorsunuz
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),

			ValidateIssuer = true,
			ValidIssuer = builder.Configuration["Jwt:Issuer"],

			ValidateAudience = true,
			ValidAudience = builder.Configuration["Jwt:Audience"],

			ValidateLifetime = true,
			ClockSkew = TimeSpan.Zero
		};
	});

// Authorization servisini ekle
builder.Services.AddAuthorization();

// Ocelot servislerini konteynere ekleyin
builder.Services.AddOcelot();

// Diğer servisler ...
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ----------------------------------------------------------------------------------
// MİDDLEWARE SIRALAMASI ÖNEMLİ!
// ----------------------------------------------------------------------------------
app.UseAuthentication();
app.UseAuthorization(); // Bu ikisi UseOcelot'tan önce gelmelidir.

// Ocelot middleware'ini en son ekle
await app.UseOcelot();

app.MapControllers();

app.Run();