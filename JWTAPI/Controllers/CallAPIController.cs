using JWTAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace JWTAPI.Controllers
{
    public class CallAPIController : Controller
    {
        public IActionResult Index(string message)
        {
            ViewBag.Message = message;
            return View();
        }

        [HttpPost]
        public IActionResult Index(string username, string password)
        {
            if ((username != "secret") || (password != "secret"))
                return View((object)"Login Failed");

            var accessToken = GenerateJSONWebToken();

            /*
            // The claims code
            
            Users loginUser = CreateDummyUsers().Where(a => a.Username == username && a.Password == password).FirstOrDefault();
            if (loginUser == null)
                return View((object)"Login Failed");

            var claims = new[] { new Claim(ClaimTypes.Role, loginUser.Role) };

            var accessToken = GenerateJSONWebToken(claims);*/

            SetJWTCookie(accessToken);

            return RedirectToAction("FlightReservation");
        }

        private string GenerateJSONWebToken()
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("MynameisJamesBond007_MynameisJamesBond007"));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "https://www.yogihosting.com",
                audience: "dotnetclient",
                expires: DateTime.Now.AddHours(3),
                signingCredentials: credentials
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /*private string GenerateJSONWebToken(Claim[] claims)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("MynameisJamesBond007_MynameisJamesBond007"));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "https://www.yogihosting.com",
                audience: "dotnetclient",
                expires: DateTime.Now.AddHours(3),
                signingCredentials: credentials,
                claims: claims
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }*/

        public List<Users> CreateDummyUsers()
        {
            List<Users> userList = new List<Users> {
                new Users { Username = "jack", Password = "jack", Role = "Admin" },
                new Users { Username = "donald", Password = "donald", Role = "Manager" },
                new Users { Username = "thomas", Password = "thomas", Role = "Developer" }
            };
            return userList;
        }

        private void SetJWTCookie(string token)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddHours(3),
            };
            Response.Cookies.Append("jwtCookie", token, cookieOptions);
        }

        public async Task<IActionResult> FlightReservation()
        {
            var jwt = Request.Cookies["jwtCookie"];

            List<Reservation> reservationList = new List<Reservation>();

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
                using (var response = await httpClient.GetAsync("https://localhost:7154/Reservation")) // change API URL to yours 
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        reservationList = JsonConvert.DeserializeObject<List<Reservation>>(apiResponse);
                    }

                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        return RedirectToAction("Index", new { message = "Please Login again" });
                    }
                }
            }

            return View(reservationList);
        }
    }
}
