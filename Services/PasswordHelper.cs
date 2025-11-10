using BCrypt.Net;

namespace BodegApp.Backend.Services // 💡 ESTE DEBE SER EL NAMESPACE CORRECTO
{
    /// <summary>
    /// Helper estático para el hashing y verificación de contraseñas usando BCrypt.
    /// </summary>
    public static class PasswordHelper
    {
        /// <summary>
        /// Hashea la contraseña proporcionada usando BCrypt.
        /// </summary>
        /// <param name="password">La contraseña en texto plano.</param>
        /// <returns>La contraseña hasheada.</returns>
        public static string Hash(string password)
        {
            // Genera el hash de la contraseña de forma segura.
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        /// <summary>
        /// Verifica una contraseña en texto plano contra un hash almacenado.
        /// </summary>
        /// <param name="password">La contraseña en texto plano introducida por el usuario.</param>
        /// <param name="hash">El hash almacenado en la base de datos.</param>
        /// <returns>True si la contraseña coincide con el hash, False en caso contrario.</returns>
        public static bool Verify(string password, string hash)
        {
            // Compara la contraseña con el hash.
            // Esto resuelve el error CS0117
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
    }
}