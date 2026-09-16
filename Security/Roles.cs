namespace Laboratorio.Api.Security
{
    public static class Roles
    {
        public const string Administrador = "Administrador";
        public const string Bioanalista = "Bioanalista";
        public const string Secretaria = "Secretaria";
        public const string Cajero = "Cajero";

        public static readonly string[] Todos = { Administrador, Bioanalista, Secretaria, Cajero };
    }
}
