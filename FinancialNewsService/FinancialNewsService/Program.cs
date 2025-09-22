using FinancialNewsService.Business.Abstract;
using FinancialNewsService.Business.Concrete;
using FinancialNewsService.DataAccess.Abstract;
using FinancialNewsService.DataAccess.Concrete;
using FinancialNewsService.DataAccess.DbConnectionFactory;
using FinancialNewsService.Helpers.Mapping;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Dependency Injection
builder.Services.AddScoped<IDbConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<IFinancialNewsRepository, FinancialNewsRepository>();
builder.Services.AddScoped<IFinancialNewsManager, FinancialNewsManager>();

// AutoMapper
builder.Services.AddAutoMapper(typeof(FinancialNewsMappingProfile));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
