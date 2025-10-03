using MassTransit;
using Shared.Contracts;
using PortfolioService.DataAccess.Abstract;

namespace PortfolioService.Business.Concrete
{
    // Consumer: Stock fiyatı güncellendiğinde Portföy item'larının CurrentPrice'ını günceller
    public class StockPriceUpdatedConsumer : IConsumer<StockPriceUpdated>
    {
        private readonly IPortfolioRepository _portfolioRepository;

        public StockPriceUpdatedConsumer(IPortfolioRepository portfolioRepository)
        {
            _portfolioRepository = portfolioRepository;
        }

        public async Task Consume(ConsumeContext<StockPriceUpdated> context)
        {
            var message = context.Message;
            // Repository'ye sembole göre tüm item'lar için CurrentPrice update metodu ekleyip burada çağıracağız
            await _portfolioRepository.UpdateCurrentPriceBySymbolAsync(message.Symbol, message.CurrentPrice);
        }
    }
}


