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
	}
}