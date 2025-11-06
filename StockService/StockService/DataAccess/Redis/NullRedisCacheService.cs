namespace StockService.DataAccess.Redis
{
	/// <summary>
	/// Redis yapılandırması yoksa veya bağlantı kurulamazsa kullanılan null object pattern implementasyonu
	/// </summary>
	public class NullRedisCacheService : IRedisCacheService
	{
		public Task Clear(string key)
		{
			// Redis yoksa hiçbir şey yapma
			return Task.CompletedTask;
		}

		public void ClearAll()
		{
			// Redis yoksa hiçbir şey yapma
		}

		public Task<string> GetValueAsync(string key)
		{
			// Redis yoksa boş string dön (cache miss)
			return Task.FromResult<string>(string.Empty);
		}

		public Task<bool> SetValueAsync(string key, string value)
		{
			// Redis yoksa false dön (cache'e yazılamadı)
			return Task.FromResult(false);
		}

		public Task<bool> SetValueAsync(string key, string value, TimeSpan expiration)
		{
			// Redis yoksa false dön (cache'e yazılamadı)
			return Task.FromResult(false);
		}
	}
}

