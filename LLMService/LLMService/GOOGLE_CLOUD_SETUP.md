# Google Cloud AI Platform Kurulum Rehberi

## Google Cloud AI Platform Nedir?
Google Cloud AI Platform, Google'ın profesyonel AI servislerini kullanmanızı sağlayan enterprise-grade bir platformdur. Gemini modellerini daha güvenilir ve ölçeklenebilir şekilde kullanabilirsiniz.

## Kurulum Adımları

### 1. Google Cloud Console'a Giriş
- https://console.cloud.google.com adresine gidin
- Google hesabınızla giriş yapın

### 2. Yeni Proje Oluşturun
- Sol menüden "Projeler" seçin
- "YENİ PROJE" butonuna tıklayın
- Proje adı girin (örn: "finance-ai-project")
- "OLUŞTUR" butonuna tıklayın

### 3. AI Platform API'sini Etkinleştirin
- Sol menüden "API'ler ve Hizmetler" > "Kütüphane" seçin
- "Vertex AI API" arayın ve etkinleştirin
- "AI Platform API" arayın ve etkinleştirin

### 4. Service Account Oluşturun
- Sol menüden "IAM ve Yönetici" > "Service Accounts" seçin
- "SERVICE ACCOUNT OLUŞTUR" butonuna tıklayın
- Service account adı girin (örn: "finance-ai-service")
- "OLUŞTUR VE DEVAM ET" butonuna tıklayın
- Roller ekleyin:
  - "Vertex AI User"
  - "AI Platform Developer"
- "TAMAMLA" butonuna tıklayın

### 5. Service Account Key İndirin
- Oluşturulan service account'a tıklayın
- "Anahtarlar" sekmesine gidin
- "ANAHTAR EKLE" > "Yeni anahtar oluştur" seçin
- "JSON" formatını seçin ve "OLUŞTUR" butonuna tıklayın
- İndirilen JSON dosyasını güvenli bir yere kaydedin

### 6. Konfigürasyonu Güncelleyin
`appsettings.json` dosyasını güncelleyin:

```json
{
  "GoogleCloud": {
    "ProjectId": "your-project-id",
    "Location": "us-central1",
    "ModelName": "gemini-1.5-flash",
    "CredentialsPath": "path/to/service-account-key.json"
  }
}
```

### 7. Test Edin
```http
POST /AI/ask
{
  "prompt": "THYAO hissesini analiz et",
  "serviceType": "googlecloud"
}
```

## Avantajları
- ✅ Enterprise-grade güvenlik
- ✅ Yüksek performans ve güvenilirlik
- ✅ Detaylı kullanım raporları
- ✅ SLA garantisi
- ✅ Ölçeklenebilir altyapı
- ✅ Gelişmiş model seçenekleri

## Dezavantajları
- ❌ Kurulum süreci karmaşık
- ❌ Ücretli servis (kullanım bazlı)
- ❌ Google Cloud hesabı gerektirir

## Fiyatlandırma
- Gemini 1.5 Flash: $0.075/1M token (input), $0.30/1M token (output)
- Gemini 1.5 Pro: $1.25/1M token (input), $5.00/1M token (output)

## Sorun Giderme
- **Kimlik doğrulama hatası**: Service account key dosyasının doğru yolda olduğundan emin olun
- **Proje bulunamadı**: Project ID'nin doğru olduğundan emin olun
- **API etkin değil**: Vertex AI API'sinin etkinleştirildiğinden emin olun
- **Yetki hatası**: Service account'a gerekli rollerin atandığından emin olun

## Güvenlik Notları
- Service account key dosyasını asla public repository'ye yüklemeyin
- Key dosyasını `.gitignore`'a ekleyin
- Production ortamında environment variables kullanın
