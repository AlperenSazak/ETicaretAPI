using ETicaretAPI.Application.Features.Commands.AuthorizationEndpoint.AssingRoleEndpoint;
using ETicaretAPI.Application.Features.Queries.AuthorizationEndpoint.GetRolesToEndpoints;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ETicaretAPI.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthorizationEndpointsController : ControllerBase
    {
        readonly IMediator _mediator;

        public AuthorizationEndpointsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("[Action]")]

        public async Task<IActionResult> GetRolesToEndpoint(GetRolesToEndpointQueryRequest rolesToEndpointQueryRequest)
        {
            GetRolesToEndpointQueryResponse response = await _mediator.Send(rolesToEndpointQueryRequest);
            return Ok(response);
        }

        [HttpPost]

        public async Task<IActionResult> AssingRoleEndpoint(AssingRoleEndpointCommandRequest assingRoleEndpointCommandRequest)
        {
            assingRoleEndpointCommandRequest.Type = typeof(Program);
            AssingRoleEndpointCommandResponse response = await _mediator.Send(assingRoleEndpointCommandRequest);
            return Ok(response);
        }
    }
}
