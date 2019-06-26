using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LevelScoreBackend.AAA
{
    public interface ILoginProvider
    {
        bool LogIn(string providedPassword, out List<Claim> claims);
    }
}
