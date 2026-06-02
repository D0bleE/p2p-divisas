using System.Collections.Generic;
using System.Threading.Tasks;
using CambioDivisasP2P.CORE.Core.DTOs;

namespace CambioDivisasP2P.API.Interfaces
{
	public interface IOfertaService
	{
		Task<ServiceResult<int>> CrearOfertaAsync(OfertaCreateDTO model);
		Task<List<OfertaDetalleDTO>> ObtenerPizarraMercadoAsync();
	}
}