namespace Laboratorio.Api.Security
{
    public static class Roles
    {
        public const string SuperAdmin = "SuperAdmin";
        public const string Administrador = "Administrador";
        public const string Bioanalista = "Bioanalista";
        public const string Secretaria = "Secretaria";
        public const string Cajero = "Cajero";

        public static readonly string[] Todos = { SuperAdmin, Administrador, Bioanalista, Secretaria, Cajero };
    }
}
