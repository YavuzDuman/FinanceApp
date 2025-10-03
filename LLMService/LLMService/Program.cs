using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using LLMService.Service;
using Shared.Extensions;
using FluentValidation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddCentralizedLogging();
// Add services to the container.

// -- AI Servisleri --


builder.Services.AddControllers()
	.AddFluentValidation(fv =>
	{
		// Validat�rleri otomatik olarak bul ve kaydet
		fv.RegisterValidatorsFromAssemblyContaining<Program>();

		fv.DisableDataAnnotationsValidation = false;
	});
// IHttpClientFactory'i ekle. Bu, HttpClient nesnelerini yönetmenin en iyi yoludur.
builder.Services.AddHttpClient();

// -- API ve Swagger Servisleri --
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Merkezi JWT Doğrulama
builder.Services.AddCentralizedJwt(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCentralizedMiddleware();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();