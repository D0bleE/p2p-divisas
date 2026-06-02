using System.Collections.Generic;
using System.Threading.Tasks;
using CambioDivisasP2P.CORE.Core.DTOs;

namespace CambioDivisasP2P.API.Interfaces
{
    public interface IBilleteraService
    {
        Task<ServiceResult<bool>> RecargarFondosAsync(BilleteraOperacionDTO model);
        Task<List<BilleteraSaldoDTO>> ObtenerSaldosUsuarioAsync(int usuarioId);
    }
}