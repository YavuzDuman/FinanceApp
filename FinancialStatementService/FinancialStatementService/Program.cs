using FinancialStatementService.Business;
using FinancialStatementService.Business.Abstract;
using FinancialStatementService.DataAccess.Abstract;
using FinancialStatementService.DataAccess.Concrete;
using FinancialStatementService.DataAccess.DbConnectionFactory;
using Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddCentralizedLogging();

// Art�k IHttpClientFactory'ye gerek yok
// builder.Services.AddHttpClient("FintablesClient", client => { ... });

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

builder.Services.AddScoped<IFinancialStatementManager, FinancialStatementManager>();
builder.Services.AddScoped<IFinancialStatementRepository, FinancialStatementRepository>();

builder.Services.AddScoped<IDbConnectionFactory, SqlConnectionFactory>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Merkezi JWT Doğrulama
builder.Services.AddCentralizedJwt(builder.Configuration);

// Merkezi Authorization Policy'leri ekle
builder.Services.AddCentralizedAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Merkezi middleware'leri ekle
app.UseCentralizedMiddleware();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();