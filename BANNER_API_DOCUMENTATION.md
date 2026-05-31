# Banner API Documentation (Updated with Delivery Speed Targeting)

## Overview

This document provides complete API documentation for the Banner Management system with the new **Delivery Speed Targeting** feature for SpeedyMart platform.

## Database Changes

### Migration: AddDeliverySpeedToBanners

```csharp
// File: 20260521160000_AddDeliverySpeedToBanners.cs

migrationBuilder.Sql(@"
    DO $$
    BEGIN
        -- Add delivery speed targeting columns for SpeedyMart
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                       WHERE table_name='banners' AND column_name='is_normal') THEN
            ALTER TABLE banners ADD COLUMN is_normal boolean NOT NULL DEFAULT true;
        END IF;

        IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                       WHERE table_name='banners' AND column_name='is_express') THEN
            ALTER TABLE banners ADD COLUMN is_express boolean NOT NULL DEFAULT false;
        END IF;
    END
    $$;
");
```

**Columns Added:**
| Column | Type | Default | Description |
|--------|------|---------|-------------|
| `is_normal` | boolean | `true` | Show banner for Normal delivery |
| `is_express` | boolean | `false` | Show banner for Express delivery |

---

## Domain Model Changes

### BannerDM.cs

```csharp
namespace Siffrum.Ecom.DomainModels.v1
{
    [Table("banners")]
    public class BannerDM : SiffrumDomainModelBase<long>
    {
        [Required]
        [Column("type")]
        public ExtensionTypeDM Extension { get; set; }

        [Required]
        [Column("banner_type")]
        public BannerTypeDM BannerType { get; set; }
        
        [Column("platform_type")]
        public PlatformTypeDM PlatformType { get; set; }
        
        public string Title { get; set; }

        [Column("sub_title")]
        public string SubTitle { get; set; }
        
        [Required]
        [Column("image")]
        public string ContentPath { get; set; } 

        [Column("slider_url")]
        public string? SliderUrl { get; set; }

        [Column("is_default")]
        public bool IsDefault { get; set; }
        
        [Column("priority")]
        public int Priority { get; set; }

        // ═══════════════════════════════════════════════════════════
        // DELIVERY SPEED TARGETING (NEW FIELDS)
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>
        /// Show banner for Normal delivery speed (SpeedyMart only)
        /// Default: true
        /// </summary>
        [Column("is_normal")]
        public bool IsNormal { get; set; } = true;
        
        /// <summary>
        /// Show banner for Express delivery speed (SpeedyMart only)
        /// Default: false
        /// </summary>
        [Column("is_express")]
        public bool IsExpress { get; set; } = false;

        public ICollection<ProductBannerDM> BannerProducts { get; set; }
    }
}
```

---

## Service Model Changes

### BannerSM.cs

```csharp
namespace Siffrum.Ecom.ServiceModels.v1
{
    public class BannerSM : SiffrumServiceModelBase<long>
    {
        public ExtensionTypeSM Extension { get; set; }
        public BannerTypeSM BannerType { get; set; }
        public PlatformTypeSM PlatformType { get; set; }
        
        public string Title { get; set; }
        public string SubTitle { get; set; }
        
        /// <summary>
        /// Base64 encoded image/video content (for upload)
        /// </summary>
        public string ContentBase64 { get; set; }
        
        /// <summary>
        /// Resolved CDN/Network URL (for display)
        /// </summary>
        public string? NetworkContent { get; set; }

        public string? SliderUrl { get; set; }
        public bool IsDefault { get; set; }
        public int Priority { get; set; }

        // ═══════════════════════════════════════════════════════════
        // DELIVERY SPEED TARGETING (NEW FIELDS)
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>
        /// Target banner for Normal delivery speed
        /// Only applicable when PlatformType = SpeedyMart (2)
        /// Default: true
        /// </summary>
        public bool IsNormal { get; set; } = true;
        
        /// <summary>
        /// Target banner for Express delivery speed
        /// Only applicable when PlatformType = SpeedyMart (2)
        /// Default: false
        /// </summary>
        public bool IsExpress { get; set; } = false;
    }
}
```

---

## Enums

### PlatformType (Existing)

```csharp
public enum PlatformTypeSM
{
    None = 0,
    HotBox = 1,      // Food delivery platform
    SpeedyMart = 2   // Grocery/mart platform (supports Normal & Express)
}
```

### BannerType

```csharp
public enum BannerTypeSM
{
    HeroBanner = 0,      // Main hero banner (platform-specific)
    ThemeSplash = 1,     // Theme-based splash
    FestivalBanner = 2,  // Festival promotions
    ProductBanner = 3    // Product-specific banner
}
```

### ExtensionType

```csharp
public enum ExtensionTypeSM
{
    JPG = 0,
    JPEG = 1,
    PNG = 2,
    WEBP = 3,
    GIF = 4,
    MP4 = 5,    // Video support
    SVG = 6
}
```

---

## API Endpoints

### 1. CREATE BANNER

**Endpoint:** `POST /api/v1/Banner`

**Description:** Creates a new banner with optional delivery speed targeting for SpeedyMart.

**Authorization:** Admin Bearer Token

**Request Body (HotBox):**
```json
{
  "reqData": {
    "Title": "Summer Food Festival",
    "SubTitle": "Get 50% off on all orders",
    "BannerType": "HeroBanner",
    "PlatformType": 1,
    "ContentBase64": "base64encodedstring...",
    "Extension": 0,
    "SliderUrl": "https://example.com/promo",
    "Priority": 1,
    "IsDefault": false,
    "IsNormal": true,
    "IsExpress": false
  }
}
```

**Request Body (SpeedyMart - Normal Only):**
```json
{
  "reqData": {
    "Title": "Grocery Mega Sale",
    "SubTitle": "Fresh vegetables at best prices",
    "BannerType": "HeroBanner",
    "PlatformType": 2,
    "ContentBase64": "base64encodedstring...",
    "Extension": 0,
    "SliderUrl": "https://example.com/grocery",
    "Priority": 1,
    "IsDefault": false,
    "IsNormal": true,
    "IsExpress": false
  }
}
```

**Request Body (SpeedyMart - Express Only):**
```json
{
  "reqData": {
    "Title": "Express Delivery Special",
    "SubTitle": "Get delivery in 30 minutes!",
    "BannerType": "HeroBanner",
    "PlatformType": 2,
    "ContentBase64": "base64encodedstring...",
    "Extension": 0,
    "SliderUrl": "https://example.com/express",
    "Priority": 1,
    "IsDefault": false,
    "IsNormal": false,
    "IsExpress": true
  }
}
```

**Request Body (SpeedyMart - Both Normal & Express):**
```json
{
  "reqData": {
    "Title": "Weekend Sale",
    "SubTitle": "Big discounts on all items",
    "BannerType": "HeroBanner",
    "PlatformType": 2,
    "ContentBase64": "base64encodedstring...",
    "Extension": 0,
    "SliderUrl": "https://example.com/weekend",
    "Priority": 1,
    "IsDefault": false,
    "IsNormal": true,
    "IsExpress": true
  }
}
```

**Success Response (200 OK):**
```json
{
  "successData": {
    "boolResponse": true,
    "displayMessage": "Banner created successfully"
  },
  "isError": false,
  "errorData": null
}
```

**Error Response (400 Bad Request):**
```json
{
  "successData": null,
  "isError": true,
  "errorData": {
    "ErrorCode": "InvalidInputData_NoLog",
    "Message": "Only JPG, JPEG, PNG images (max 4 MB) and MP4 videos (max 6 MB) are allowed for banners"
  }
}
```

---

### 2. UPDATE BANNER

**Endpoint:** `PUT /api/v1/Banner/{id}`

**Description:** Updates an existing banner including delivery speed targeting.

**Authorization:** Admin Bearer Token

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | long | Yes | Banner ID |

**Request Body:**
```json
{
  "reqData": {
    "Id": 123,
    "Title": "Updated Sale Banner",
    "SubTitle": "New subtitle",
    "BannerType": "HeroBanner",
    "PlatformType": 2,
    "SliderUrl": "https://example.com/updated",
    "Priority": 2,
    "IsDefault": true,
    "IsNormal": true,
    "IsExpress": true,
    "ContentBase64": "",  // Empty if not changing image
    "Extension": 0
  }
}
```

**Success Response (200 OK):**
```json
{
  "successData": {
    "Id": 123,
    "Title": "Updated Sale Banner",
    "SubTitle": "New subtitle",
    "BannerType": "HeroBanner",
    "PlatformType": 2,
    "PlatformTypeName": "SpeedyMart",
    "ContentBase64": "base64string...",
    "NetworkContent": "https://cdn.example.com/banner123.jpg",
    "SliderUrl": "https://example.com/updated",
    "Priority": 2,
    "IsDefault": true,
    "IsNormal": true,
    "IsExpress": true,
    "CreatedAt": "2025-05-21T10:00:00Z",
    "UpdatedAt": "2025-05-21T12:00:00Z"
  },
  "isError": false,
  "errorData": null
}
```

---

### 3. GET BANNER BY ID

**Endpoint:** `GET /api/v1/Banner/{id}`

**Description:** Retrieves a single banner by ID with delivery speed settings.

**Authorization:** Admin Bearer Token

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | long | Yes | Banner ID |

**Success Response (200 OK):**
```json
{
  "successData": {
    "Id": 123,
    "Title": "Grocery Mega Sale",
    "SubTitle": "Fresh vegetables at best prices",
    "BannerType": "HeroBanner",
    "PlatformType": 2,
    "PlatformTypeName": "SpeedyMart",
    "ContentBase64": "base64string...",
    "NetworkContent": "https://cdn.example.com/banner123.jpg",
    "SliderUrl": "https://example.com/grocery",
    "Priority": 1,
    "IsDefault": false,
    "IsNormal": true,
    "IsExpress": false,
    "CreatedAt": "2025-05-21T10:00:00Z",
    "UpdatedAt": "2025-05-21T10:00:00Z"
  },
  "isError": false,
  "errorData": null
}
```

---

### 4. GET ALL BANNERS (Admin)

**Endpoint:** `GET /api/v1/Banner`

**Description:** Retrieves paginated list of all banners.

**Authorization:** Admin Bearer Token

**Query Parameters:**
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| skip | int | No | 0 | Number of records to skip |
| top | int | No | 10 | Number of records to return |
| platformType | int | No | - | Filter by platform (1=HotBox, 2=SpeedyMart) |

**Success Response (200 OK):**
```json
{
  "successData": [
    {
      "Id": 1,
      "Title": "HotBox Special",
      "SubTitle": "Food offers",
      "BannerType": "HeroBanner",
      "PlatformType": 1,
      "PlatformTypeName": "HotBox",
      "NetworkContent": "https://cdn.example.com/banner1.jpg",
      "Priority": 1,
      "IsDefault": true,
      "IsNormal": true,
      "IsExpress": false
    },
    {
      "Id": 2,
      "Title": "SpeedyMart Normal",
      "SubTitle": "Grocery deals",
      "BannerType": "HeroBanner",
      "PlatformType": 2,
      "PlatformTypeName": "SpeedyMart",
      "NetworkContent": "https://cdn.example.com/banner2.jpg",
      "Priority": 2,
      "IsDefault": false,
      "IsNormal": true,
      "IsExpress": false
    },
    {
      "Id": 3,
      "Title": "SpeedyMart Express",
      "SubTitle": "Fast delivery",
      "BannerType": "HeroBanner",
      "PlatformType": 2,
      "PlatformTypeName": "SpeedyMart",
      "NetworkContent": "https://cdn.example.com/banner3.jpg",
      "Priority": 3,
      "IsDefault": false,
      "IsNormal": false,
      "IsExpress": true
    },
    {
      "Id": 4,
      "Title": "SpeedyMart All",
      "SubTitle": "All delivery types",
      "BannerType": "HeroBanner",
      "PlatformType": 2,
      "PlatformTypeName": "SpeedyMart",
      "NetworkContent": "https://cdn.example.com/banner4.jpg",
      "Priority": 4,
      "IsDefault": false,
      "IsNormal": true,
      "IsExpress": true
    }
  ],
  "isError": false,
  "errorData": null
}
```

---

### 5. GET BANNER COUNT

**Endpoint:** `GET /api/v1/Banner/count`

**Description:** Get total count of banners.

**Authorization:** Admin Bearer Token

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| platformType | int | No | Filter by platform |

**Success Response (200 OK):**
```json
{
  "successData": {
    "intResponse": 45,
    "displayMessage": "Total Banners"
  },
  "isError": false,
  "errorData": null
}
```

---

### 6. GET BANNERS BY TYPE

**Endpoint:** `GET /api/v1/Banner/by-type`

**Description:** Get banners filtered by banner type and platform.

**Authorization:** Public / Admin

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| bannerType | string | Yes | HeroBanner, ThemeSplash, etc. |
| platform | int | Yes | 1=HotBox, 2=SpeedyMart |
| skip | int | No | Pagination skip |
| top | int | No | Pagination top |

**Success Response (200 OK):**
```json
{
  "successData": [
    {
      "Id": 10,
      "Title": "Weekend Special",
      "SubTitle": "Great deals",
      "BannerType": "HeroBanner",
      "PlatformType": 2,
      "NetworkContent": "https://cdn.example.com/banner10.jpg",
      "Priority": 1,
      "IsDefault": true,
      "IsNormal": true,
      "IsExpress": true
    }
  ],
  "isError": false,
  "errorData": null
}
```

---

### 7. GET DEFAULT BANNER

**Endpoint:** `GET /api/v1/Banner/default`

**Description:** Get the default banner for a specific platform and banner type.

**Authorization:** Public

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| bannerType | string | Yes | Banner type enum value |
| platform | int | Yes | 1=HotBox, 2=SpeedyMart |

**Success Response (200 OK):**
```json
{
  "successData": {
    "Id": 10,
    "Title": "Featured Banner",
    "SubTitle": "Default display",
    "BannerType": "HeroBanner",
    "PlatformType": 2,
    "NetworkContent": "https://cdn.example.com/default.jpg",
    "SliderUrl": "https://example.com/featured",
    "IsDefault": true,
    "IsNormal": true,
    "IsExpress": false
  },
  "isError": false,
  "errorData": null
}
```

---

### 8. UPDATE DEFAULT STATUS

**Endpoint:** `PUT /api/v1/Banner/{id}/default`

**Description:** Update the default status of a banner.

**Authorization:** Admin Bearer Token

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | long | Yes | Banner ID |

**Request Body:**
```json
{
  "reqData": {
    "IsDefault": true
  }
}
```

**Success Response (200 OK):**
```json
{
  "successData": {
    "boolResponse": true,
    "displayMessage": "Banner default status updated successfully"
  },
  "isError": false,
  "errorData": null
}
```

---

### 9. DELETE BANNER

**Endpoint:** `DELETE /api/v1/Banner/{id}`

**Description:** Delete a banner and its associated media.

**Authorization:** Admin Bearer Token

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | long | Yes | Banner ID |

**Success Response (200 OK):**
```json
{
  "successData": {
    "IsDeleted": true,
    "DisplayMessage": "Banner deleted successfully",
    "RecordId": 123
  },
  "isError": false,
  "errorData": null
}
```

---

## User-Facing APIs (Mobile Apps)

### 10. GET BANNERS FOR USER APP (HotBox)

**Endpoint:** `GET /api/v1/User/hotbox/banners`

**Description:** Get active banners for HotBox platform.

**Authorization:** User Bearer Token

**Query Parameters:**
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| skip | int | No | 0 | Pagination |
| top | int | No | 10 | Page size |

**Success Response (200 OK):**
```json
{
  "successData": [
    {
      "Id": 1,
      "Title": "HotBox Food Festival",
      "SubTitle": "Delicious deals",
      "BannerType": "HeroBanner",
      "PlatformType": 1,
      "NetworkContent": "https://cdn.example.com/hotbox1.jpg",
      "SliderUrl": "https://example.com/food-festival",
      "Priority": 1
    }
  ],
  "isError": false,
  "errorData": null
}
```

---

### 11. GET BANNERS FOR USER APP (SpeedyMart)

**Endpoint:** `GET /api/v1/User/speedymart/banners`

**Description:** Get active banners for SpeedyMart filtered by delivery speed.

**Authorization:** User Bearer Token

**Query Parameters:**
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| deliverySpeed | int | Yes | - | 1=Normal, 2=Express |
| skip | int | No | 0 | Pagination |
| top | int | No | 10 | Page size |

**Request Example:**
```
GET /api/v1/User/speedymart/banners?deliverySpeed=1&skip=0&top=10
```

**Success Response (Normal Delivery - 200 OK):**
```json
{
  "successData": [
    {
      "Id": 2,
      "Title": "Grocery Mega Sale",
      "SubTitle": "Fresh vegetables",
      "BannerType": "HeroBanner",
      "PlatformType": 2,
      "NetworkContent": "https://cdn.example.com/normal-banner.jpg",
      "SliderUrl": "https://example.com/grocery",
      "Priority": 1,
      "IsNormal": true,
      "IsExpress": false
    },
    {
      "Id": 4,
      "Title": "Weekend Sale",
      "SubTitle": "All items discounted",
      "BannerType": "HeroBanner",
      "PlatformType": 2,
      "NetworkContent": "https://cdn.example.com/weekend.jpg",
      "SliderUrl": "https://example.com/weekend",
      "Priority": 2,
      "IsNormal": true,
      "IsExpress": true
    }
  ],
  "isError": false,
  "errorData": null
}
```

**Success Response (Express Delivery - 200 OK):**
```json
{
  "successData": [
    {
      "Id": 3,
      "Title": "Express Delivery Special",
      "SubTitle": "30 min delivery",
      "BannerType": "HeroBanner",
      "PlatformType": 2,
      "NetworkContent": "https://cdn.example.com/express-banner.jpg",
      "SliderUrl": "https://example.com/express",
      "Priority": 1,
      "IsNormal": false,
      "IsExpress": true
    },
    {
      "Id": 4,
      "Title": "Weekend Sale",
      "SubTitle": "All items discounted",
      "BannerType": "HeroBanner",
      "PlatformType": 2,
      "NetworkContent": "https://cdn.example.com/weekend.jpg",
      "SliderUrl": "https://example.com/weekend",
      "Priority": 2,
      "IsNormal": true,
      "IsExpress": true
    }
  ],
  "isError": false,
  "errorData": null
}
```

**Filtering Logic (Backend):**
```csharp
// For Normal delivery (deliverySpeed=1)
query = query.Where(b => b.IsNormal == true);

// For Express delivery (deliverySpeed=2)
query = query.Where(b => b.IsExpress == true);
```

---

## Business Logic

### Delivery Speed Targeting Rules

| Platform | IsNormal | IsExpress | When Shown |
|----------|----------|-----------|------------|
| HotBox | true | false | Always (HotBox has single speed) |
| SpeedyMart | true | false | Only for Normal delivery orders |
| SpeedyMart | false | true | Only for Express delivery orders |
| SpeedyMart | true | true | For both Normal & Express orders |
| SpeedyMart | false | false | Never shown (validation error) |

### Backend Filtering Implementation

```csharp
public async Task<List<BannerSM>> GetBannersForSpeedyMart(
    int deliverySpeed, 
    int skip, 
    int top)
{
    var query = _apiDbContext.Banners
        .AsNoTracking()
        .Where(b => b.PlatformType == PlatformTypeDM.SpeedyMart)
        .Where(b => b.BannerType == BannerTypeDM.HeroBanner);

    // Apply delivery speed filter
    if (deliverySpeed == 1) // Normal
    {
        query = query.Where(b => b.IsNormal);
    }
    else if (deliverySpeed == 2) // Express
    {
        query = query.Where(b => b.IsExpress);
    }

    var banners = await query
        .OrderBy(b => b.Priority)
        .Skip(skip)
        .Take(top)
        .ToListAsync();

    return await MapBannersToSM(banners);
}
```

---

## Frontend Implementation (Admin Panel)

### Banner Form UI

When `PlatformType = SpeedyMart (2)`, show delivery speed checkboxes:

```tsx
{form.bannerType === "HeroBanner" && form.platformType === "2" && (
  <div className="space-y-2 rounded-lg border border-slate-200 bg-slate-50/50 p-3">
    <Label className="text-sm font-medium text-slate-700">
      Delivery Speed Targeting
    </Label>
    <p className="text-xs text-slate-500">
      Select which delivery speeds this banner should appear for
    </p>
    <div className="flex items-center gap-4">
      <label className="flex items-center gap-2 cursor-pointer">
        <input
          type="checkbox"
          checked={form.isNormal}
          onChange={(e) => setForm((f) => ({ ...f, isNormal: e.target.checked }))}
          className="h-4 w-4 rounded border-slate-300 text-indigo-600"
        />
        <span className="text-sm text-slate-700">Normal Delivery</span>
      </label>
      <label className="flex items-center gap-2 cursor-pointer">
        <input
          type="checkbox"
          checked={form.isExpress}
          onChange={(e) => setForm((f) => ({ ...f, isExpress: e.target.checked }))}
          className="h-4 w-4 rounded border-slate-300 text-indigo-600"
        />
        <span className="text-sm text-slate-700">Express Delivery</span>
      </label>
    </div>
    {!form.isNormal && !form.isExpress && (
      <p className="text-xs text-amber-600">
        ⚠️ Please select at least one delivery speed
      </p>
    )}
  </div>
)}
```

### Table Display

Show delivery speed badges for SpeedyMart banners:

```tsx
<TableCell>
  {String(b.platformType) === "2" || b.platformType === "SpeedyMart" ? (
    <div className="flex flex-col gap-1">
      <Badge className="bg-emerald-100 text-emerald-800">SpeedyMart</Badge>
      {(b.isNormal || b.isExpress) && (
        <div className="flex gap-1">
          {b.isNormal && (
            <span className="text-[10px] px-1.5 py-0.5 bg-blue-100 text-blue-700 rounded">
              Normal
            </span>
          )}
          {b.isExpress && (
            <span className="text-[10px] px-1.5 py-0.5 bg-purple-100 text-purple-700 rounded">
              Express
            </span>
          )}
        </div>
      )}
    </div>
  ) : (
    <Badge className="bg-orange-100 text-orange-800">HotBox</Badge>
  )}
</TableCell>
```

---

## Migration Notes

### Backward Compatibility
- Existing banners will have `IsNormal = true` and `IsExpress = false` (database defaults)
- HotBox banners ignore these fields (always shown)
- API consumers not sending these fields will use defaults

### Rollback Script
```sql
ALTER TABLE banners DROP COLUMN IF EXISTS is_normal;
ALTER TABLE banners DROP COLUMN IF EXISTS is_express;
```

---

## Summary of Changes

### Files Modified

| File | Changes |
|------|---------|
| `20260521160000_AddDeliverySpeedToBanners.cs` | New migration - adds `is_normal` and `is_express` columns |
| `BannerDM.cs` | Added `IsNormal` and `IsExpress` properties |
| `BannerSM.cs` | Added `IsNormal` and `IsExpress` properties |
| `banner-page-content.tsx` | Added delivery speed checkboxes UI for SpeedyMart |

### API Changes

| Endpoint | Impact |
|----------|--------|
| `POST /api/v1/Banner` | Now accepts `IsNormal` and `IsExpress` fields |
| `PUT /api/v1/Banner/{id}` | Now accepts `IsNormal` and `IsExpress` fields |
| `GET /api/v1/Banner` | Returns `IsNormal` and `IsExpress` in response |
| `GET /api/v1/Banner/{id}` | Returns `IsNormal` and `IsExpress` in response |
| `GET /api/v1/User/speedymart/banners` | Filters by `deliverySpeed` parameter |

---

## Error Codes

| Error Code | HTTP Status | Description |
|------------|-------------|-------------|
| InvalidInputData_NoLog | 400 | Invalid input (no logging) |
| InvalidInputData_Log | 400 | Invalid input (with logging) |
| Fatal_Log | 500 | Critical error |
| Unauthorized | 401 | Missing/invalid token |
| Forbidden | 403 | Insufficient permissions |
| NotFound | 404 | Banner not found |

---

*Documentation generated for SpeedyCart/HotBox Banner Management System*  
*Feature: Delivery Speed Targeting for SpeedyMart*  
*Last updated: May 21, 2026*
