using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using NoteService.Business;
using NoteService.DataAccess;
using NoteService.DataAccess.ConnectionFactory;
using System.Text;
using Shared.Extensions;
using FluentValidation.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Merkezi loglama konfigürasyonu
builder.AddCentralizedLogging();

// Merkezi JWT Doğrulama - Güvenlik için geri eklendi
// Çift katmanlı koruma: API Gateway + Service Level
builder.Services.AddCentralizedJwt(builder.Configuration);

// Merkezi Authorization Policy'leri ekle
builder.Services.AddCentralizedAuthorization();

// CORS Konfigürasyonu - Frontend ile iletişim için
builder.Services.AddCors(options =>
{
	options.AddPolicy("CorsPolicy",
		builder => builder
			.WithOrigins(
				"http://localhost:5173",
				"https://localhost:5173",
				"https://localhost:5000",
				"http://localhost:5000"
			)
			.AllowAnyMethod()
			.AllowAnyHeader()
			.AllowCredentials());
});

// Add services to the container.

// JWT Token Cache için Memory Cache ekle
builder.Services.AddMemoryCache();

builder.Services.AddScoped<INotesManager, NotesManager>();
builder.Services.AddScoped<INotesRepository, NotesRepository>();

builder.Services.AddScoped < IDbConnectionFactory, SqlConnectionFactory>();


builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

builder.Services.AddControllers()
	.AddFluentValidation(fv =>
	{
		// Validat�rleri otomatik olarak bul ve kaydet
		fv.RegisterValidatorsFromAssemblyContaining<Program>();

		fv.DisableDataAnnotationsValidation = false;
	});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Health Checks
builder.Services.AddHealthChecks();

// Response Compression (gzip/brotli)
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Merkezi middleware'leri ekle
app.UseCentralizedMiddleware();

// Routing'i ekle - Middleware sıralaması için gerekli
app.UseRouting();
app.UseCors("CorsPolicy"); // CORS'u routing'den sonra, authentication'dan önce ekle
// JWT doğrulama - Güvenlik için geri eklendi
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
