using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using LLMService.Service;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// -- AI Servisleri --
// IHttpClientFactory'i ekle. Bu, HttpClient nesnelerini yönetmenin en iyi yoludur.
builder.Services.AddHttpClient();

// -- API ve Swagger Servisleri --
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

app.UseAuthorization();

app.MapControllers();

app.Run();