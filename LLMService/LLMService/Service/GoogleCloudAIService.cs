using LLMService.Service;
using Google.Cloud.AIPlatform.V1Beta1;
using System;
using System.Threading.Tasks;

public class GoogleCloudAIService : IAIService
{
    private readonly string _projectId;
    private readonly string _location;
    private readonly string _modelName;
    private readonly string _credentialsPath;

    public GoogleCloudAIService(string projectId, string location = "us-central1", string modelName = "gemini-1.5-flash", string credentialsPath = null)
    {
        _projectId = projectId;
        _location = location;
        _modelName = modelName;
        _credentialsPath = credentialsPath;
    }

    public async Task<string> GetAIResponseAsync(string prompt)
    {
        try
        {
            // Google Cloud kimlik doğrulama
            var clientBuilder = new PredictionServiceClientBuilder();
            
            if (!string.IsNullOrEmpty(_credentialsPath))
            {
                clientBuilder.CredentialsPath = _credentialsPath;
            }

            var client = await clientBuilder.BuildAsync();

            // Model endpoint'ini oluştur
            var modelEndpoint = $"projects/{_projectId}/locations/{_location}/publishers/google/models/{_modelName}";

            // İstek oluştur
            var request = new PredictRequest
            {
                Endpoint = modelEndpoint,
                Instances =
                {
                    new Google.Protobuf.WellKnownTypes.Value
                    {
                        StructValue = new Google.Protobuf.WellKnownTypes.Struct
                        {
                            Fields =
                            {
                                ["contents"] = new Google.Protobuf.WellKnownTypes.Value
                                {
                                    ListValue = new Google.Protobuf.WellKnownTypes.ListValue
                                    {
                                        Values =
                                        {
                                            new Google.Protobuf.WellKnownTypes.Value
                                            {
                                                StructValue = new Google.Protobuf.WellKnownTypes.Struct
                                                {
                                                    Fields =
                                                    {
                                                        ["role"] = new Google.Protobuf.WellKnownTypes.Value { StringValue = "user" },
                                                        ["parts"] = new Google.Protobuf.WellKnownTypes.Value
                                                        {
                                                            ListValue = new Google.Protobuf.WellKnownTypes.ListValue
                                                            {
                                                                Values =
                                                                {
                                                                    new Google.Protobuf.WellKnownTypes.Value
                                                                    {
                                                                        StructValue = new Google.Protobuf.WellKnownTypes.Struct
                                                                        {
                                                                            Fields =
                                                                            {
                                                                                ["text"] = new Google.Protobuf.WellKnownTypes.Value 
                                                                                { 
                                                                                    StringValue = $"Sen finansal analiz konusunda uzman bir AI asistanısın. Türkçe olarak finansal hisse analizi, temel analiz ve yatırım tavsiyeleri konularında yardımcı oluyorsun. Cevaplarını Türkçe ver ve finansal terimleri açık bir şekilde açıkla.\n\nKullanıcı sorusu: {prompt}"
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                Parameters = new Google.Protobuf.WellKnownTypes.Value
                {
                    StructValue = new Google.Protobuf.WellKnownTypes.Struct
                    {
                        Fields =
                        {
                            ["temperature"] = new Google.Protobuf.WellKnownTypes.Value { NumberValue = 0.7 },
                            ["topP"] = new Google.Protobuf.WellKnownTypes.Value { NumberValue = 0.9 },
                            ["topK"] = new Google.Protobuf.WellKnownTypes.Value { NumberValue = 40 },
                            ["maxOutputTokens"] = new Google.Protobuf.WellKnownTypes.Value { NumberValue = 2048 }
                        }
                    }
                }
            };

            // API çağrısı yap
            var response = await client.PredictAsync(request);

            if (response?.Predictions?.Count > 0)
            {
                var prediction = response.Predictions[0];
                if (prediction?.StructValue?.Fields?.ContainsKey("candidates") == true)
                {
                    var candidates = prediction.StructValue.Fields["candidates"];
                    if (candidates?.ListValue?.Values?.Count > 0)
                    {
                        var candidate = candidates.ListValue.Values[0];
                        if (candidate?.StructValue?.Fields?.ContainsKey("content") == true)
                        {
                            var content = candidate.StructValue.Fields["content"];
                            if (content?.StructValue?.Fields?.ContainsKey("parts") == true)
                            {
                                var parts = content.StructValue.Fields["parts"];
                                if (parts?.ListValue?.Values?.Count > 0)
                                {
                                    var part = parts.ListValue.Values[0];
                                    if (part?.StructValue?.Fields?.ContainsKey("text") == true)
                                    {
                                        return part.StructValue.Fields["text"].StringValue ?? "Cevap alınamadı.";
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return "Cevap formatı beklenmedik.";
        }
        catch (Exception ex)
        {
            throw new Exception($"Google Cloud AI Platform çağrısı başarısız: {ex.Message}");
        }
    }
}
