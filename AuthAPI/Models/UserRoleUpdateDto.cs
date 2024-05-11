using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthAPI.Models
{
    public class UserRolesUpdateDto
{
    public List<string> RolesToAdd { get; set; }
    public List<string> RolesToRemove { get; set; }
}
}