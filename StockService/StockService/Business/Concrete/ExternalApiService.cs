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
				FileName = "python",
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

					// 60 saniyelik bir zaman aşımı belirle
					var timeoutTask = Task.Delay(TimeSpan.FromSeconds(60));

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
				FileName = "python",
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

				Console.WriteLine($"[PYTHON] Exit Code: {process.ExitCode}");
				Console.WriteLine($"[PYTHON] Output Length: {jsonOutput?.Length ?? 0}");
				if (!string.IsNullOrEmpty(errorOutput))
				{
					Console.WriteLine($"[PYTHON] Error Output: {errorOutput}");
				}

				if (process.ExitCode == 0)
				{
					if (!string.IsNullOrEmpty(jsonOutput))
					{
						Console.WriteLine($"[PYTHON] First 200 chars: {jsonOutput.Substring(0, Math.Min(200, jsonOutput.Length))}");
						
						var options = new JsonSerializerOptions
						{
							PropertyNameCaseInsensitive = true
						};
						historicalData = JsonSerializer.Deserialize<List<StockHistoricalData>>(jsonOutput, options);
						Console.WriteLine($"[DESERIALIZE] Success! Item count: {historicalData?.Count ?? 0}");
						
						if (historicalData != null && historicalData.Any())
						{
							var first = historicalData.First();
							Console.WriteLine($"[DESERIALIZE] First item - Date: {first.Date}, Close: {first.Close}");
						}
					}
					else
					{
						Console.WriteLine("[PYTHON] JSON output boş!");
					}
				}
				else
				{
					Console.Error.WriteLine($"[HATA] Python exit code: {process.ExitCode}");
					Console.Error.WriteLine($"[HATA] Error: {errorOutput}");
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