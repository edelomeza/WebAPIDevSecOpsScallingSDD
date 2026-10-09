namespace WebAPIDevSecOpsScallingSDD.Services
{
    /// <summary>Opciones de hashing Argon2id (04-02). Sin secretos: solo parámetros de coste.</summary>
    public sealed class PasswordHasherOptions
    {
        public int MemoryKBytes { get; set; } = 65536;

        public int Iterations { get; set; } = 3;
    }
}
