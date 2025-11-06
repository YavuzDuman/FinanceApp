namespace StockService.DataAccess.Redis
{
	public interface IRedisCacheService
	{
		Task<string> GetStringAsync(string key); 
		Task<bool> SetStringAsync(string key, string value); 
		Task<bool> SetStringAsync(string key, string value, TimeSpan expiration); 
		Task Clear(string key);
		void ClearAll();
	}
}
