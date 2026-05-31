namespace Siffrum.Ecom.ServiceModels.v1
{
    public class DeliveryBoyOrderDetailsSM
    {
        public string CustomerName { get; set; }
        public string UserMobile { get; set; }
        public OrderSM OrderDetails { get; set; }
        public List<OrderItemDetailSM> OrderItems { get; set; }
        public DeliverySM DeliveryDetails { get; set; }
        public DeliveryAddressInfoSM DeliveryAddress { get; set; }
        public SellerPickupInfoSM SellerInfo { get; set; }
    }

    public class DeliveryAddressInfoSM
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string Landmark { get; set; }
        public string Area { get; set; }
        public string Pincode { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    public class SellerPickupInfoSM
    {
        public long SellerId { get; set; }
        public string StoreName { get; set; }
        public string Mobile { get; set; }
        public string Address { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
    }
}
