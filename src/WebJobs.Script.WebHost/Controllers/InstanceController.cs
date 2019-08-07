// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Script.WebHost.Management;
using Microsoft.Azure.WebJobs.Script.WebHost.Models;
using Microsoft.Azure.WebJobs.Script.WebHost.Security.Authorization.Policies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Azure.WebJobs.Script.WebHost.Controllers
{
    /// <summary>
    /// Controller responsible for instance operations that are orthogonal to the script host.
    /// An instance is an unassigned generic container running with the runtime in standby mode.
    /// These APIs are used by the AppService Controller to validate standby instance status and info.
    /// </summary>
    public class InstanceController : Controller
    {
        private readonly IEnvironment _environment;
        private readonly IInstanceManager _instanceManager;
        private readonly ILogger _logger;

        public InstanceController(IEnvironment environment, IInstanceManager instanceManager, ILoggerFactory loggerFactory)
        {
            _environment = environment;
            _instanceManager = instanceManager;
            _logger = loggerFactory.CreateLogger<InstanceController>();
        }

        [HttpPost]
        [Route("admin/instance/assign")]
        [Authorize(Policy = PolicyNames.AdminAuthLevel)]
        public async Task<IActionResult> Assign([FromBody] EncryptedHostAssignmentContext encryptedAssignmentContext)
        {
            _logger.LogDebug("Request url is: " + Request.Host + Request.Path);
            _logger.LogDebug("IsWarmup " + encryptedAssignmentContext.IsWarmup);
            foreach (var header in Request.Headers)
            {
                _logger.LogDebug("Header is " + header.Key + " " + header.Value);
            }

           // _logger.LogDebug("request string is: " + Request.)
            if (encryptedAssignmentContext.EncryptedContext == null)
            {
                StringValues siteName;
                Request.Headers.TryGetValue("sf-handlertrace-siteName", out siteName);
                _logger.LogError("Encrypted Assignment context is null for " + siteName.ToString());
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            else
            {
                StringValues siteName;
                Request.Headers.TryGetValue("sf-handlertrace-siteName", out siteName);
                _logger.LogInformation("Encrypted Assignment context is not null" + encryptedAssignmentContext.EncryptedContext + "  for " + siteName.ToString());
            }

            var containerKey = _environment.GetEnvironmentVariable(EnvironmentSettingNames.ContainerEncryptionKey);
            var assignmentContext = encryptedAssignmentContext.IsWarmup
                ? null
                : encryptedAssignmentContext.Decrypt(containerKey);

            // before starting the assignment we want to perform as much
            // up front validation on the context as possible
            string error = await _instanceManager.ValidateContext(assignmentContext, encryptedAssignmentContext.IsWarmup);
            if (error != null)
            {
                return StatusCode(StatusCodes.Status400BadRequest, error);
            }

            // Wait for Sidecar specialization to complete before returning ok.
            // This shouldn't take too long so ok to do this sequentially.
            error = await _instanceManager.SpecializeMSISidecar(assignmentContext, encryptedAssignmentContext.IsWarmup);
            if (error != null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, error);
            }

            var result = _instanceManager.StartAssignment(assignmentContext, encryptedAssignmentContext.IsWarmup);

            return result || encryptedAssignmentContext.IsWarmup
                ? Accepted()
                : StatusCode(StatusCodes.Status409Conflict, "Instance already assigned");
        }

        [HttpGet]
        [Route("admin/instance/info")]
        [Authorize(Policy = PolicyNames.AdminAuthLevel)]
        public IActionResult GetInstanceInfo()
        {
            return Ok(_instanceManager.GetInstanceInfo());
        }
    }
}
