# 🚀 Performans İyileştirme Önerileri

## ⚠️ KRİTİK SORUNLAR

### 1. **Memory'de Sorting (UserManager.cs)**
**Sorun:** `GetAllUsersOrderByDateAsync` tüm veriyi memory'ye çekip sonra sıralıyor.

**Mevcut Kod:**
```csharp
var users = await _userRepo.GetAllWithRolesAsync(ct);
return _mapper.Map<List<UserDto>>(users.OrderByDescending(u => u.InsertDate).ToList());
```

**Öneri:**
```csharp
// Repository'de ORDER BY ekle
public async Task<List<User>> GetAllWithRolesOrderByDateAsync(CancellationToken ct = default)
    => await _context.Users
          .Include(u => u.UserRoles)
          .ThenInclude(ur => ur.Role)
          .OrderByDescending(u => u.InsertDate)
          .ToListAsync(ct);

// Manager'da
public async Task<List<UserDto>> GetAllUsersOrderByDateAsync(CancellationToken ct = default)
{
    var users = await _userRepo.GetAllWithRolesOrderByDateAsync(ct);
    return _mapper.Map<List<UserDto>>(users);
}
```

### 2. **Pagination Yok - GetAll Metodları**
**Sorun:** Tüm kullanıcıları, stock'ları, finansal raporları çekiyor. Büyük veri setlerinde sorun yaratır.

**Öneri:**
```csharp
// Repository'ye ekle
public async Task<(List<User> Users, int TotalCount)> GetAllWithRolesPagedAsync(
    int pageNumber, 
    int pageSize, 
    CancellationToken ct = default)
{
    var query = _context.Users
        .Include(u => u.UserRoles)
        .ThenInclude(ur => ur.Role)
        .Where(u => u.IsActive); // Sadece aktif kullanıcılar
    
    var totalCount = await query.CountAsync(ct);
    
    var users = await query
        .OrderByDescending(u => u.InsertDate)
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(ct);
    
    return (users, totalCount);
}
```

### 3. **AsNoTracking() Eksik**
**Sorun:** Read-only sorgularda change tracking gereksiz.

**Öneri:**
```csharp
public async Task<List<User>> GetAllWithRolesAsync(CancellationToken ct = default)
    => await _context.Users
          .AsNoTracking() // ⚡ Change tracking'i kapat
          .Include(u => u.UserRoles)
          .ThenInclude(ur => ur.Role)
          .ToListAsync(ct);
```

### 4. **Frontend'de Client-Side Filtering**
**Sorun:** StockDetailPage'de tüm stock'lar çekilip client-side'da filter yapılıyor.

**Mevcut:**
```typescript
StockService.getAll().then(stocks => stocks.find(s => s.symbol === symbol))
```

**Öneri:**
```typescript
// Backend'e endpoint ekle
StockService.getBySymbol(symbol)

// Controller'da
[HttpGet("{symbol}")]
public async Task<IActionResult> GetBySymbol(string symbol)
{
    var stock = await _stockManager.GetStockBySymbolAsync(symbol);
    return Ok(stock);
}
```

### 5. **AutoMapper - Record ile Uyumsuzluk**
**Sorun:** Record kullanıyorsunuz ama `UserDto` hala `set` property'leri var. Record'lar immutable olmalı.

**Mevcut:**
```csharp
public record UserDto : IDto
{
    public int UserId { get; set; } // ❌ Record'da set olmamalı
}
```

**Öneri:**
```csharp
// ✅ DOĞRU: Record immutable
public record UserDto(
    int UserId,
    string Name,
    string Username,
    string Email,
    string RoleName,
    DateTime RegistrationDate,
    bool IsActive
) : IDto;

// AutoMapper Profile'da
CreateMap<User, UserDto>()
    .ConstructUsing(src => new UserDto(
        src.UserId,
        src.Name,
        src.Username,
        src.Email,
        src.UserRoles.FirstOrDefault()?.Role?.Name ?? "User",
        src.InsertDate,
        src.IsActive
    ));
```

### 6. **N+1 Query Problem (AutoMapper)**
**Sorun:** `UserDto` mapping'de `FirstOrDefault()` her kayıt için çalışıyor.

**Mevcut:**
```csharp
.ForMember(dest => dest.RoleName,
    opt => opt.MapFrom(src =>
        src.UserRoles.FirstOrDefault() != null
            ? src.UserRoles.FirstOrDefault().Role.Name
            : null))
```

**Öneri:**
```csharp
// Include ile zaten yükleniyor, direkt kullan
.ForMember(dest => dest.RoleName,
    opt => opt.MapFrom(src =>
        src.UserRoles.Select(ur => ur.Role.Name).FirstOrDefault() ?? "User"))
```

### 7. **Gereksiz ToList() Çağrıları**
**Sorun:** `GetAllUsersOrderByDateAsync`'de gereksiz `ToList()` çağrısı.

**Öneri:** Repository'de direkt `ToListAsync()` kullan.

### 8. **Redis Cache - TTL Optimizasyonu**
**Mevcut:** 10 dakika TTL. Update olduğunda cache invalidate edilmeli.

**Öneri:**
```csharp
public async Task UpdateStocksFromExternalApiAsync()
{
    var stocksFromApi = await _externalApiService.FetchBistStocksAsync();
    if (stocksFromApi == null || !stocksFromApi.Any()) return;
    
    await _stockRepository.BulkUpsertAsync(stocksFromApi);
    
    // Cache'i güncelle
    var cacheKey = "stocks:data:all";
    var jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    await _redisCacheService.SetValueAsync(
        cacheKey,
        JsonSerializer.Serialize(stocksFromApi, jsonOptions),
        TimeSpan.FromMinutes(10));
    
    // Event yayınla
    foreach (var s in stocksFromApi)
    {
        await _publishEndpoint.Publish(new StockPriceUpdated(
            s.Symbol, s.CurrentPrice, DateTime.UtcNow));
    }
}
```

## 📊 ÖNCELİK SIRASI

### 🔴 YÜKSEK ÖNCELİK
1. ✅ **Memory'de Sorting** → SQL'de ORDER BY
2. ✅ **AsNoTracking()** ekle (read-only sorgular)
3. ✅ **Pagination** ekle (GetAll metodlarına)
4. ✅ **Frontend client-side filtering** → Backend endpoint

### 🟡 ORTA ÖNCELİK
5. ✅ **Record immutable** yap (set kaldır)
6. ✅ **N+1 Query** düzelt (AutoMapper)
7. ✅ **Gereksiz ToList()** kaldır

### 🟢 DÜŞÜK ÖNCELİK
8. ✅ **Cache invalidation** stratejisi
9. ✅ **Connection pooling** kontrolü
10. ✅ **Index'ler** kontrolü (PostgreSQL)

## 🎯 RECORD KULLANIMI - EN İYİ PRATİKLER

### ✅ DOĞRU KULLANIM
```csharp
// Immutable record
public record UserDto(
    int UserId,
    string Name,
    string Username,
    string Email,
    string RoleName,
    DateTime RegistrationDate,
    bool IsActive
) : IDto;

// AutoMapper ile
CreateMap<User, UserDto>()
    .ConstructUsing(src => new UserDto(
        src.UserId,
        src.Name,
        src.Username,
        src.Email,
        src.UserRoles.Select(ur => ur.Role.Name).FirstOrDefault() ?? "User",
        src.InsertDate,
        src.IsActive
    ));
```

### ❌ YANLIŞ KULLANIM
```csharp
// Record ama mutable - gereksiz
public record UserDto : IDto
{
    public int UserId { get; set; } // ❌
    public string Name { get; set; } // ❌
}
```

## 📈 BEKLENEN İYİLEŞTİRMELER

- **Memory kullanımı:** %30-50 azalma
- **Query süresi:** %40-60 iyileşme (AsNoTracking + SQL ORDER BY)
- **Network trafiği:** %70-90 azalma (Pagination)
- **Response süresi:** %50-70 iyileşme (Frontend filtering → Backend)

## 🔧 HIZLI UYGULAMA ADIMLARI

1. **UserRepository.cs** - `AsNoTracking()` ekle
2. **UserRepository.cs** - `GetAllWithRolesOrderByDateAsync()` ekle
3. **UserManager.cs** - `GetAllUsersOrderByDateAsync()` düzelt
4. **StockService** - `GetBySymbol` endpoint ekle
5. **Frontend** - `StockService.getBySymbol()` kullan
6. **UserDto** - Record immutable yap
7. **AutoMapperProfile** - `ConstructUsing` kullan

