namespace Shared.Contracts
{
    // Event: Bir hissenin güncel fiyatı değiştiğinde yayınlanır
    public record StockPriceUpdated
    (
        string Symbol,
        decimal CurrentPrice,
        DateTime OccurredAtUtc
    );
}


