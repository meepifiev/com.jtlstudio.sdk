namespace JTLStudio.SDK
{
    public readonly struct ProductData
    {
        public ProductData(string id, decimal price, string currency, string formatted)
        {
            Id = id;
            Price = price;
            Currency = currency;
            Formatted = formatted;
        }

        public string Id { get; }
        public decimal Price { get; }
        public string Currency { get; }
        public string Formatted { get; }

        public bool IsValid => string.IsNullOrEmpty(Id) == false;
    }
}
