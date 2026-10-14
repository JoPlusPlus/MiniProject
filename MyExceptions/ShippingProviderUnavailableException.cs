namespace Mini_Project___Order_Processing_System.MyExceptions
{
    public class ShippingProviderUnavailableException : Exception 
    {
        public ShippingProviderUnavailableException() : base("Shipping provider unavailable")
        {
        }
    }
}
