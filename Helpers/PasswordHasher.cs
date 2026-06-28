using System.Security.Cryptography;
using System.Text;

namespace QuanLyTaiChinhCaNhan_Nhom06.Helpers
{
    public static class PasswordHasher
    {
        private const string Algorithm = "PBKDF2-SHA256";
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 100_000;

        public static string Hash(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var key = Rfc2898DeriveBytes.Pbkdf2(
                password ?? string.Empty,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                KeySize);

            return string.Join(
                "$",
                Algorithm,
                Iterations,
                Convert.ToBase64String(salt),
                Convert.ToBase64String(key));
        }

        public static bool Verify(string password, string hash)
        {
            if (string.IsNullOrWhiteSpace(hash))
                return false;

            if (hash.StartsWith($"{Algorithm}$", StringComparison.Ordinal))
                return VerifyPbkdf2(password, hash);

            return VerifyLegacySha256(password, hash);
        }

        public static bool NeedsUpgrade(string hash)
        {
            return !string.IsNullOrWhiteSpace(hash)
                && !hash.StartsWith($"{Algorithm}$", StringComparison.Ordinal);
        }

        private static bool VerifyPbkdf2(string password, string hash)
        {
            var parts = hash.Split('$');
            if (parts.Length != 4 || parts[0] != Algorithm || !int.TryParse(parts[1], out var iterations))
                return false;

            try
            {
                var salt = Convert.FromBase64String(parts[2]);
                var expectedKey = Convert.FromBase64String(parts[3]);
                var actualKey = Rfc2898DeriveBytes.Pbkdf2(
                    password ?? string.Empty,
                    salt,
                    iterations,
                    HashAlgorithmName.SHA256,
                    expectedKey.Length);

                return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static bool VerifyLegacySha256(string password, string hash)
        {
            try
            {
                using var sha256 = SHA256.Create();
                var actualHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(password ?? string.Empty));
                var expectedHash = Convert.FromBase64String(hash);
                return actualHash.Length == expectedHash.Length
                    && CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
