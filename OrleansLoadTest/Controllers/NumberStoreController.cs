using Orleans;
using Microsoft.AspNetCore.Mvc;
using LoadTest.Grains.Interfaces;
using LoadTest.Grains.Interfaces.Models;
using LoadTest.SharedBase.Models;


namespace OrleansLoadTest.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NumberStoreController : ControllerBase
    {
        private readonly ILogger<NumberStoreController> _logger;
        private readonly IClusterClient _clusterClient;

        public NumberStoreController(
            ILogger<NumberStoreController> logger,
            IClusterClient clusterClient)
        {
            _logger = logger;
            _clusterClient = clusterClient;
        }

        [HttpGet(Name = "Get")]
        public async Task<ActionResult<NumberInfo>> Get(int grainId)
        {
            try
            {
                var numGrain = this._clusterClient.GetGrain<INumberStoreGrain>(grainId);
                var state = await numGrain.GetState();
                return Ok(state);
            }
            catch (Exception ex)
            {
                _logger.LogError(new Exception(), ex.ToString());
            }

            return DefaultProblem();
        }

        [HttpPost(Name = "Post")]
        public async Task<ActionResult> Post([FromBody] NumberAndGrainPost data)
        {
            try
            {
                var numGrain = this._clusterClient.GetGrain<INumberStoreGrain>(data.GrainId);
                await numGrain.UpdateNumberInfo(new NumberInfo(data.Number));
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(new Exception(), ex.ToString());
            }

            return DefaultProblem();
        }


        private ObjectResult DefaultProblem()
        {
            return Problem("An error Occurred", "TDB", 500, "Internal Server Error");
        }


    }
}