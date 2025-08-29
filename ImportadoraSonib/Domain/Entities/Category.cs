namespace ImportadoraSonib.Domain.Entities
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string Slug { get; set; } = default!;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
