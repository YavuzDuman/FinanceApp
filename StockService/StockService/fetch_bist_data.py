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
    'TCELL.IS', 'TGES.IS', 'THYAO.IS', 'TKFEN.IS', 'TOASO.IS', 'TRCAS.IS',
    'TSKB.IS', 'TTKOM.IS', 'TTRAK.IS', 'TUPRS.IS', 'ULKER.IS', 'VAKBN.IS',
    'VESBE.IS', 'VESTL.IS', 'YATAS.IS', 'YKBNK.IS', 'ZOREN.IS', 'AKFGY.IS',
    'ALGYO.IS', 'ANACM.IS', 'DOAS.IS', 'DOGUB.IS', 'ECILC.IS', 'ECZYT.IS',
    'EGEEN.IS', 'TGSAS.IS', 'THY.IS', 'TKNSA.IS', 'TSGYO.IS', 'TURSG.IS',
    'ULAS.IS', 'VAKIF.IS', 'VERUS.IS', 'YAPIK.IS'
]

def fetch_bist_data():
    """
    yfinance kütüphanesi ile BIST hisse senedi verilerini günlük (1d) olarak çeker.
    """
    
    stocks = []
    try:
        # yfinance'tan günlük veri çek
        data = yf.download(bist_tickers, period='1d', interval='1d')
        
        if data.empty:
            print("yfinance kütüphanesi ile veri çekilemedi. Piyasalar kapalı olabilir veya semboller hatalı olabilir.", file=sys.stderr)
            return stocks
        
        # Son güncel veri setini al
        latest_data = data.iloc[-1]

        current_utc_time = datetime.now(timezone.utc).isoformat()
        
        for ticker in data['Close'].columns:
            try:
                # Sadece geçerli verilere sahip hisseleri al
                if latest_data['Close'][ticker] and latest_data['Open'][ticker] and latest_data['Volume'][ticker]:
                    symbol_data = {
                        'Symbol': ticker.replace('.IS', ''),
                        'CompanyName': ticker.replace('.IS', ''), 
                        'CurrentPrice': float(latest_data['Close'][ticker]),
                        'Change': float(latest_data['Close'][ticker] - float(latest_data['Open'][ticker])),
                        'ChangePercent': (float(latest_data['Close'][ticker]) - float(latest_data['Open'][ticker])) / float(latest_data['Open'][ticker]) * 100 if float(latest_data['Open'][ticker]) != 0 else 0,
                        'OpenPrice': float(latest_data['Open'][ticker]),
                        'HighPrice': float(latest_data['High'][ticker]),
                        'LowPrice': float(latest_data['Low'][ticker]),
                        'Volume': int(latest_data['Volume'][ticker]),
                        'LastUpdate': current_utc_time,
                    }
                    stocks.append(symbol_data)
                else:
                    print(f"Hatalı veri: {ticker} için geçersiz değerler bulundu. Atlanıyor.", file=sys.stderr)
            except Exception as e:
                print(f"Veri işleme hatası: {ticker} için - {e}", file=sys.stderr)
                pass

    except Exception as e:
        print(f"Genel Python script hatası: {e}", file=sys.stderr)
        
    return stocks

if __name__ == '__main__':
    stocks = fetch_bist_data()
    print(json.dumps(stocks))
