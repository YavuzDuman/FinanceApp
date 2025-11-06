using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

// IHttpClientFactory servisini ekle
builder.Services.AddHttpClient();

// Ocelot için SSL sertifika doğrulamasını bypass et (Development)
if (builder.Environment.IsDevelopment())
{
	builder.Services.ConfigureAll<HttpClientFactoryOptions>(options =>
	{
		options.HttpMessageHandlerBuilderActions.Add(handlerBuilder =>
		{
			handlerBuilder.PrimaryHandler = new HttpClientHandler
			{
				ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
			};
		});
	});
}

builder.Services.AddCors(options =>
{
	options.AddPolicy("CorsPolicy",
		builder => builder
			// *** YILDIZ (*) YERİNE SPESİFİK KAYNAKLARI LİSTELİYORUZ ***
			.WithOrigins(
				// React Frontend adresi
				"http://localhost:5173",
				"https://localhost:5173",
				// API Gateway ve diğer local adresler
				"https://localhost:5000",
				"http://localhost:5000"
			)
			.AllowAnyMethod()
			.AllowAnyHeader()
			.AllowCredentials()); // SignalR için bu artık geçerlidir
});

// Ocelot yaplandrma dosyasn (ocelot.json) ekleyin
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// JWT Claim eşleştirmesini temizle (İYİ UYGULAMA)
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

// ----------------------------------------------------------------------------------
// BÜYÜK DEĞİŞİKLİK: JWT Authentication konfigürasyonu
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
// 1. Yönlendirmeyi (Routing) başlat
app.UseRouting();

// 2. CORS'u uygulayın (Authentication/Authorization'dan önce)
app.UseCors("CorsPolicy");

// 3. Kimlik Doğrulama ve Yetkilendirmeyi uygulayın
app.UseAuthentication();
app.UseAuthorization();

// 4. Endpointleri ve Controller'ları haritalayın
app.MapControllers();

// 5. OCELOT'u çalıştırın - Tüm route yönlendirmelerini Ocelot yönetir
await app.UseOcelot();

app.Run();