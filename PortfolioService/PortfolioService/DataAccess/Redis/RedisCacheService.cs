using StackExchange.Redis;
using System;
using System.Threading.Tasks;

// Namespace'i, PortfolioService projenizden referans alabilmek için Shared bir yere taşıyabilirsiniz
// Örneğin: Infrastructure.Redis veya Shared.Redis
namespace StockService.DataAccess.Redis
{
	public class RedisCacheService : IRedisCacheService
	{
		private readonly IConnectionMultiplexer _redisConnection;
		private readonly StackExchange.Redis.IDatabase _cache;
		// Varsayılan süre, süresiz Set çağrıldığında kullanılır
		private readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(1);

		public RedisCacheService(IConnectionMultiplexer redisConnection)
		{
			_redisConnection = redisConnection;
			// RedisConnection'ı alırken GetDatabase çağrısı yapabilirsiniz, Thread-Safe'dir.
			_cache = redisConnection.GetDatabase();
		}

		public async Task Clear(string key)
		{
			// KeyDeleteAsync, key yoksa bile hata vermez.
			await _cache.KeyDeleteAsync(key);
		}

		[Obsolete("UYARI: Bu metot tüm veritabanlarındaki tüm veriyi siler. Üretimde KULLANILMAMALIDIR!")]
		public void ClearAll()
		{
			var redisEndpoints = _redisConnection.GetEndPoints(true);
			foreach (var redisEndpoint in redisEndpoints)
			{
				var redisServer = _redisConnection.GetServer(redisEndpoint);
				// DİKKAT: flushAllDatabases() tüm veritabanlarındaki veriyi siler.
				redisServer.FlushAllDatabases();
			}
		}

		// GetValueAsync -> GetStringAsync
		public async Task<string> GetStringAsync(string key)
		{
			var result = await _cache.StringGetAsync(key);
			// RedisValue implicit olarak string'e döner.
			return result.ToString();
		}

		// SetValueAsync -> SetStringAsync (Varsayılan süreyi kullanır)
		public async Task<bool> SetStringAsync(string key, string value)
		{
			return await _cache.StringSetAsync(key, value, DefaultExpiration);
		}

		// SetValueAsync -> SetStringAsync (Belirlenen süreyi kullanır)
		public async Task<bool> SetStringAsync(string key, string value, TimeSpan expiration)
		{
			return await _cache.StringSetAsync(key, value, expiration);
		}
	}
}