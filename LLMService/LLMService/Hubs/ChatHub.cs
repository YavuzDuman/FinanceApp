using Microsoft.AspNetCore.SignalR;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace LLMService.Hubs
{
	public class ChatHub : Hub
	{
		private readonly Kernel _kernel;
		private readonly IMemoryCache _cache;
		private readonly string _modelName;
		private readonly IServiceProvider _serviceProvider;

		// Key için sabit prefix
		private const string CacheKeyPrefix = "ChatHistory_";

		// 2. Constructor'ı Kernel sınıfını alacak şekilde değiştir
		public ChatHub(Kernel kernel, IMemoryCache cache, IConfiguration configuration, IServiceProvider serviceProvider)
		{
			_kernel = kernel;
			_cache = cache;
			_modelName = configuration["OpenRouter:ModelName"] ?? "default-model";
			_serviceProvider = serviceProvider;
		}

		// 1. Bağlantı kesildiğinde History'yi temizle
		public override Task OnDisconnectedAsync(Exception? exception)
		{
			var key = CacheKeyPrefix + Context.ConnectionId;
			_cache.Remove(key);

			// Stock chat history'lerini de temizle
			var stockKeyPrefix = $"{CacheKeyPrefix}Stock_{Context.ConnectionId}_";
			// Tüm stock key'lerini temizlemek için basit bir yaklaşım (geliştirilebilir)
			return base.OnDisconnectedAsync(exception);
		}

		// 2. İstemciden mesajı alacak ana metot
		public async Task SendMessage(string userPrompt)
		{
			var userIdFromHeader = Context.GetHttpContext()?.Request.Headers["X-User-ID"].FirstOrDefault();

			// Token'dan gelen Claim'i kontrol et.
			var userIdFromClaim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ??
								  Context.User?.FindFirstValue("sub");

			// Öncelik: Önce Ocelot'tan gelen başlık, sonra token claim'i
			var userId = userIdFromHeader ?? userIdFromClaim;

			var connectionId = Context.ConnectionId;

			var history = GetOrCreateHistory(connectionId);
			history.AddUserMessage(userPrompt);

			// Chat Completion Service'i Kernel'den doğru bir şekilde alıyoruz
			var chatService = _kernel.GetRequiredService<IChatCompletionService>();

			// İstemciye akışın başladığını bildir
			await Clients.Caller.SendAsync("ReceiveStreamStart");

			var fullResponse = "";
			try
			{
				// DÜZELTME: Metot adı artık 'GetStreamingChatMessageContentsAsync'
				// ve çağrıyı chatService değişkeni üzerinden yapıyoruz.
				await foreach (var content in chatService.GetStreamingChatMessageContentsAsync(history))
				{
					// Metin içeriğini doğru şekilde kontrol et ve gönder
					var messageChunk = content.Content ?? string.Empty;
					if (!string.IsNullOrEmpty(messageChunk))
					{
						await Clients.Caller.SendAsync("ReceiveMessageChunk", messageChunk);
						fullResponse += messageChunk;
					}
				}

				// Tamamlanmış yanıtı geçmişe kaydet
				history.AddAssistantMessage(fullResponse);
			}
			catch (Exception ex)
			{
				await Clients.Caller.SendAsync("ReceiveError", $"AI Yanıt Hatası: {ex.Message}");
			}
			finally
			{
				await Clients.Caller.SendAsync("ReceiveStreamEnd");
			}
		}

		// History alma/oluşturma metodu
		private ChatHistory GetOrCreateHistory(string connectionId)
		{
			var key = CacheKeyPrefix + connectionId;

			if (_cache.TryGetValue(key, out ChatHistory? history) && history != null)
			{
				return history;
			}

			// Yeni history oluştur
			history = new ChatHistory(
				// Sisteme rol ataması
				"Sen, Türkiye Finans Piyasaları konusunda uzman, yapay zeka destekli bir finans asistanısın. Kısa ve bilgilendirici yanıtlar ver. Sadece finansal konulara odaklan."
			);

			// History'yi Redis'e (veya IMemoryCache'e) 30 dakikalık bir süreyle kaydet
			_cache.Set(key, history, new MemoryCacheEntryOptions
			{
				AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
			});

			return history;
		}

		// 3. Hisse bazlı chat metodu
		public async Task SendStockMessage(string stockSymbol, string userPrompt)
		{
			var connectionId = Context.ConnectionId;

			// Plugin'ler kullanılmıyor - model kendi bilgisiyle cevap verecek
			var history = GetOrCreateStockHistory(connectionId, stockSymbol);
			history.AddUserMessage(userPrompt);

			// Chat Completion Service'i Kernel'den al
			var chatService = _kernel.GetRequiredService<IChatCompletionService>();

			// İstemciye akışın başladığını bildir
			await Clients.Caller.SendAsync("ReceiveStreamStart");

			var fullResponse = "";
			try
			{
				// Plugin/tool çağrıları devre dışı - model sadece kendi bilgisiyle cevap versin
				var settings = new OpenAIPromptExecutionSettings
				{
					Temperature = 0.7f,
					MaxTokens = 1000
				};

				await foreach (var content in chatService.GetStreamingChatMessageContentsAsync(
					history,
					settings))
				{
					var messageChunk = content.Content ?? string.Empty;
					if (!string.IsNullOrEmpty(messageChunk))
					{
						await Clients.Caller.SendAsync("ReceiveMessageChunk", messageChunk);
						fullResponse += messageChunk;
					}
				}

				// Tamamlanmış yanıtı geçmişe kaydet
				history.AddAssistantMessage(fullResponse);
			}
			catch (Exception ex)
			{
				await Clients.Caller.SendAsync("ReceiveError", $"AI Yanıt Hatası: {ex.Message}");
			}
			finally
			{
				await Clients.Caller.SendAsync("ReceiveStreamEnd");
			}
		}

		// Stock bazlı history oluşturma
		private ChatHistory GetOrCreateStockHistory(string connectionId, string stockSymbol)
		{
			var key = $"{CacheKeyPrefix}Stock_{connectionId}_{stockSymbol}";

			if (_cache.TryGetValue(key, out ChatHistory? history) && history != null)
			{
				return history;
			}

			// System prompt - model kendi bilgisiyle cevap versin
			var systemMessage = $@"Sen, Türkiye Finans Piyasaları konusunda uzman bir finans asistanısın. Kullanıcı şu anda {stockSymbol} hissesi hakkında sorular soruyor. 

ÖNEMLİ TALİMATLAR:
- Yanıtlarını kısa, öz ve bilgilendirici tut.
- Türkçe, profesyonel ve özlü bir şekilde cevap ver.
- Şirket kodunu analiz ederek şirket hakkında bilgi ver. Bilgiyi doğru bir şekilde ver.
- Emin olmadığın bilgiler için ""bilmiyorum"" veya ""bu konuda kesin bilgim yok"" gibi dürüst cevaplar ver.
";

			history = new ChatHistory(systemMessage);

			_cache.Set(key, history, new MemoryCacheEntryOptions
			{
				AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
			});

			return history;
		}

	}
}