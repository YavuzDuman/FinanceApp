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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Merkezi middleware'leri ekle
app.UseCentralizedMiddleware();

// JWT doğrulama - Güvenlik için geri eklendi
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
