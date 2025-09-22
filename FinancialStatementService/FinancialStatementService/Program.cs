using FinancialStatementService.Business;
using FinancialStatementService.Business.Abstract;
using FinancialStatementService.DataAccess.Abstract;
using FinancialStatementService.DataAccess.Concrete;
using FinancialStatementService.DataAccess.DbConnectionFactory;

var builder = WebApplication.CreateBuilder(args);

// Artýk IHttpClientFactory'ye gerek yok
// builder.Services.AddHttpClient("FintablesClient", client => { ... });

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

builder.Services.AddScoped<IFinancialStatementManager, FinancialStatementManager>();
builder.Services.AddScoped<IFinancialStatementRepository, FinancialStatementRepository>();

builder.Services.AddScoped<IDbConnectionFactory, SqlConnectionFactory>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();