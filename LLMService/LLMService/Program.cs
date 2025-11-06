using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using Shared.Extensions;
using FluentValidation.AspNetCore;
using Microsoft.SemanticKernel;
using LLMService.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

builder.AddCentralizedLogging();

var apiKey = builder.Configuration["OpenRouter:ApiKey"]!;
var modelName = builder.Configuration["OpenRouter:ModelName"]!;
var baseUrl = "https://openrouter.ai/api/v1";

// Kernel Kaydı:
builder.Services.AddSingleton<Kernel>(sp =>
{
	// OpenRouter, OpenAI uyumlu olduğu için AddOpenAIChatCompletion kullanılır.
	var kernel = Kernel.CreateBuilder()
		// HttpClient'ı burada oluşturmak yerine, Connector'a Base URL'i doğrudan veriyoruz
		// OpenRouter için BaseUrl'i direkt AddOpenAIChatCompletion metoduna parametre olarak verin.
		.AddOpenAIChatCompletion(
			modelId: modelName,
			apiKey: apiKey,
			serviceId: modelName, // serviceId olarak model adını kullanabiliriz
								  // Yeni Semantic Kernel'de Base URL'i bu şekilde geçmek gerekiyor
			endpoint: new Uri(baseUrl)
		)
		.Build();

	return kernel;
});

builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowFrontend",
		builder =>
		{
			builder.WithOrigins(
				"http://localhost:5173",
				"https://localhost:5173",
				"https://localhost:5000",
				"http://localhost:5000",
				"https://localhost:7077",
				"http://localhost:7211"
			)
			.AllowAnyHeader()
			.AllowAnyMethod()
			.AllowCredentials();
		});
});

builder.Services.AddMemoryCache();

// 3. SignalR Servisi Kaydı
builder.Services.AddSignalR();


builder.Services.AddControllers()
	.AddFluentValidation(fv =>
	{
		// Validat�rleri otomatik olarak bul ve kaydet
		fv.RegisterValidatorsFromAssemblyContaining<Program>();

		fv.DisableDataAnnotationsValidation = false;
	});
// IHttpClientFactory'i ekle. Bu, HttpClient nesnelerini yönetmenin en iyi yoludur.
builder.Services.AddHttpClient();

// HttpContextAccessor ekle (Plugin'ler için gerekli)
builder.Services.AddHttpContextAccessor();

// -- API ve Swagger Servisleri --
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Merkezi JWT Doğrulama
builder.Services.AddCentralizedJwt(builder.Configuration);

builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
	// *** NİHAİ DÜZELTME BAŞLANGIÇ ***
	// Önce Events nesnesinin kendisinin var olup olmadığını kontrol et
	if (options.Events == null)
	{
		options.Events = new JwtBearerEvents();
	}
	// *** NİHAİ DÜZELTME BİTİŞ ***

	// Şimdi Events nesnesi oluşturuldu, güvenle erişebiliriz
	var existingOnMessageReceived = options.Events.OnMessageReceived;

	options.Events.OnMessageReceived = async context =>
	{
		// 1. Önce merkezi JwtExtensions'tan gelen olayı çalıştır (varsa)
		if (existingOnMessageReceived != null)
		{
			await existingOnMessageReceived(context);
		}

		// 2. SignalR'a özel mantığı ekle
		var accessToken = context.Request.Query["access_token"];
		var path = context.HttpContext.Request.Path;

		// Sadece /chatHub yolunda ve query string'de token yoksa (header'da yoksa) al
		if (string.IsNullOrEmpty(context.Token) &&
			!string.IsNullOrEmpty(accessToken) &&
			path.StartsWithSegments("/chatHub"))
		{
			context.Token = accessToken;
		}
	};
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

// Merkezi middleware'leri ekle
app.UseCentralizedMiddleware();

// Routing'i ekle - CORS'tan önce olmalı
app.UseRouting();

// CORS'u routing'den sonra ekle
app.UseCors("AllowFrontend");

app.Use(async (context, next) =>
{
	context.Response.Headers.Append("Content-Security-Policy",
		"default-src 'self'; " +
		"script-src 'self' 'unsafe-inline'; " +
		"connect-src 'self' ws://localhost:7211 wss://localhost:7211 ws://localhost:5000 wss://localhost:5000 http://localhost:5173 https://localhost:5173;");

	await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/chatHub");

app.Run();