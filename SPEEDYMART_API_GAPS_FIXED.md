# SpeedyMart API Gaps — Fixed (16 May 2026)

All gaps identified from the `SPEEDYMART_BACKEND_API_SPEC.md` review have been implemented.

---

## 1. `DeliverySpeedType` on Orders

**Problem:** Orders had no way to record whether the user chose Express or Normal delivery for SpeedyMart.

**Changes:**
- `OrderDM.cs` — added `delivery_speed_type` column (int, default 0)
- `OrderSM.cs` — added `DeliverySpeedType` field (0=N/A, 1=Normal, 2=Express)
- AutoMapper maps it automatically since names match

**Usage by app:**
- `POST /api/v1/Order/mine` — send `deliverySpeedType` inside `order` object
- `GET /api/v1/Order/mine/{id}` — response now includes `deliverySpeedType`

---

## 2. Fee Fields on Cart Response

**Problem:** App needed `deliveryFee`, `platformFee`, and `freeDeliveryThreshold` to show checkout breakdown without a separate API call.

**Changes:**
- `CombineCartSM.cs` — added 3 fields:
  - `DeliveryFee` (decimal)
  - `PlatformFee` (decimal)
  - `FreeDeliveryThreshold` (decimal)
- `DeliveryPlacesDM.cs` — added 2 DB columns:
  - `platform_charges` (decimal, default 0)
  - `free_delivery_threshold` (decimal, default 0)
- `CartProcess.BuildCombineCartResponse()` — populates fee fields from first active `DeliveryPlace`

**Usage by app:**
- `GET /api/v1/Cart/mine` — response now includes `deliveryFee`, `platformFee`, `freeDeliveryThreshold`
- All cart mutation endpoints (`add`, `update`, `remove`, `clear`) also return these fields in the response

---

## 3. `ProductId` on Listing DTOs

**Problem:** App couldn't navigate to PDP from product cards without knowing the parent `productId`.

**Changes:**
- `UserSpeedyMartProductSM.cs` — added `ProductId` field (long)
- `ProductVariantProcess.GetSpeedyMartProductsByBanner()` — populates `ProductId = product.Product?.Id ?? 0`

**Usage by app:**
- All SpeedyMart product listing endpoints now include `productId` in each product card object
- Use `productId` to call `GET /api/v1/Product/{productId}/pdp`

---

## 4. `UnitLabel` on Cart Line Items

**Problem:** Cart items didn't show the variant name (e.g. "1 kg", "500 ml") to display in the cart UI.

**Changes:**
- `SpeedyMartCartItemSM.cs` — added `UnitLabel` field (string, nullable)
- `CartProcess.BuildCombineCartResponse()` — loads variant names and sets `UnitLabel`

**Usage by app:**
- `GET /api/v1/Cart/mine` — each `speedyMartCartItems[]` entry now has `unitLabel`

---

## 5. Platform & Speed Filters on Order History

**Problem:** `GET /Order/mine` returned ALL orders across platforms. App had to filter client-side.

**Changes:**
- `OrderController.GetMyOrders()` — added optional query params: `platformType`, `deliverySpeedType`
- `OrderController.GetMineOrdersCount()` — same optional params
- `OrderProcess.GetMyOrdersAsync()` — applies server-side filters
- `OrderProcess.GetMyOrdersCountAsync()` — applies server-side filters

**Usage by app:**
```
GET /api/v1/Order/mine?skip=0&top=10&platformType=2
GET /api/v1/Order/mine?skip=0&top=10&platformType=2&deliverySpeedType=2
GET /api/v1/Order/mine/count?platformType=2
```

---

## Migration

**File:** `20260516000000_AddSpeedyMartApiGapFields.cs`

Adds (idempotent `IF NOT EXISTS`):
- `orders.delivery_speed_type` — integer NOT NULL DEFAULT 0
- `delivery_places.platform_charges` — numeric NOT NULL DEFAULT 0
- `delivery_places.free_delivery_threshold` — numeric NOT NULL DEFAULT 0

Auto-applies on next backend startup via `dbContext.Database.Migrate()`.

---

## Enum Reference

| Field | Values |
|-------|--------|
| `platformType` | 0=None, 1=HotBox, 2=SpeedyMart |
| `deliverySpeedType` | 0=N/A, 1=Normal, 2=Express, 3=Both |
| `paymentMode` | 1=CashOnDelivery, 2=Online |
