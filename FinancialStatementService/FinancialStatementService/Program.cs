using FinancialStatementService.Business;
using FinancialStatementService.Business.Abstract;
using FinancialStatementService.DataAccess.Abstract;
using FinancialStatementService.DataAccess.Concrete;
using FinancialStatementService.DataAccess.DbConnectionFactory;
using FinancialStatementService.BackgroundServices;
using Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddCentralizedLogging();

// Art�k IHttpClientFactory'ye gerek yok
// builder.Services.AddHttpClient("FintablesClient", client => { ... });

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

builder.Services.AddScoped<IFinancialStatementManager, FinancialStatementManager>();
builder.Services.AddScoped<IFinancialStatementRepository, FinancialStatementRepository>();

builder.Services.AddScoped<IDbConnectionFactory, SqlConnectionFactory>();

// Background Service: Günde bir kere bilançoları otomatik olarak çek
builder.Services.AddHostedService<FinancialStatementUpdateWorker>();

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

builder.Services.AddControllers()
	.AddJsonOptions(options =>
	{
		options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
		options.JsonSerializerOptions.WriteIndented = false;
	});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Merkezi JWT Doğrulama
builder.Services.AddCentralizedJwt(builder.Configuration);

// Merkezi Authorization Policy'leri ekle
builder.Services.AddCentralizedAuthorization();

// Health Checks
builder.Services.AddHealthChecks();

// Response Compression (gzip/brotli)
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

var app = builder.Build();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();