using System;

namespace ImportadoraSonib.Services
{
    public static class WhatsappLinkService
    {
        private const string PhoneNumber = "593992856725"; 

        public static string BuildLink(string productName, int productId, decimal price)
        {
            var msg = Uri.EscapeDataString($"Hola, quiero comprar: {productName} (ID {productId}) a USD {price:F2}");
            return $"https://wa.me/{PhoneNumber}?text={msg}";
        }
    }
}
