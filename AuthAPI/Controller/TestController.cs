using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace AuthAPI.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize]
    public class TestController : ControllerBase
    {
        public TestController()
        {

        }
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            return Ok("salut");
        }
    }
}