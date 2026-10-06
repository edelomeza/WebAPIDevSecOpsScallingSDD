namespace WebAPIDevSecOpsScallingSDD.Models
{
    public interface IConcurrenteAuditable
    {
        byte[] RowVersion { get; set; }
    }
}
