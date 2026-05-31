# SpeedyMart — Backend Pending Items Fixed (Round 2)

**Date:** 16 May 2026  
**Reference:** `SPEEDYMART_BACKEND_PENDING.md` from Flutter dev  
**Build:** ✅ Successful (0 errors)

---

## Summary

| Section | Item | Status |
|---------|------|--------|
| §3.1 | Place order — `discountAmount`, `promoCodeId`, post-order cart clear | ✅ Done |
| §3.2 | Order detail — `itemCount`, `unitLabel` on order items | ✅ Done |
| §4.3 | PDP reviews + Q&A on `GET /Product/{id}/pdp` | ✅ Done |
| §4.4 | Offers filter — `GET /Offer/platform?platformType=2` | ✅ Done |
| §5.1 | Store hours ETA — `estimatedMinutesMin/Max` | ✅ Done |
| §6.1 | `productId` on all listings — already covered (single code path) | ✅ Verified |
| §6.3 | `unitLabel` on nested `speedyMartProductDetails` | ✅ Done |
| §6.4 | MRP (`price` + `discountedPrice`) on cart items | ✅ Already present |
| §6.5 | `itemCount` on order list rows | ✅ Done |

---

## 1. §3.1 — Place Order Enhancements

### New fields on `OrderDM`

```csharp
[Column("discount_amount", TypeName = "decimal(18,2)")]
public decimal DiscountAmount { get; set; } = 0;

[Column("promo_code_id")]
public long? PromoCodeId { get; set; }
```

### New fields on `OrderSM`

```csharp
public decimal DiscountAmount { get; set; }
public long? PromoCodeId { get; set; }
public int ItemCount { get; set; }
```

### Post-order cart clear

After successful order creation, if `platformType == SpeedyMart` and `deliverySpeedType > 0`:
- Finds user's SpeedyMart cart
- Removes only items matching the ordered `deliverySpeedType` bucket (Express or Normal)
- Recalculates cart subtotal/grandTotal
- Does not fail the order if cart clear fails

**File:** `OrderProcess.cs` (after transaction commit)

### Mobile usage

```json
POST /api/v1/Order/mine
{
  "reqData": {
    "order": {
      "platformType": 2,
      "deliverySpeedType": 2,
      "discountAmount": 82,
      "promoCodeId": 5,
      ...
    },
    "orderItems": [...]
  }
}
```

---

## 2. §3.2 — Order Detail Enhancements

### `itemCount` on OrderSM

Every `GetOrderSM()` call now populates `ItemCount` (count of order_items for that order). This applies to:
- `GET /Order/mine` (list)
- `GET /Order/mine/{id}` (detail)
- All seller/admin order views

### `unitLabel` on OrderItemSM

```csharp
public string? UnitLabel { get; set; }
```

- `ProductName` → parent product name (e.g. "Fresh Apples")
- `UnitLabel` → variant name (e.g. "1 kg", "500 ml")
- `NetworkProductImage` → already populated from variant images

**File:** `OrderItemSM.cs`, `OrderProcess.GetOrderItemSM()`

---

## 3. §4.3 — PDP Reviews & Q&A

### Extended `GET /Product/{id}/pdp` response

New fields on `SpeedyMartPdpSM`:

```csharp
public PdpRatingSummarySM? Rating { get; set; }
public List<PdpReviewSM> Reviews { get; set; }
public List<PdpQaItemSM> QaItems { get; set; }
```

### Rating summary

```json
{
  "rating": {
    "rate": 4.2,
    "totalRatings": 45,
    "recommendPercent": 89,
    "tiers": [
      { "stars": 5, "count": 25 },
      { "stars": 4, "count": 12 },
      { "stars": 3, "count": 5 },
      { "stars": 2, "count": 2 },
      { "stars": 1, "count": 1 }
    ]
  }
}
```

### Reviews (top 20, newest first)

```json
{
  "reviews": [
    {
      "userName": "Priya",
      "rating": 5,
      "body": "Great quality",
      "createdAt": "2026-05-01T10:00:00",
      "verifiedPurchase": true
    }
  ]
}
```

### Q&A (from ProductFaq, top 20)

```json
{
  "qaItems": [
    {
      "question": "Is it organic?",
      "answer": "Yes, certified organic.",
      "createdAt": "2026-04-20T08:00:00"
    }
  ]
}
```

**Files:** `SpeedyMartPdpSM.cs`, `ProductController.cs`

---

## 4. §4.4 — Offers Filter by Platform

### New endpoints

```
GET /api/v1/Offer/platform?platformType=2&skip=0&top=60
GET /api/v1/Offer/platform/count?platformType=2
```

Returns only offers matching the given `platformType`. SpeedyMart = `2`.

**Files:** `OfferController.cs`, `OffersAndCouponsProcess.cs`

---

## 5. §5.1 — Store Hours ETA

### New fields on `StoreAvailabilitySM`

```csharp
public int? EstimatedMinutesMin { get; set; }
public int? EstimatedMinutesMax { get; set; }
```

When store is open, returns default ETA (`10`–`30` minutes). Can be made configurable per seller later.

### Mobile usage

```
GET /api/v1/StoreHours/availability/{sellerId}
```

```json
{
  "isOpen": true,
  "message": "Store is open until 10:00 PM",
  "estimatedMinutesMin": 10,
  "estimatedMinutesMax": 30
}
```

**File:** `StoreHoursProcess.cs`

---

## 6. §6.3 — `unitLabel` on Product Listings

Added `UnitLabel` to `UserSpeedyMartProductSM`:

```csharp
public string? UnitLabel { get; set; }
```

Populated with variant name (e.g. "1 kg") in `GetSpeedyMartProductsByBanner`. Mobile can now read:
- `speedyMartCartItems[].unitLabel` (root — already done in Round 1)
- `speedyMartProductDetails.unitLabel` (nested fallback — this fix)

**Files:** `UserSpeedyMartProductSM.cs`, `ProductVariantProcess.cs`

---

## 7. Migration Update

`20260516000000_AddSpeedyMartApiGapFields.cs` now includes:

```sql
-- orders: discount_amount
ALTER TABLE orders ADD COLUMN discount_amount numeric(18,2) NOT NULL DEFAULT 0;

-- orders: promo_code_id
ALTER TABLE orders ADD COLUMN promo_code_id bigint NULL;
```

(In addition to the existing `delivery_speed_type`, `platform_charges`, `free_delivery_threshold` columns)

---

## 8. Items NOT requiring backend changes

| Section | Reason |
|---------|--------|
| §4.2 Checkout address | Shared `UserAddress` APIs already work for SpeedyMart |
| §5.2 Home banners | Existing `GET /Banner/type/{type}?platform=2` already works |
| §5.3 Live tracking | Existing `GET /OrderDelivery/...` endpoints already work |
| §6.1 productId audit | All listing endpoints route through single `GetSpeedyMartProductsByBanner` method |
| §6.4 Cart MRP | `price` + `discountedPrice` already on `UserSpeedyMartProductSM` |
| §6.6 deliverySpeedType=Both | Backend already handles `Both=3` correctly; client sends explicit `1` or `2` on add-to-cart |
| §6.7 Fee config per place | Fees already come from `delivery_places` per seller/pincode |

---

## Files Modified

| File | Changes |
|------|---------|
| `OrderDM.cs` | +`DiscountAmount`, +`PromoCodeId` |
| `OrderSM.cs` | +`DiscountAmount`, +`PromoCodeId`, +`ItemCount` |
| `OrderItemSM.cs` | +`UnitLabel` |
| `OrderProcess.cs` | +ItemCount in GetOrderSM, +UnitLabel in GetOrderItemSM, +cart clear logic, +Product include |
| `UserSpeedyMartProductSM.cs` | +`UnitLabel` |
| `ProductVariantProcess.cs` | Populate `UnitLabel` in listings |
| `SpeedyMartPdpSM.cs` | +`PdpRatingSummarySM`, +`PdpReviewSM`, +`PdpQaItemSM` DTOs |
| `ProductController.cs` | PDP endpoint populates ratings, reviews, Q&A |
| `OfferController.cs` | +`GET /Offer/platform`, +`GET /Offer/platform/count` |
| `OffersAndCouponsProcess.cs` | +`GetAllByPlatform`, +`GetCountByPlatform` |
| `StoreHoursProcess.cs` | +`EstimatedMinutesMin/Max` on availability response |
| `20260516000000_AddSpeedyMartApiGapFields.cs` | +`discount_amount`, +`promo_code_id` columns |
