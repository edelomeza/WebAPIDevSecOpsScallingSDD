namespace WebAPIDevSecOpsScallingSDD.Dtos
{
    public sealed class ErrorResponse
    {
        public string Error { get; set; } = string.Empty;

        public int Status { get; set; }

        public string TraceId { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? Detail { get; set; }
    }
}
