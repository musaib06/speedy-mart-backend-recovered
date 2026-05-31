# Surge Pricing API Documentation

## Overview

This document provides complete API documentation for all endpoints affected by the Surge Pricing feature across SpeedyMart and HotBox platforms.

## Database Changes

### Migration: AddSurgeChargeToOrders
```sql
ALTER TABLE orders ADD COLUMN surge_charge numeric(18,2) NOT NULL DEFAULT 0;
```

---

## Domain Model Changes

### OrderDM (Order Domain Model)
```csharp
[Column("surge_charge", TypeName = "decimal(18,2)")]
public decimal SurgeCharge { get; set; } = 0;
```

---

## Service Model Changes

### 1. OrderSM (Order Service Model)

```csharp
public class OrderSM : SiffrumServiceModelBase<long>
{
    public string OrderNumber { get; set; } = string.Empty;
    public long TransactionId { get; set; }
    public string? RazorpayOrderId { get; set; }
    public string? RazorpayPaymentId { get; set; }
    public string? RazorpayPaymentLinkUrl { get; set; }
    public long UserId { get; set; }
    public string? CustomerName { get; set; }
    public string? Receipt { get; set; }
    public string Currency { get; set; } = "INR";
    public decimal Amount { get; set; }
    public decimal TipAmount { get; set; }
    public decimal PaidAmount { get; set; } 
    public decimal DueAmount { get; set; }
    public decimal RefundAmount { get; set; } = 0;
    public string? FailureReason { get; set; }
    public bool IsCutlaryInculded { get; set; }
    public bool IsGiftWrapIncluded { get; set; }
    public decimal DeliveryCharge { get; set; }
    public decimal PlatormCharge { get; set; }
    public decimal CutlaryCharge { get; set; }
    public decimal GiftWrapCharge { get; set; }
    public decimal LowCartFeeCharge { get; set; }
    
    // SURGE PRICING FIELD
    public decimal SurgeCharge { get; set; }
    
    public string? CookingInstructions { get; set; }
    public decimal TaxAmount { get; set; }
    public long? AddressId { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public PaymentStatusSM PaymentStatus { get; set; } = PaymentStatusSM.Pending;
    public OrderStatusSM OrderStatus { get; set; } = OrderStatusSM.Created;
    public PaymentModeSM PaymentMode { get; set; }
    public long? SellerId { get; set; }
    public PlatformTypeSM PlatformType { get; set; } = PlatformTypeSM.HotBox;
    public int DeliverySpeedType { get; set; } // 0=N/A, 1=Normal, 2=Express
    public decimal DiscountAmount { get; set; }
    public long? PromoCodeId { get; set; }
    public string? PromoCode { get; set; }
    public int ItemCount { get; set; }
    public int PreparationTimeInMinutes { get; set; }
    public DateTime? SellerAcceptedAt { get; set; }
    public string? PreparationStatus { get; set; }
    public string? PreparationStatusMessage { get; set; }
    public long? DeliveryBoyId { get; set; }
    public string? DeliveryBoyName { get; set; }
    public UserAddressSM? DeliveryAddress { get; set; }
}
```

---

### 2. OrderInvoiceSM (Invoice Response)

```csharp
public class OrderInvoiceSM
{
    // Invoice meta
    public string InvoiceNumber { get; set; }
    public DateTime InvoiceDate { get; set; }
    public long OrderId { get; set; }
    public string OrderNumber { get; set; }

    // Seller info
    public InvoiceSellerSM Seller { get; set; }

    // Customer info
    public InvoiceCustomerSM Customer { get; set; }

    // Delivery address
    public InvoiceAddressSM DeliveryAddress { get; set; }

    // Order items
    public List<InvoiceItemSM> Items { get; set; }

    // Charges breakdown
    public decimal Subtotal { get; set; }
    public decimal DeliveryCharge { get; set; }
    public decimal PlatformCharge { get; set; }
    public decimal CutleryCharge { get; set; }
    public decimal GiftWrapCharge { get; set; }
    public decimal LowCartFeeCharge { get; set; }
    
    // SURGE PRICING FIELD
    public decimal SurgeCharge { get; set; }
    
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? PromoCode { get; set; }
    public decimal TipAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DueAmount { get; set; }

    // Payment
    public string Currency { get; set; }
    public string PaymentMode { get; set; }
    public string PaymentStatus { get; set; }
    public string OrderStatus { get; set; }
    public long TransactionId { get; set; }
    public string? RazorpayPaymentId { get; set; }
}
```

---

### 3. SettingsSM (Global Settings)

```csharp
public class SettingsSM : SiffrumServiceModelBase<long>
{
    public bool IsOrderPossible { get; set; }
    public bool IsFreeDelivery { get; set; }
    
    // LEGACY SURGE SETTINGS (Backward Compatibility)
    public bool IsSurge { get; set; }
    public int SurgeCount { get; set; } = 0;
    public decimal SurgeCharge { get; set; }
    
    public int DeliveryInMinutes { get; set; }
    public int ReferralPercentage { get; set; }
    public decimal PlatormCharge { get; set; }
    public decimal CutlaryCharge { get; set; }
    public decimal GiftWrapCharge { get; set; }
    public decimal LowCartFeeCharge { get; set; }
    public decimal LowCartAmountValue { get; set; }
    public bool IsCodAvailable { get; set; }
    public decimal CommissionPerKm { get; set; }

    // Maintenance Mode
    public bool IsMaintenanceMode { get; set; }
    public DateTime? MaintenanceStartUtc { get; set; }
    public DateTime? MaintenanceEndUtc { get; set; }
    public string MaintenanceMessage { get; set; }

    // SpeedyMart Settings
    public bool IsSpeedyMartExpressEnabled { get; set; }
    public bool IsSpeedyMartNormalEnabled { get; set; }
    public int SpeedyMartExpressDeliveryMinMinutes { get; set; }
    public int SpeedyMartExpressDeliveryMaxMinutes { get; set; }
    public int SpeedyMartNormalDeliveryMinHours { get; set; }
    public int SpeedyMartNormalDeliveryMaxHours { get; set; }
    public decimal SpeedyMartExpressDeliveryCharge { get; set; }
    public decimal SpeedyMartNormalDeliveryCharge { get; set; }
    public decimal SpeedyMartExpressMinOrderAmount { get; set; }
    public decimal SpeedyMartNormalMinOrderAmount { get; set; }
}
```

---

### 4. SellerSettingsJson (Seller-Specific Settings)

```csharp
public class SellerSettingsJson
{
    public bool IsOrderPossible { get; set; }
    public bool IsFreeDelivery { get; set; }
    public bool IsCodAvailable { get; set; }
    public int MinRadiusInKms { get; set; }
    public int MaxRadiusInKms { get; set; }

    // LEGACY SURGE FIELDS (for backward compatibility)
    public bool IsSurge { get; set; }
    public int SurgeCount { get; set; } = 0;
    public decimal SurgeCharge { get; set; }

    // PLATFORM-SPECIFIC SURGE PRICING
    public SurgePricingConfig? SurgePricing { get; set; }

    public decimal MinDeliveryCharge { get; set; }
    public decimal DeliveryChargeAterMinRadius { get; set; }
    public decimal CommissionPerKm { get; set; }
}

public class SurgePricingConfig
{
    // Enable surge pricing per platform
    public bool EnableForHotBox { get; set; } = false;
    public bool EnableForSpeedyMartNormal { get; set; } = false;
    public bool EnableForSpeedyMartExpress { get; set; } = false;

    // Surge charges per platform
    public decimal HotBoxSurgeCharge { get; set; } = 0;
    public decimal SpeedyMartNormalSurgeCharge { get; set; } = 0;
    public decimal SpeedyMartExpressSurgeCharge { get; set; } = 0;

    // Threshold count for each platform
    public int HotBoxSurgeThreshold { get; set; } = 100;
    public int SpeedyMartNormalSurgeThreshold { get; set; } = 100;
    public int SpeedyMartExpressSurgeThreshold { get; set; } = 100;
}
```

---

### 5. DeliveryAvailabiltySM

```csharp
public class DeliveryAvailabiltySM
{
    public bool IsDeliveryAvailable { get; set; }
    public string? Address { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Pincode { get; set; }
    
    // SURGE RESPONSE FLAG
    public bool SurgeResponse { get; set; }
}
```

---

## API Endpoints

---

### 1. CREATE ORDER (With Surge Calculation)

**Endpoint:** `POST /api/v1/Order`

**Description:** Creates a new order. Automatically calculates and applies surge charge based on seller settings and platform type.

**Request Body:**
```json
{
  "reqData": {
    "OrderNumber": "ORD-20250521-001",
    "UserId": 123,
    "SellerId": 456,
    "Amount": 500.00,
    "DeliveryCharge": 30.00,
    "PlatformType": "HotBox",
    "DeliverySpeedType": 1,
    "AddressId": 789,
    "Items": [
      {
        "ProductVariantId": 101,
        "Quantity": 2,
        "UnitPrice": 250.00
      }
    ]
  }
}
```

**Success Response (200 OK):**
```json
{
  "successData": {
    "Id": 1001,
    "OrderNumber": "ORD-20250521-001",
    "UserId": 123,
    "SellerId": 456,
    "Amount": 550.00,
    "DueAmount": 550.00,
    "SurgeCharge": 20.00,
    "DeliveryCharge": 30.00,
    "PlatformType": "HotBox",
    "DeliverySpeedType": 1,
    "OrderStatus": "Created",
    "PaymentStatus": "Pending",
    "CreatedAt": "2025-05-21T15:30:00Z",
    "Items": [
      {
        "Id": 2001,
        "ProductVariantId": 101,
        "Quantity": 2,
        "UnitPrice": 250.00,
        "TotalPrice": 500.00
      }
    ]
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
    "Message": "Order must contain at least one item"
  }
}
```

---

### 2. GET ORDER BY ID

**Endpoint:** `GET /api/v1/Order/{id}`

**Description:** Retrieves order details including surge charge.

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | long | Yes | Order ID |

**Success Response (200 OK):**
```json
{
  "successData": {
    "Id": 1001,
    "OrderNumber": "ORD-20250521-001",
    "UserId": 123,
    "CustomerName": "John Doe",
    "SellerId": 456,
    "Amount": 550.00,
    "DueAmount": 550.00,
    "PaidAmount": 0.00,
    "RefundAmount": 0.00,
    "SurgeCharge": 20.00,
    "DeliveryCharge": 30.00,
    "PlatformCharge": 5.00,
    "CutlaryCharge": 0.00,
    "GiftWrapCharge": 0.00,
    "LowCartFeeCharge": 0.00,
    "TaxAmount": 25.00,
    "DiscountAmount": 0.00,
    "TipAmount": 0.00,
    "PlatformType": "HotBox",
    "DeliverySpeedType": 1,
    "OrderStatus": "Created",
    "PaymentStatus": "Pending",
    "PaymentMode": "Online",
    "Currency": "INR",
    "ItemCount": 1,
    "AddressId": 789,
    "ExpectedDeliveryDate": "2025-05-21T16:30:00Z",
    "CookingInstructions": null,
    "CreatedAt": "2025-05-21T15:30:00Z",
    "UpdatedAt": "2025-05-21T15:30:00Z",
    "SellerAcceptedAt": null,
    "DeliveryAddress": {
      "Id": 789,
      "Address": "123 Main Street",
      "City": "Mumbai",
      "State": "Maharashtra",
      "Pincode": "400001",
      "Latitude": 19.0760,
      "Longitude": 72.8777
    }
  },
  "isError": false,
  "errorData": null
}
```

---

### 3. GET ORDER INVOICE

**Endpoint:** `GET /api/v1/Order/Invoice/{orderId}`

**Description:** Retrieves detailed invoice with complete charge breakdown including surge.

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| orderId | long | Yes | Order ID |

**Success Response (200 OK):**
```json
{
  "successData": {
    "InvoiceNumber": "INV-20250521-001",
    "InvoiceDate": "2025-05-21T15:30:00Z",
    "OrderId": 1001,
    "OrderNumber": "ORD-20250521-001",
    "Seller": {
      "Id": 456,
      "Name": "Fresh Foods",
      "StoreName": "Fresh Foods - Andheri",
      "Email": "fresh@example.com",
      "Mobile": "9876543210",
      "Address": "Shop 12, Market Road",
      "City": "Mumbai",
      "State": "Maharashtra",
      "Country": "India",
      "FssaiLicNo": "12345678901234",
      "TaxName": "GST",
      "TaxNumber": "27AABCU9603R1ZM"
    },
    "Customer": {
      "Id": 123,
      "Name": "John Doe",
      "Mobile": "9876543210",
      "Email": "john@example.com"
    },
    "DeliveryAddress": {
      "Name": "John Doe",
      "Mobile": "9876543210",
      "Address": "123 Main Street, Apt 4B",
      "Landmark": "Near City Mall",
      "Area": "Andheri West",
      "Pincode": "400053",
      "City": "Mumbai",
      "State": "Maharashtra",
      "Country": "India"
    },
    "Items": [
      {
        "ProductVariantId": 101,
        "BaseProductName": "Paneer Tikka",
        "VariantName": "Full Plate",
        "Indicator": "🟢",
        "Quantity": 2,
        "UnitPrice": 250.00,
        "TotalPrice": 500.00,
        "Toppings": null,
        "ToppingsTotal": 0.00,
        "Addons": null,
        "AddonsTotal": 0.00,
        "LineTotal": 500.00
      }
    ],
    "Subtotal": 500.00,
    "DeliveryCharge": 30.00,
    "PlatformCharge": 5.00,
    "CutleryCharge": 0.00,
    "GiftWrapCharge": 0.00,
    "LowCartFeeCharge": 0.00,
    "SurgeCharge": 20.00,
    "TaxAmount": 25.00,
    "DiscountAmount": 0.00,
    "PromoCode": null,
    "TipAmount": 0.00,
    "TotalAmount": 580.00,
    "PaidAmount": 0.00,
    "DueAmount": 580.00,
    "Currency": "INR",
    "PaymentMode": "Online",
    "PaymentStatus": "Pending",
    "OrderStatus": "Created",
    "TransactionId": 5001,
    "RazorpayPaymentId": null
  },
  "isError": false,
  "errorData": null
}
```

---

### 4. GET GLOBAL SETTINGS (Admin)

**Endpoint:** `GET /api/v1/Settings`

**Description:** Retrieves global platform settings including legacy surge configuration.

**Success Response (200 OK):**
```json
{
  "successData": {
    "Id": 1,
    "IsOrderPossible": true,
    "IsFreeDelivery": false,
    "IsSurge": true,
    "SurgeCount": 50,
    "SurgeCharge": 20.00,
    "DeliveryInMinutes": 45,
    "ReferralPercentage": 10,
    "PlatormCharge": 5.00,
    "CutlaryCharge": 10.00,
    "GiftWrapCharge": 25.00,
    "LowCartFeeCharge": 20.00,
    "LowCartAmountValue": 200.00,
    "IsCodAvailable": true,
    "CommissionPerKm": 8.00,
    "IsMaintenanceMode": false,
    "MaintenanceStartUtc": null,
    "MaintenanceEndUtc": null,
    "MaintenanceMessage": "",
    "IsSpeedyMartExpressEnabled": true,
    "IsSpeedyMartNormalEnabled": true,
    "SpeedyMartExpressDeliveryMinMinutes": 30,
    "SpeedyMartExpressDeliveryMaxMinutes": 60,
    "SpeedyMartNormalDeliveryMinHours": 4,
    "SpeedyMartNormalDeliveryMaxHours": 24,
    "SpeedyMartExpressDeliveryCharge": 50.00,
    "SpeedyMartNormalDeliveryCharge": 30.00,
    "SpeedyMartExpressMinOrderAmount": 500.00,
    "SpeedyMartNormalMinOrderAmount": 200.00
  },
  "isError": false,
  "errorData": null
}
```

---

### 5. UPDATE GLOBAL SETTINGS (Admin)

**Endpoint:** `PUT /api/v1/Settings`

**Description:** Updates global platform settings including surge configuration.

**Request Body:**
```json
{
  "reqData": {
    "IsOrderPossible": true,
    "IsFreeDelivery": false,
    "IsSurge": true,
    "SurgeCount": 100,
    "SurgeCharge": 25.00,
    "DeliveryInMinutes": 45,
    "ReferralPercentage": 10,
    "PlatormCharge": 5.00,
    "CutlaryCharge": 10.00,
    "GiftWrapCharge": 25.00,
    "LowCartFeeCharge": 20.00,
    "LowCartAmountValue": 200.00,
    "IsCodAvailable": true,
    "CommissionPerKm": 8.00
  }
}
```

**Success Response (200 OK):**
```json
{
  "successData": {
    "Id": 1,
    "IsOrderPossible": true,
    "IsFreeDelivery": false,
    "IsSurge": true,
    "SurgeCount": 100,
    "SurgeCharge": 25.00,
    "DeliveryInMinutes": 45,
    "ReferralPercentage": 10,
    "PlatormCharge": 5.00,
    "CutlaryCharge": 10.00,
    "GiftWrapCharge": 25.00,
    "LowCartFeeCharge": 20.00,
    "LowCartAmountValue": 200.00,
    "IsCodAvailable": true,
    "CommissionPerKm": 8.00,
    "IsMaintenanceMode": false,
    "MaintenanceStartUtc": null,
    "MaintenanceEndUtc": null,
    "MaintenanceMessage": ""
  },
  "isError": false,
  "errorData": null
}
```

---

### 6. GET SELLER SETTINGS

**Endpoint:** `GET /api/v1/Seller/Settings`

**Description:** Retrieves seller-specific settings including platform-specific surge pricing configuration.

**Headers:**
```
Authorization: Bearer {seller_jwt_token}
```

**Success Response (200 OK):**
```json
{
  "successData": {
    "SellerId": 456,
    "IsOrderPossible": true,
    "IsFreeDelivery": false,
    "IsCodAvailable": true,
    "MinRadiusInKms": 5,
    "MaxRadiusInKms": 15,
    "MinDeliveryCharge": 30.00,
    "DeliveryChargeAterMinRadius": 10.00,
    "CommissionPerKm": 8.00,
    
    // Legacy Surge Settings
    "IsSurge": false,
    "SurgeCount": 0,
    "SurgeCharge": 0.00,
    
    // Platform-Specific Surge Pricing
    "SurgePricing": {
      "EnableForHotBox": true,
      "EnableForSpeedyMartNormal": true,
      "EnableForSpeedyMartExpress": true,
      
      "HotBoxSurgeCharge": 20.00,
      "SpeedyMartNormalSurgeCharge": 15.00,
      "SpeedyMartExpressSurgeCharge": 30.00,
      
      "HotBoxSurgeThreshold": 100,
      "SpeedyMartNormalSurgeThreshold": 80,
      "SpeedyMartExpressSurgeThreshold": 50
    }
  },
  "isError": false,
  "errorData": null
}
```

---

### 7. UPDATE SELLER SETTINGS

**Endpoint:** `PUT /api/v1/Seller/Settings`

**Description:** Updates seller-specific settings including platform-specific surge pricing.

**Headers:**
```
Authorization: Bearer {seller_jwt_token}
```

**Request Body:**
```json
{
  "reqData": {
    "IsOrderPossible": true,
    "IsFreeDelivery": false,
    "IsCodAvailable": true,
    "MinRadiusInKms": 5,
    "MaxRadiusInKms": 15,
    "MinDeliveryCharge": 30.00,
    "DeliveryChargeAterMinRadius": 10.00,
    "CommissionPerKm": 8.00,
    
    // Legacy fields (optional, for backward compatibility)
    "IsSurge": false,
    "SurgeCount": 0,
    "SurgeCharge": 0.00,
    
    // Platform-Specific Surge Pricing
    "SurgePricing": {
      "EnableForHotBox": true,
      "EnableForSpeedyMartNormal": true,
      "EnableForSpeedyMartExpress": true,
      
      "HotBoxSurgeCharge": 25.00,
      "SpeedyMartNormalSurgeCharge": 20.00,
      "SpeedyMartExpressSurgeCharge": 35.00,
      
      "HotBoxSurgeThreshold": 100,
      "SpeedyMartNormalSurgeThreshold": 80,
      "SpeedyMartExpressSurgeThreshold": 50
    }
  }
}
```

**Success Response (200 OK):**
```json
{
  "successData": {
    "SellerId": 456,
    "IsOrderPossible": true,
    "IsFreeDelivery": false,
    "IsCodAvailable": true,
    "MinRadiusInKms": 5,
    "MaxRadiusInKms": 15,
    "MinDeliveryCharge": 30.00,
    "DeliveryChargeAterMinRadius": 10.00,
    "CommissionPerKm": 8.00,
    "IsSurge": false,
    "SurgeCount": 0,
    "SurgeCharge": 0.00,
    "SurgePricing": {
      "EnableForHotBox": true,
      "EnableForSpeedyMartNormal": true,
      "EnableForSpeedyMartExpress": true,
      "HotBoxSurgeCharge": 25.00,
      "SpeedyMartNormalSurgeCharge": 20.00,
      "SpeedyMartExpressSurgeCharge": 35.00,
      "HotBoxSurgeThreshold": 100,
      "SpeedyMartNormalSurgeThreshold": 80,
      "SpeedyMartExpressSurgeThreshold": 50
    }
  },
  "isError": false,
  "errorData": null
}
```

---

### 8. CHECK DELIVERY AVAILABILITY

**Endpoint:** `POST /api/v1/User/DeliveryAvailability`

**Description:** Checks if delivery is available at a location and returns surge status.

**Request Body:**
```json
{
  "reqData": {
    "SellerId": 456,
    "Address": "123 Main Street, Mumbai",
    "Latitude": 19.0760,
    "Longitude": 72.8777,
    "Pincode": "400053",
    "PlatformType": "HotBox"
  }
}
```

**Success Response (200 OK):**
```json
{
  "successData": {
    "IsDeliveryAvailable": true,
    "Address": "123 Main Street, Mumbai",
    "Latitude": 19.0760,
    "Longitude": 72.8777,
    "Pincode": "400053",
    "SurgeResponse": true
  },
  "isError": false,
  "errorData": null
}
```

---

### 9. GET SELLER ORDERS (For Dashboard)

**Endpoint:** `GET /api/v1/Seller/mine/orders`

**Description:** Retrieves all orders for a seller including surge charge details.

**Headers:**
```
Authorization: Bearer {seller_jwt_token}
```

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 20) |
| status | string | No | Filter by order status |
| platformType | string | No | Filter by platform (HotBox/SpeedyMart) |

**Success Response (200 OK):**
```json
{
  "successData": {
    "Items": [
      {
        "Id": 1001,
        "OrderNumber": "ORD-20250521-001",
        "UserId": 123,
        "CustomerName": "John Doe",
        "Amount": 550.00,
        "DueAmount": 550.00,
        "SurgeCharge": 20.00,
        "DeliveryCharge": 30.00,
        "PlatformType": "HotBox",
        "DeliverySpeedType": 1,
        "OrderStatus": "Pending",
        "PaymentStatus": "Pending",
        "CreatedAt": "2025-05-21T15:30:00Z",
        "ExpectedDeliveryDate": "2025-05-21T16:30:00Z"
      },
      {
        "Id": 1002,
        "OrderNumber": "ORD-20250521-002",
        "UserId": 124,
        "CustomerName": "Jane Smith",
        "Amount": 850.00,
        "DueAmount": 850.00,
        "SurgeCharge": 30.00,
        "DeliveryCharge": 50.00,
        "PlatformType": "SpeedyMart",
        "DeliverySpeedType": 2,
        "OrderStatus": "Accepted",
        "PaymentStatus": "Paid",
        "CreatedAt": "2025-05-21T14:00:00Z",
        "ExpectedDeliveryDate": "2025-05-21T15:00:00Z"
      }
    ],
    "TotalCount": 156,
    "Page": 1,
    "PageSize": 20
  },
  "isError": false,
  "errorData": null
}
```

---

## Surge Charge Calculation Logic

### Algorithm (OrderProcess.cs)

```csharp
public async Task<decimal> CalculateSurgeCharge(
    long? sellerId, 
    PlatformTypeDM platformType, 
    int deliverySpeedType)
{
    if (!sellerId.HasValue || sellerId.Value <= 0)
        return 0;

    // Get seller settings
    var sellerSettings = await _apiDbContext.SellerSettings
        .AsNoTracking()
        .FirstOrDefaultAsync(s => s.SellerId == sellerId.Value);

    if (sellerSettings == null || string.IsNullOrEmpty(sellerSettings.JsonData))
        return 0;

    var settings = JsonSerializer.Deserialize<SellerSettingsJson>(
        sellerSettings.JsonData, _jsonOptions);
    
    if (settings == null)
        return 0;

    // PRIORITY 1: New platform-specific surge config
    if (settings.SurgePricing != null)
    {
        // HotBox Platform
        if (platformType == PlatformTypeDM.HotBox && 
            settings.SurgePricing.EnableForHotBox)
        {
            return settings.SurgePricing.HotBoxSurgeCharge;
        }
        // SpeedyMart Platform
        else if (platformType == PlatformTypeDM.SpeedyMart)
        {
            // Express Delivery (Type = 2)
            if (deliverySpeedType == 2 && 
                settings.SurgePricing.EnableForSpeedyMartExpress)
            {
                return settings.SurgePricing.SpeedyMartExpressSurgeCharge;
            }
            // Normal Delivery (Type = 1)
            else if (deliverySpeedType == 1 && 
                settings.SurgePricing.EnableForSpeedyMartNormal)
            {
                return settings.SurgePricing.SpeedyMartNormalSurgeCharge;
            }
        }
    }
    
    // PRIORITY 2: Legacy surge settings (backward compatibility)
    else if (settings.IsSurge)
    {
        return settings.SurgeCharge;
    }

    return 0;
}
```

---

## Surge Application Flow

### Order Creation Flow

1. **User initiates order** → System validates cart and address
2. **Store hours validation** → Check if seller is open
3. **Calculate order total** → Sum of items + taxes
4. **Calculate surge charge** → Based on platform type and delivery speed
5. **Add surge to order**:
   ```csharp
   orderDM.SurgeCharge = surgeCharge;
   if (surgeCharge > 0)
   {
       orderDM.Amount += surgeCharge;
       orderDM.DueAmount += surgeCharge;
   }
   ```
6. **Save order** → Order created with surge charge recorded
7. **Return response** → User sees total including surge

---

## Platform-Specific Surge Configuration

### HotBox (Food Delivery)
| Setting | Value | Description |
|---------|-------|-------------|
| EnableForHotBox | boolean | Enable/disable surge for food delivery |
| HotBoxSurgeCharge | decimal | Fixed surge amount (₹) |
| HotBoxSurgeThreshold | int | Order count threshold (legacy) |

### SpeedyMart Normal (Grocery - Standard)
| Setting | Value | Description |
|---------|-------|-------------|
| EnableForSpeedyMartNormal | boolean | Enable/disable surge for normal delivery |
| SpeedyMartNormalSurgeCharge | decimal | Fixed surge amount (₹) |
| SpeedyMartNormalSurgeThreshold | int | Order count threshold (legacy) |

### SpeedyMart Express (Grocery - Fast)
| Setting | Value | Description |
|---------|-------|-------------|
| EnableForSpeedyMartExpress | boolean | Enable/disable surge for express delivery |
| SpeedyMartExpressSurgeCharge | decimal | Fixed surge amount (₹) |
| SpeedyMartExpressSurgeThreshold | int | Order count threshold (legacy) |

---

## Error Codes

| Error Code | HTTP Status | Description |
|------------|-------------|-------------|
| InvalidInputData_NoLog | 400 | Missing or invalid input data |
| InvalidInputData_Log | 400 | Invalid input with logging |
| Fatal_Log | 500 | Critical error with logging |
| Unauthorized | 401 | Invalid or missing JWT token |
| Forbidden | 403 | Insufficient permissions |
| NotFound | 404 | Resource not found |

---

## Files Modified Summary

### Database
- `20260520160000_AddSurgeChargeToOrders.cs` - Migration

### Domain Models
- `OrderDM.cs` - Added `SurgeCharge` property

### Service Models
- `OrderSM.cs` - Added `SurgeCharge` field
- `OrderInvoiceSM.cs` - Added `SurgeCharge` in invoice
- `SettingsSM.cs` - Added legacy surge fields
- `SellerSettingsJson.cs` - Added `SurgePricingConfig`
- `DeliveryAvailabiltySM.cs` - Added `SurgeResponse`

### Business Logic
- `OrderProcess.cs` - Added `CalculateSurgeCharge()` method
- `OrderProcess.cs` - Integrated surge in `CreateOrderAsync()`

---

## Migration Notes

### Backward Compatibility
- Legacy `IsSurge` + `SurgeCharge` fields still work
- New `SurgePricing` config takes precedence if present
- All existing orders have `SurgeCharge = 0` (default)

### Database Rollback
```sql
ALTER TABLE orders DROP COLUMN IF EXISTS surge_charge;
```

---

## API Versioning

**Current Version:** v1  
**Base URL:** `http://localhost:5050/api/v1`  
**Content-Type:** `application/json`  
**Auth:** JWT Bearer Token

---

*Documentation generated for SpeedyCart/HotBox Surge Pricing Feature*  
*Last updated: May 21, 2026*
