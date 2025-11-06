using StockService.Business.Abstract;
using StockService.Entities.Concrete;
using System.Diagnostics;
using System.Text.Json;
using System.Text;

namespace StockService.Business.Concrete
{
	public class ExternalApiService : IExternalApiService
	{
		public async Task<List<Stock>> FetchBistStocksAsync()
		{
			var stocks = new List<Stock>();
			var pythonScriptPath = "fetch_bist_data.py";

			if (!File.Exists(pythonScriptPath))
			{
				Console.Error.WriteLine($"Python scripti bulunamadı: {pythonScriptPath}");
				return stocks;
			}

			var startInfo = new ProcessStartInfo
			{
				FileName = "python3",
				Arguments = pythonScriptPath,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
				StandardOutputEncoding = Encoding.UTF8
			};

			using (var process = new Process { StartInfo = startInfo })
			{
				try
				{
					process.Start();

					// Python çıktısını asenkron olarak oku
					var outputTask = process.StandardOutput.ReadToEndAsync();
					var errorTask = process.StandardError.ReadToEndAsync();

					// 90 saniyelik bir zaman aşımı belirle (0.1 CPU'da işlemler daha yavaş olabilir)
					var timeoutTask = Task.Delay(TimeSpan.FromSeconds(90));

					// İşlem, çıktı okuma veya zaman aşımı görevlerinden hangisi önce biterse
					var completedTask = await Task.WhenAny(outputTask, errorTask, timeoutTask);

					if (completedTask == timeoutTask)
					{
						// Zaman aşımı durumunda işlemi sonlandır
						process.Kill(true);
						Console.Error.WriteLine("Python scripti zaman aşımına uğradı ve sonlandırıldı.");
						return stocks;
					}

					// İşlem çıktı verdiyse
					await Task.WhenAll(outputTask, errorTask);

					if (process.ExitCode == 0)
					{
						var jsonOutput = outputTask.Result;
						if (!string.IsNullOrEmpty(jsonOutput))
						{
							stocks = JsonSerializer.Deserialize<List<Stock>>(jsonOutput);
						}
					}
					else
					{
						Console.Error.WriteLine($"Python scripti hata verdi: {errorTask.Result}");
					}
				}
				catch (Exception ex)
				{
					Console.Error.WriteLine($"Python işlemi başlatılırken bir hata oluştu: {ex.Message}");
				}
				finally
				{
					if (!process.HasExited)
					{
						process.Kill(true);
					}
				}
			}

			return stocks;
		}

	public async Task<List<StockHistoricalData>> FetchHistoricalDataAsync(string symbol, string period, string interval)
	{
		var historicalData = new List<StockHistoricalData>();
		var pythonScriptPath = "fetch_bist_data.py";

		Console.WriteLine($"[TARIHSEL VERI] Symbol: {symbol}, Period: {period}, Interval: {interval}");
		Console.WriteLine($"[TARIHSEL VERI] Script path: {Path.GetFullPath(pythonScriptPath)}");

		if (!File.Exists(pythonScriptPath))
		{
			Console.Error.WriteLine($"[HATA] Python script bulunamadı: {pythonScriptPath}");
			return historicalData;
		}

			var startInfo = new ProcessStartInfo
			{
				FileName = "python3",
				Arguments = $"{pythonScriptPath} historical {symbol} {period} {interval}",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
				StandardOutputEncoding = Encoding.UTF8
			};

			using (var process = new Process { StartInfo = startInfo })
			{
				try
				{
					process.Start();

					var outputTask = process.StandardOutput.ReadToEndAsync();
					var errorTask = process.StandardError.ReadToEndAsync();
					var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30));

					var completedTask = await Task.WhenAny(outputTask, errorTask, timeoutTask);

					if (completedTask == timeoutTask)
					{
						process.Kill(true);
						Console.Error.WriteLine("Tarihsel veri timeout.");
						return historicalData;
					}

				await Task.WhenAll(outputTask, errorTask);

				var jsonOutput = outputTask.Result;
				var errorOutput = errorTask.Result;

				if (process.ExitCode == 0)
				{
					if (!string.IsNullOrEmpty(jsonOutput))
					{
						var options = new JsonSerializerOptions
						{
							PropertyNameCaseInsensitive = true
						};
						historicalData = JsonSerializer.Deserialize<List<StockHistoricalData>>(jsonOutput, options);
					}
				}
				else if (!string.IsNullOrEmpty(errorOutput))
				{
					Console.Error.WriteLine($"[HATA] Python exit code: {process.ExitCode}, Error: {errorOutput.Substring(0, Math.Min(200, errorOutput.Length))}");
				}
				}
				catch (Exception ex)
				{
					Console.Error.WriteLine($"İşlem hatası: {ex.Message}");
				}
				finally
				{
					if (!process.HasExited)
					{
						process.Kill(true);
					}
				}
			}

			return historicalData;
		}
	}
}