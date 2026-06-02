using System.Threading.Tasks;
using CambioDivisasP2P.CORE.Core.DTOs;

namespace CambioDivisasP2P.API.Interfaces
{
    public interface IAuthService
    {
        Task<ServiceResult<bool>> RegisterAsync(UsuarioRegistroDTO model);
        Task<ServiceResult<UsuarioDTO>> LoginAsync(LoginDTO model);
    }
}