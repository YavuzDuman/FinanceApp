using AutoMapper;
using FinancialNewsService.Business.Abstract;
using FinancialNewsService.DataAccess.Abstract;
using FinancialNewsService.Entities;
using Microsoft.Playwright;
using HtmlAgilityPack;

namespace FinancialNewsService.Business.Concrete
{
	public class FinancialNewsManager : IFinancialNewsManager
	{
		private readonly IFinancialNewsRepository _repository;
		private readonly IMapper _mapper;

		public FinancialNewsManager(IFinancialNewsRepository repository, IMapper mapper)
		{
			_repository = repository;
			_mapper = mapper;
		}

		public async Task<List<FinancialNewsDto>> GetAllNewsAsync()
		{
			var news = await _repository.GetAllAsync();
			return _mapper.Map<List<FinancialNewsDto>>(news);
		}

		public async Task<FinancialNewsDto?> GetNewsByIdAsync(int id)
		{
			var news = await _repository.GetByIdAsync(id);
			return _mapper.Map<FinancialNewsDto>(news);
		}

		public async Task<List<FinancialNewsDto>> FetchAndSaveFinancialNewsAsync()
		{
			// Her işlem öncesi tabloyu temizle
			await _repository.DeleteAllAsync();
			System.Console.WriteLine("Tüm eski haberler silindi, yeni haberler ekleniyor...");

			var newsList = new List<FinancialNewsDto>();

			try
			{
				using var playwright = await Playwright.CreateAsync();
				await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
				{
					Headless = false, // Anti-bot için görünür browser
					Args = new[] 
					{
						"--disable-blink-features=AutomationControlled",
						"--disable-features=VizDisplayCompositor",
						"--disable-web-security",
						"--disable-features=TranslateUI",
						"--disable-ipc-flooding-protection",
						"--no-sandbox",
						"--disable-setuid-sandbox"
					}
				});

				var contextOptions = new BrowserNewContextOptions
				{
					UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
					Locale = "tr-TR",
					TimezoneId = "Europe/Istanbul",
					ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
					// Anti-bot bypass için ek ayarlar
					JavaScriptEnabled = true,
					AcceptDownloads = true,
					HasTouch = false,
					IsMobile = false,
					DeviceScaleFactor = 1,
					// Ekstra headers
					ExtraHTTPHeaders = new Dictionary<string, string>
					{
						["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8",
						["Accept-Language"] = "tr-TR,tr;q=0.9,en;q=0.8",
						["Accept-Encoding"] = "gzip, deflate, br",
						["DNT"] = "1",
						["Connection"] = "keep-alive",
						["Upgrade-Insecure-Requests"] = "1"
					}
				};

				await using var context = await browser.NewContextAsync(contextOptions);
				var page = await context.NewPageAsync();

				System.Console.WriteLine("Investing.com finans haberleri sayfasına gidiliyor...");
				
				// Anti-bot için önce ana sayfaya git
				await page.GotoAsync("https://tr.investing.com", new PageGotoOptions { Timeout = 10000 });
				await page.WaitForTimeoutAsync(3000); // Ana sayfada bekle
				
				// Sonra haberler sayfasına git
				await page.GotoAsync("https://tr.investing.com/news/economic-indicators", new PageGotoOptions { Timeout = 10000 });
				await page.WaitForSelectorAsync("body", new PageWaitForSelectorOptions { Timeout = 10000 });
				await page.WaitForTimeoutAsync(5000); // Sayfa tam yüklensin

				// Cookie banner'ı kapatmaya çalış
				try
				{
					var cookieButton = await page.QuerySelectorAsync("button:has-text('Kabul Et')");
					if (cookieButton != null)
					{
						await cookieButton.ClickAsync();
						await page.WaitForTimeoutAsync(1000);
					}
				}
				catch (Exception ex)
				{
					System.Console.WriteLine($"Cookie banner kapatılamadı: {ex.Message}");
				}

				// HTML içeriğini al
				var htmlContent = await page.ContentAsync();
				System.Console.WriteLine($"Sayfa HTML içeriği alındı, uzunluk: {htmlContent.Length}");

				// HtmlAgilityPack ile parse et
				var doc = new HtmlDocument();
				doc.LoadHtml(htmlContent);

				// Ana sayfadaki haber linklerini bul
				var allLinks = doc.DocumentNode.SelectNodes("//a[@href]");
				System.Console.WriteLine($"Toplam link sayısı: {allLinks?.Count ?? 0}");

				// Sadece haber detay linklerini filtrele (ana sayfadaki haber başlıkları)
				var newsLinks = allLinks?.Where(link => 
				{
					var href = link.GetAttributeValue("href", "");
					var text = link.InnerText?.Trim() ?? "";
					
					// Haber detay linki olup olmadığını kontrol et
					return href.Contains("/news/") && 
						   href.Contains("/economic-indicators/") && // Sadece ekonomik göstergeler haberleri
						   !string.IsNullOrEmpty(text) && 
						   text.Length > 10 && 
						   text.Length < 200 &&
						   !href.Contains("/news/economic-indicators"); // Ana sayfa linkini hariç tut
				}).Take(5).ToList(); // Sadece 5 haber (test için)

				System.Console.WriteLine($"Filtrelenmiş haber detay link sayısı: {newsLinks?.Count ?? 0}");

				if (newsLinks != null)
				{
					foreach (var link in newsLinks)
					{
						try
						{
							string title = link.InnerText?.Trim() ?? "";
							string href = link.GetAttributeValue("href", "");
							
							// Link'i düzelt
							if (!string.IsNullOrEmpty(href) && !href.StartsWith("http"))
							{
								href = "https://tr.investing.com" + href;
							}

							System.Console.WriteLine($"Haber işleniyor - Başlık: '{title}', Link: '{href}'");

							if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(href))
							{
								// Her haber için detay sayfasına git ve içeriği çek
								System.Console.WriteLine($"Detay sayfasına gidiliyor: {href}");
								
								// Haber detayını çek
								var newsDetail = await FetchNewsDetailAsync(page, href);
								
								if (!string.IsNullOrEmpty(newsDetail.Content) && newsDetail.Content.Length > 50)
								{
									var news = new FinancialNews
									{
										Title = title,
										Content = newsDetail.Content,
										Summary = newsDetail.Content.Length > 200 ? newsDetail.Content.Substring(0, 200) + "..." : newsDetail.Content,
										Author = newsDetail.Author,
										PublishedDate = DateTime.Now,
										SourceUrl = href,
										Category = "Ekonomik Göstergeler",
										Tags = newsDetail.Tags,
										CreatedDate = DateTime.Now,
										UpdatedDate = DateTime.Now
									};

									// Duplicate kontrolü
									var existingNews = await _repository.GetByUrlAsync(href);
									if (existingNews == null)
									{
										await _repository.InsertAsync(news);
										newsList.Add(_mapper.Map<FinancialNewsDto>(news));
										System.Console.WriteLine($"✅ Haber başarıyla eklendi: {title}");
									}
									else
									{
										System.Console.WriteLine($"⚠️ Haber zaten mevcut: {title}");
									}
								}
								else
								{
									System.Console.WriteLine($"❌ Haber içeriği çekilemedi: {title}");
								}
								
								// Bir sonraki habere geçmeden önce ana sayfaya geri dön
								System.Console.WriteLine("Ana sayfaya geri dönülüyor...");
								await page.GotoAsync("https://tr.investing.com/news/economic-indicators", new PageGotoOptions { Timeout = 10000 });
								await page.WaitForTimeoutAsync(3000); // Ana sayfada bekle
							}
						}
						catch (Exception ex)
						{
							System.Console.WriteLine($"❌ Haber işlenirken hata: {ex.Message}");
						}
					}
				}
			}
			catch (Exception ex)
			{
				System.Console.WriteLine($"Haberler çekilirken hata: {ex.Message}");
			}

			System.Console.WriteLine($"Toplam {newsList.Count} haber başarıyla eklendi.");
			return newsList;
		}

		private async Task<(string Content, string Author, string Tags)> FetchNewsDetailAsync(IPage page, string url)
		{
			try
			{
				System.Console.WriteLine($"Haber detayı çekiliyor: {url}");
				
				// Anti-bot için daha yavaş ve doğal hareket
				await page.GotoAsync(url, new PageGotoOptions { Timeout = 15000 });
				await page.WaitForSelectorAsync("body", new PageWaitForSelectorOptions { Timeout = 10000 });
				await page.WaitForTimeoutAsync(5000); // Sayfa tam yüklensin
				
				// Anti-bot kontrolü
				var pageContent = await page.ContentAsync();
				if (pageContent.Contains("İnsan olduğunuz doğrulanıyor") || pageContent.Contains("verification"))
				{
					System.Console.WriteLine("Anti-bot koruması tespit edildi, bekleniyor...");
					await page.WaitForTimeoutAsync(10000); // 10 saniye bekle
					pageContent = await page.ContentAsync();
				}

				var htmlContent = await page.ContentAsync();
				var doc = new HtmlDocument();
				doc.LoadHtml(htmlContent);

				// Debug: Sayfa yapısını kontrol et
				System.Console.WriteLine($"Detay sayfası HTML uzunluğu: {htmlContent.Length}");

				// Farklı selector'ları dene
				var contentNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'article-content')]") ??
								 doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'article')]") ??
								 doc.DocumentNode.SelectSingleNode("//article") ??
								 doc.DocumentNode.SelectSingleNode("//main") ??
								 doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'content')]");

				var content = "";
				if (contentNode != null)
				{
					content = contentNode.InnerText?.Trim() ?? "";
					System.Console.WriteLine($"İçerik bulundu, uzunluk: {content.Length}");
				}
				else
				{
					System.Console.WriteLine("İçerik bulunamadı, tüm metni alıyor...");
					// Fallback: Tüm sayfa metnini al
					content = doc.DocumentNode.InnerText?.Trim() ?? "";
				}

				// İçerik çok uzunsa kısalt
				if (content.Length > 2000)
				{
					content = content.Substring(0, 2000) + "...";
				}

				// Yazar bilgisi
				var authorNode = doc.DocumentNode.SelectSingleNode("//span[contains(@class, 'author')]") ??
								 doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'author')]");
				var author = authorNode?.InnerText?.Trim() ?? "Investing.com";

				System.Console.WriteLine($"İçerik başarıyla çekildi: {content.Length} karakter");

				return (content, author, "");
			}
			catch (Exception ex)
			{
				System.Console.WriteLine($"Haber detayı çekilirken hata: {ex.Message}");
				return ("İçerik çekilemedi", "Investing.com", "");
			}
		}

		public async Task DeleteAllNewsAsync()
		{
			await _repository.DeleteAllAsync();
		}
	}
}
