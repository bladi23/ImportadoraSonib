namespace ImportadoraSonib.Services;

public static class WhatsappLinkService
{
    private const string PhoneNumber = "593992856725"; // EC

    public static string BuildLink(string productName, int productId, decimal price)
    {
        var msg = Uri.EscapeDataString($"Hola, quiero comprar: {productName} (ID {productId}) a USD {price:F2}");
        return $"https://wa.me/{PhoneNumber}?text={msg}";
    }

    public static string BuildOrderLink(int orderId, IEnumerable<(string Name,int Qty, decimal Price)> lines, decimal total)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Hola, quiero confirmar la orden #{orderId}:");
        foreach (var l in lines)
            sb.AppendLine($"- {l.Name} x{l.Qty} @ {l.Price:F2}");
        sb.AppendLine($"Total: USD {total:F2}");
        var msg = Uri.EscapeDataString(sb.ToString());
        return $"https://wa.me/{PhoneNumber}?text={msg}";
    }
}
