using FinancialStatementService.Business.Abstract;
using FinancialStatementService.DataAccess.Abstract;
using FinancialStatementService.Entities;
using Microsoft.Playwright;
using HtmlAgilityPack;

public class FinancialStatementManager : IFinancialStatementManager
{
	private readonly IFinancialStatementRepository _repository;
	private readonly AutoMapper.IMapper _mapper;

	public FinancialStatementManager(IFinancialStatementRepository repository, AutoMapper.IMapper mapper)
	{
		_repository = repository;
		_mapper = mapper;
	}

	public async Task<FinancialStatementDto?> GetSymbolByNameAsync(string symbol)
	{
		var statement = await _repository.GetSymbolByNameAsync(symbol);
		return _mapper.Map<FinancialStatementDto>(statement);
	}

	public async Task<List<FinancialStatementDto>> GetAllSymbolsAsync()
	{
		var statements = await _repository.GetAllSymbolsAsync();
		return _mapper.Map<List<FinancialStatementDto>>(statements);
	}

	public async Task<List<FinancialStatement>> FetchAndSaveFinancialStatementsAsync()
	{
		// Her işlem öncesi tabloyu temizle
		await _repository.DeleteAllAsync();
		System.Console.WriteLine("Tüm eski veriler silindi, yeni veriler ekleniyor...");
		
		var statements = new List<FinancialStatement>();

		using var playwright = await Playwright.CreateAsync();
		await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
		{
			Headless = true,
			Args = new[] { "--disable-blink-features=AutomationControlled" }
		});
		var context = await browser.NewContextAsync(new BrowserNewContextOptions
		{
			UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
			Locale = "tr-TR",
			TimezoneId = "Europe/Istanbul",
			ViewportSize = new ViewportSize { Width = 1366, Height = 900 }
		});
		var page = await context.NewPageAsync();
		await page.GotoAsync("https://www.getborsa.com/arastirma/son-bilancolar/index.html", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 90000 });

		// Olası çerez/banner butonlarını dene (hata yutulur)
		try { await page.Locator("button:has-text('Kabul')").First.ClickAsync(new LocatorClickOptions { Timeout = 2000 }); } catch { }
		try { await page.Locator("button:has-text('Kabul Et')").First.ClickAsync(new LocatorClickOptions { Timeout = 2000 }); } catch { }
		try { await page.Locator("button:has-text('Accept')").First.ClickAsync(new LocatorClickOptions { Timeout = 2000 }); } catch { }
		try { await page.Locator("text=Tümünü kabul et").First.ClickAsync(new LocatorClickOptions { Timeout = 2000 }); } catch { }

		await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await page.WaitForSelectorAsync("table tr");

		// HtmlAgilityPack ile parse et (daha güvenilir)
		var pageSource = await page.ContentAsync();
		
		// Debug için HTML'i logla
		System.Console.WriteLine("=== PAGE HTML DEBUG ===");
		System.Console.WriteLine($"Page source length: {pageSource.Length}");
		
		// Tablo elementlerini ara
		var htmlDoc = new HtmlDocument();
		htmlDoc.LoadHtml(pageSource);
		var allTables = htmlDoc.DocumentNode.SelectNodes("//table");
		System.Console.WriteLine($"Found {allTables?.Count ?? 0} table elements");
		
		if (allTables != null)
		{
			for (int i = 0; i < allTables.Count; i++)
			{
				var table = allTables[i];
				System.Console.WriteLine($"Table {i}: class='{table.GetAttributeValue("class", "none")}', rows={table.SelectNodes(".//tr")?.Count ?? 0}");
			}
		}
		
		var tableNode = htmlDoc.DocumentNode.SelectSingleNode("//table");
		System.Console.WriteLine($"Found main table: {tableNode != null}");
		
		if (tableNode != null)
		{
			var rows = tableNode.SelectNodes(".//tr");
			System.Console.WriteLine($"Found {rows?.Count ?? 0} rows in main table");
			
			if (rows != null)
			{
				foreach (var row in rows)
				{
					var cells = row.SelectNodes(".//td");
					System.Console.WriteLine($"Row has {cells?.Count ?? 0} cells");
					
					if (cells != null && cells.Count > 0)
					{
						// Debug: her hücrenin içeriğini yazdır
						for (int i = 0; i < Math.Min(cells.Count, 8); i++)
						{
							var cellText = cells[i].InnerText.Trim();
							System.Console.WriteLine($"Cell {i}: '{cellText}'");
						}
						
						// GetBorsa.com tablo yapısı: Hisse | Tarih | Net Dönem Karı | Yıllık Net Kar Değişimi | Çeyreklik Net Kar
					var stockSymbolFull = cells[0]?.InnerText.Trim(); // Hisse + Şirket Adı
					
					// GetBorsa.com'da sembol ve şirket adı ayrı span'larda: <span>MAVI</span><span>Mavi Giyim...</span>
					var stockSymbol = "";
					var companyName = "";
					
					if (cells[0] != null)
					{
						// İlk span elementini bul (sembol kısmı)
						var firstSpan = cells[0].SelectSingleNode(".//span[1]");
						if (firstSpan != null)
						{
							stockSymbol = firstSpan.InnerText.Trim();
						}
						
						// İkinci span elementini bul (şirket ismi)
						var secondSpan = cells[0].SelectSingleNode(".//span[2]");
						if (secondSpan != null)
						{
							companyName = secondSpan.InnerText.Trim();
						}
						
						// Fallback: Eğer span bulunamazsa eski mantık
						if (string.IsNullOrEmpty(stockSymbol))
						{
							var firstWord = stockSymbolFull.Split(' ')[0];
							var upperCasePart = "";
							foreach (char c in firstWord)
							{
								if (char.IsUpper(c))
									upperCasePart += c;
								else
									break;
							}
							stockSymbol = upperCasePart.Length >= 3 ? upperCasePart : firstWord;
						}
						
						// Fallback: Şirket ismi için
						if (string.IsNullOrEmpty(companyName))
						{
							companyName = stockSymbolFull; // Tam metin
						}
					}
					
					System.Console.WriteLine($"Parsing - Symbol: '{stockSymbol}', Company: '{companyName}', Full: '{stockSymbolFull}'");
						var announcementDateText = cells[1]?.InnerText.Trim(); // Tarih
						var periodNetProfitText = cells[2]?.InnerText.Trim(); // Net Dönem Karı
						var annualNetProfitChangeText = cells[3]?.InnerText.Trim(); // Yıllık Net Kar Değişimi
						var quarterlyNetProfitText = cells[4]?.InnerText.Trim(); // Çeyreklik Net Kar
						
						System.Console.WriteLine($"Parsed - SymbolFull: '{stockSymbolFull}', Symbol: '{stockSymbol}', Announcement: '{announcementDateText}', PeriodNetProfit: '{periodNetProfitText}', AnnualChange: '{annualNetProfitChangeText}', QuarterlyNetProfit: '{quarterlyNetProfitText}'");

						var statementDate = DateTime.UtcNow.Date;
						var announcementDate = (DateTime?)null;
						var netProfitChangeRate = (decimal?)null;

						// Açıklanma tarihi parse et ("17 Eylül" -> DateTime)
						if (!string.IsNullOrEmpty(announcementDateText))
						{
							var announcementStr = announcementDateText.Trim();
							// Türkçe ay isimlerini İngilizce'ye çevir
							announcementStr = announcementStr.Replace("Ocak", "January")
								.Replace("Şubat", "February")
								.Replace("Mart", "March")
								.Replace("Nisan", "April")
								.Replace("Mayıs", "May")
								.Replace("Haziran", "June")
								.Replace("Temmuz", "July")
								.Replace("Ağustos", "August")
								.Replace("Eylül", "September")
								.Replace("Ekim", "October")
								.Replace("Kasım", "November")
								.Replace("Aralık", "December");
							
							if (DateTime.TryParseExact(announcementStr, "d MMMM", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var parsedAnnouncement))
							{
								announcementDate = new DateTime(DateTime.Now.Year, parsedAnnouncement.Month, parsedAnnouncement.Day);
							}
						}

						// GetBorsa.com'da fiyat bilgisi yok, null bırakıyoruz

						// Yıllık net kar değişimi oranı parse et
						if (!string.IsNullOrEmpty(annualNetProfitChangeText))
						{
							var changeStr = annualNetProfitChangeText.Replace("%", "").Replace(",", ".").Trim();
							if (decimal.TryParse(changeStr, out var parsedChange))
							{
								netProfitChangeRate = parsedChange;
							}
						}

						if (!string.IsNullOrWhiteSpace(stockSymbol))
						{
							var statement = new FinancialStatement
							{
								StockSymbol = stockSymbol,
								CompanyName = companyName,
								StatementDate = statementDate,
								Type = "Quarterly",
								Data = quarterlyNetProfitText, // Çeyreklik Net Kar
								AnnouncementDate = announcementDate,
								NetProfitChangeRate = netProfitChangeRate,
								UpdatedDate = DateTime.Now
							};
							statements.Add(statement);
						}
					}
				}
			}
		}

		foreach (var statement in statements)
		{
			await _repository.InsertAsync(statement);
			System.Console.WriteLine($"Inserted new record: {statement.StockSymbol} - {statement.StatementDate:yyyy-MM-dd}");
		}
		return statements;
	}

	
}