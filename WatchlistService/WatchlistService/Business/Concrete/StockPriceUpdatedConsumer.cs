using MassTransit;
using Shared.Contracts;
using WatchlistService.DataAccess.Abstract;

namespace WatchlistService.Business.Concrete
{
    // Consumer: Stock fiyatı güncellendiğinde Portföy item'larının CurrentPrice'ını günceller
    public class StockPriceUpdatedConsumer : IConsumer<StockPriceUpdated>
    {
        private readonly IWatchlistRepository _watchlistRepository;

        public StockPriceUpdatedConsumer(IWatchlistRepository watchlistRepository)
        {
			_watchlistRepository = watchlistRepository;
        }

        public async Task Consume(ConsumeContext<StockPriceUpdated> context)
        {
            var message = context.Message;
            // Repository'ye sembole göre tüm item'lar için CurrentPrice update metodu ekleyip burada çağıracağız
            await _watchlistRepository.UpdateCurrentPriceBySymbolAsync(message.Symbol, message.CurrentPrice);
        }
    }
}


