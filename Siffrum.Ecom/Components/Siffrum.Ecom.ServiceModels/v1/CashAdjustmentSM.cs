namespace Siffrum.Ecom.ServiceModels.v1
{
    /// <summary>
    /// Request to adjust/correct a delivery boy's COD balance
    /// </summary>
    public class CashAdjustmentSM
    {
        public long DeliveryBoyId { get; set; }
        
        /// <summary>
        /// Adjustment amount. Positive = add to balance (reduce negative), Negative = deduct from balance
        /// </summary>
        public decimal AdjustmentAmount { get; set; }
        
        /// <summary>
        /// Reason for the adjustment (e.g., "Incorrect calculation", "Refund processed", "System error correction")
        /// </summary>
        public string Reason { get; set; } = string.Empty;
        
        /// <summary>
        /// Current balance before adjustment (for audit trail)
        /// </summary>
        public decimal CurrentBalance { get; set; }
    }
}
