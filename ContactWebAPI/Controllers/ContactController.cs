using ContactWebAPI.Domain.Models;
using ContactWebAPI.SqlServer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContactWebAPI.Controllers
{
    [ApiController]
    [Route("[Controller]")]
    [Authorize(Roles = UserRoles.Admin)]
    public class ContactController : ControllerBase
    {
        [HttpGet]
        public JsonResult GetContact()
        {
            var contacts = new List<Contact>
            {
                new Contact
                {
                    ContactId= 1, FirstName = "Jane", LastName = "Smith", MiddleName = "John"
                }
            };

            var result = new JsonResult(contacts);
            return result;
            
        }

        [HttpGet("all")]
        public ActionResult<IEnumerable<Contact>> GetContacts()
        {
            var contacts = new List<Contact>
            {
                new Contact
                {
                    ContactId= 1, FirstName = "Jane", LastName = "Smith", MiddleName = "John"
                }
            };
            return Ok(contacts);

        }
    }
}
