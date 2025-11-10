using System.ComponentModel.DataAnnotations;


namespace BodegApp.Backend.DTOs
{
    public class RegisterRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string NombreEmpresa { get; set; } = string.Empty;
        public string TipoNegocio { get; set; } = string.Empty;

    }
}
