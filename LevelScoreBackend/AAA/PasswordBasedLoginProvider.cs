using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LevelScoreBackend.AAA
{
    public class PasswordBasedLoginProvider : ILoginProvider, IDisposable
    {
        public const string SERVER_START_TIME_CLAIM_TYPE = "serverStartTime";
        public const string IS_ADMIN_CLAIM_TYPE = "admin";

        public bool LogIn(string providedPassword, out List<Claim> claims)
        {
            claims = new List<Claim>();

            if (string.IsNullOrEmpty(providedPassword))
            {
                return false;
            }

            if (Program.AdminPassword.Equals(providedPassword))
            {
                claims.Add(new Claim(SERVER_START_TIME_CLAIM_TYPE, Program.StartTime.ToString()));
                claims.Add(new Claim(IS_ADMIN_CLAIM_TYPE, "yes"));
                return true;
            }

            if (Program.LeiterPassword.Equals(providedPassword))
            {
                claims.Add(new Claim(SERVER_START_TIME_CLAIM_TYPE, Program.StartTime.ToString()));
                return true;
            }

            return false;
        }

        public void Dispose() { }
    }
}
