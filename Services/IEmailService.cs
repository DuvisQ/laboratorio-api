using System.Threading.Tasks;

namespace Laboratorio.Api.Services
{
    public interface IEmailService
    {
        Task<bool> EnviarCodigoOtpAsync(string emailDestino, string nombreUsuario, string codigoOtp);
    }
}
