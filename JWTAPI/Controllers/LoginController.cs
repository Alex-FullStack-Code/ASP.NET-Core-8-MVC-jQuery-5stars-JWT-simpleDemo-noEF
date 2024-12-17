using JWTAPI.Models;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;

namespace JWTAPI.Controllers
{
    public class LoginController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Index(string username, string password)
        {
            if ((username != "secret") || (password != "secret"))
                return View((object)"Login Failed");

            var accessToken = GenerateJSONWebToken();
            return RedirectToAction("FlightReservation", new { token = accessToken });
        }

        private string GenerateJSONWebToken()
        {
            string rt = CreateRT();
            SetRTCookie(rt);

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("MynameisJamesBond007_MynameisJamesBond007"));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "https://www.yogihosting.com",
                audience: "dotnetclient",
                expires: DateTime.Now.AddMinutes(1),
                signingCredentials: credentials
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string CreateRT()
        {
            var randomNumber = new byte[32];
            using (var generator = new RNGCryptoServiceProvider())
            {
                generator.GetBytes(randomNumber);
                string token = Convert.ToBase64String(randomNumber);
                return token;
            }
        }

        private void SetRTCookie(string refreshToken)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddMinutes(2), 
            };
            Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
        }

        public async Task<IActionResult> FlightReservation(string token)
        {
            List<Reservation> reservationList = new List<Reservation>();

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                using (var response = await httpClient.GetAsync("https://localhost:7154/Reservation")) // change API URL to yours 
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        reservationList = JsonConvert.DeserializeObject<List<Reservation>>(apiResponse);
                    }

                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        return RedirectToAction("RefreshToken");
                    }
                }
            }

            return View(reservationList);
        }

        public IActionResult RefreshToken()
        {
            string cookieValue = Request.Cookies["refreshToken"];

            // If cookie is expired then it will give null
            if (cookieValue == null)
                return RedirectToAction("Index");

            // If cookie value is not the same as stored in db it is Hacking Attempt
            if (!CheckCookieValue(cookieValue))
                return RedirectToAction("Index");

            // If cookie is revoked by admin
            if (!CheckCookieEnabled(cookieValue))
                return RedirectToAction("Index");

            var tokenString = GenerateJSONWebToken();
            return RedirectToAction("FlightReservation", new { token = tokenString });
        }

        private bool CheckCookieValue(string cookieValue)
        {
            // Check the cookie value with stored in the db. If No match then it is forged cookie so return false.
            return true;
        }

        private bool CheckCookieEnabled(string cookieValue)
        {
            // Check if the cookie is enabled in the database. If cookie is not enabled then probably the admin has revoked it so return false.
            return true;
        }
    }
}
