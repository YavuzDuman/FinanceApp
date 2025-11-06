using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Http; // HttpClient Factory için
using System.Net.Http; // HttpClientHandler için
using Microsoft.Extensions.Configuration; // IConfiguration için
using System; // InvalidOperationException için

var builder = WebApplication.CreateBuilder(args);

// IHttpClientFactory servisini ekle (AuthProxyController için gerekli)
builder.Services.AddHttpClient();

// Ocelot Downstream Servislerinin Sertifika Doğrulamasını Bypass Etme (Yalnızca Dev/Test Ortamı İçin)
if (builder.Environment.IsDevelopment())
{
	builder.Services.ConfigureAll<HttpClientFactoryOptions>(options =>
	{
		options.HttpMessageHandlerBuilderActions.Add(handlerBuilder =>
		{
			handlerBuilder.PrimaryHandler = new HttpClientHandler
			{
				// Localhost'taki kendi kendine imzalanmış sertifikalara güvenmeyi sağlar
				ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
			};
		});
	});
}

builder.Services.AddCors(options =>
{
	options.AddPolicy("CorsPolicy",
		builder => builder
			// Frontend adresleri (Render'da bu listeye Canlı Alan adınızı ekleyeceksiniz)
			.WithOrigins(
				"http://localhost:5173",
				"https://localhost:5173",
				// API Gateway ve diğer local adresler
				"https://localhost:5000",
				"http://localhost:5000"
			)
			.AllowAnyMethod()
			.AllowAnyHeader()
			.AllowCredentials()); // SignalR, Token ve Cookie için kritik
});

// Ocelot yaplandrma dosyasn (ocelot.json) ekleyin
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// JWT Claim eşleştirmesini temizle (İYİ UYGULAMA)
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

// ----------------------------------------------------------------------------------
// JWT Authentication konfigürasyonu
// ----------------------------------------------------------------------------------

// 1. JWT Anahtarını güvenli bir şekilde al ve zorla (Render'da Jwt__Key olarak bekleniyor)
var jwtKey = builder.Configuration["Jwt:Key"]; // appsettings.json'dan veya Environment Variable'dan alır.

if (string.IsNullOrEmpty(jwtKey))
{
	// Canlı ortamda anahtar yoksa uygulamayı durdurmak zorundayız.
	throw new InvalidOperationException("JWT Signing Key (Jwt:Key) configuration is missing or empty. Check 'Jwt__Key' environment variable on Render.");
}

builder.Services.AddAuthentication()
	.AddJwtBearer("OcelotAuthScheme", options => // *** ADLANDIRILMIŞ ŞEMA KULLANIYORUZ ***
	{
		options.IncludeErrorDetails = true;
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuerSigningKey = true,
			// Düzeltme: Burada artık anahtarın boş olmadığını biliyoruz.
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),

			// Diğer JWT parametreleri
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
// Health checks
builder.Services.AddHealthChecks();

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
// 1. Yönlendirmeyi (Routing) başlat
app.UseRouting();

// 2. CORS'u uygulayın (Authentication/Authorization'dan önce)
app.UseCors("CorsPolicy");

// 3. Kimlik Doğrulama ve Yetkilendirmeyi uygulayın
app.UseAuthentication();
app.UseAuthorization();

// 4. Endpointleri ve Controller'ları haritalayın (AuthProxyController burada çalışır)
app.MapControllers();
// Health endpoint (gateway'in kendi sağlığı)
app.MapHealthChecks("/health");

// 5. OCELOT'u en sona yakın çalıştırın (MapControllers'a gitmeyen tüm yönlendirmeler Ocelot'a kalır)
await app.UseOcelot();

app.Run();