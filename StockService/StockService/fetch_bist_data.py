import yfinance as yf
import json
import sys
from datetime import datetime, timedelta, timezone

# yfinance tarafından desteklenen BIST 100 sembollerinin güncel listesi.
bist_tickers = [
    'AEFES.IS', 'AKBNK.IS', 'AKSA.IS', 'AKSEN.IS', 'ALARK.IS', 'ALBRK.IS',
    'ARCLK.IS', 'ASELS.IS', 'AYGAZ.IS', 'BIMAS.IS', 'BRSAN.IS', 'CCOLA.IS',
    'DEVA.IS', 'DOHOL.IS', 'EKGYO.IS', 'ENJSA.IS', 'EREGL.IS', 'FROTO.IS',
    'GARAN.IS', 'GESAN.IS', 'GUBRF.IS', 'HALKB.IS', 'HEKTS.IS', 'ISCTR.IS',
    'ISFIN.IS', 'IZMDC.IS', 'KCHOL.IS', 'KMPUR.IS', 'KOZAL.IS', 'KRDMD.IS',
    'MAVI.IS', 'MGROS.IS', 'MPARK.IS', 'ODAS.IS', 'ORGE.IS', 'PETKM.IS',
    'PGSUS.IS', 'SAHOL.IS', 'SASA.IS', 'SAYAS.IS', 'SEKFK.IS', 'SELEC.IS',
    'SISE.IS', 'SKBNK.IS', 'SOKM.IS', 'SUWEN.IS', 'TATIL.IS', 'TAVHL.IS',
    'TCELL.IS', 'THYAO.IS', 'TKFEN.IS', 'TOASO.IS', 'TRCAS.IS',
    'TSKB.IS', 'TTKOM.IS', 'TTRAK.IS', 'TUPRS.IS', 'ULKER.IS', 'VAKBN.IS',
    'VESBE.IS', 'VESTL.IS', 'YATAS.IS', 'YKBNK.IS', 'ZOREN.IS', 'AKFGY.IS',
    'ALGYO.IS', 'ANACM.IS', 'DOAS.IS', 'DOGUB.IS', 'ECILC.IS', 'ECZYT.IS',
    'EGEEN.IS', 'TGSAS.IS', 'TKNSA.IS', 'TSGYO.IS', 'TURSG.IS',
    'ULAS.IS', 'VAKIF.IS', 'VERUS.IS', 'YAPIK.IS','KBORU.IS'
]

def fetch_bist_data():
    """
    yfinance kütüphanesi ile BIST hisse senedi verilerini günlük (1d) olarak çeker.
    """
    
    stocks = []
    try:
        # Son 3 günlük veriyi toplu çek (hafta sonu için güvenlik payı)
        # 3 takvim günü = en az 2 işlem günü garantisi
        # Bu yaklaşım çok daha hızlı (tek seferde tüm hisseler)
        data = yf.download(bist_tickers, period='3d', interval='1d', group_by='ticker', threads=True)
        
        if data.empty:
            print("yfinance kütüphanesi ile veri çekilemedi. Piyasalar kapalı olabilir.", file=sys.stderr)
            return stocks
        
        # Tek hisse varsa farklı format geliyor, düzelt
        if len(bist_tickers) == 1:
            data = {bist_tickers[0]: data}
        
        current_utc_time = datetime.now(timezone.utc).isoformat()
        
        for ticker_symbol in bist_tickers:
            try:
                # Her hissenin verisini al
                ticker_data = data[ticker_symbol]
                
                # Boş veri kontrolü
                if ticker_data.empty or len(ticker_data) < 1:
                    print(f"Veri yok: {ticker_symbol}", file=sys.stderr)
                    continue
                
                # Son gün verisi (bugün)
                latest = ticker_data.iloc[-1]
                
                # Önceki gün kapanışını bul (previousClose)
                previous_close = None
                if len(ticker_data) >= 2:
                    previous_close = float(ticker_data.iloc[-2]['Close'])
                
                current_price = float(latest['Close'])
                
                # previousClose varsa ona göre, yoksa Open'a göre hesapla
                if previous_close and previous_close > 0:
                    change = current_price - previous_close
                    change_percent = (change / previous_close * 100)
                else:
                    open_price = float(latest['Open'])
                    change = current_price - open_price
                    change_percent = (change / open_price * 100) if open_price > 0 else 0
                
                symbol_data = {
                    'Symbol': ticker_symbol.replace('.IS', ''),
                    'CompanyName': ticker_symbol.replace('.IS', ''),
                    'CurrentPrice': current_price,
                    'Change': change,
                    'ChangePercent': change_percent,
                    'OpenPrice': float(latest['Open']),
                    'HighPrice': float(latest['High']),
                    'LowPrice': float(latest['Low']),
                    'Volume': int(latest['Volume']),
                    'LastUpdate': current_utc_time,
                }
                stocks.append(symbol_data)
                
            except Exception as e:
                print(f"Veri işleme hatası: {ticker_symbol} için - {e}", file=sys.stderr)
                continue

    except Exception as e:
        print(f"Genel Python script hatası: {e}", file=sys.stderr)
        
    return stocks

def fetch_historical_data(symbol, period='1mo', interval='1d'):
    """
    Belirtilen hisse için tarihsel veri çeker.
    
    Args:
        symbol: 'AKSA', 'THYAO' gibi
        period: '1d', '5d', '1mo', '3mo', '6mo', '1y'
        interval: '15m', '30m', '1h', '4h', '1d', '1wk'
    """
    try:
        # .IS ekle
        if not symbol.endswith('.IS'):
            symbol = f"{symbol}.IS"
        
        ticker = yf.Ticker(symbol)
        hist = ticker.history(period=period, interval=interval)
        
        if hist.empty:
            print(f"Veri yok: {symbol}", file=sys.stderr)
            return []
        
        data = []
        for date, row in hist.iterrows():
            data.append({
                'date': date.isoformat(),
                'open': float(row['Open']),
                'high': float(row['High']),
                'low': float(row['Low']),
                'close': float(row['Close']),
                'volume': int(row['Volume'])
            })
        
        return data
    except Exception as e:
        print(f"Hata ({symbol}): {e}", file=sys.stderr)
        return []

if __name__ == '__main__':
    if len(sys.argv) > 1 and sys.argv[1] == 'historical':
        # python fetch_bist_data.py historical AKSA 1mo 1d
        symbol = sys.argv[2] if len(sys.argv) > 2 else 'THYAO'
        period = sys.argv[3] if len(sys.argv) > 3 else '1mo'
        interval = sys.argv[4] if len(sys.argv) > 4 else '1d'
        result = fetch_historical_data(symbol, period, interval)
        print(json.dumps(result))
    else:
        stocks = fetch_bist_data()
        print(json.dumps(stocks))
