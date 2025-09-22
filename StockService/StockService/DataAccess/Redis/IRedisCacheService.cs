namespace StockService.DataAccess.Redis
{
	public interface IRedisCacheService
	{
		Task<string> GetValueAsync(string key);
		Task<bool> SetValueAsync(string key, string value);
		Task<bool> SetValueAsync(string key, string value, TimeSpan expiration);
		Task Clear(string key);
		void ClearAll();
	}
}
