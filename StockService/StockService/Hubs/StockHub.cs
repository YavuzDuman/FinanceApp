using Microsoft.AspNetCore.SignalR;

namespace StockService.Hubs
{
	public class StockHub : Hub
	{
		// İstemciler bu metodu çağırarak sunucuya mesaj gönderebilir.
		// Bizim senaryomuzda bu metodu doğrudan kullanmayacağız,
		// ancak Hub'ın nasıl çalıştığını göstermek için burada duruyor.
		public async Task SendMessage(string user, string message)
		{
			// Bağlı tüm istemcilere mesaj gönderir.
			// Bu metod, genellikle istemciden gelen bir olaya tepki vermek için kullanılır.
			await Clients.All.SendAsync("ReceiveMessage", user, message);
		}
	}
}
