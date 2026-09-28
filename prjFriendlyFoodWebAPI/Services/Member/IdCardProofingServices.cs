using Google.Cloud.Vision.V1;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace prjFriendlyFoodWebAPI.Services.Member
{
    public class IdCardProofingServices
    {
        public class IdCardResult
        {
            public string? Name { get; set; }
            public string? IdNumber { get; set; }
        }
        private readonly IConfiguration _config;
        private readonly HttpClient _http;
        public IdCardProofingServices(IConfiguration config,HttpClient http)
        {
            _config = config;
            _http = http;
        }
        public async Task<IdCardResult> ReadTextAsync(IFormFile file)
        {
            string? api = _config["Authentication:Google:CloudVision"];
            using var ms = new MemoryStream();

            await file.CopyToAsync(ms);
            string base64 =Convert.ToBase64String(ms.ToArray());
            var body = new
            {
                requests = new[]
                {
                new
                {
                    image = new
                    {
                        content = base64
                    },

                    features = new[]
                        {
                            new
                            {
                            type = "DOCUMENT_TEXT_DETECTION"
                            }
                        }
                    }
                }
            };
            string url =$"https://vision.googleapis.com/v1/images:annotate?key={api}";
            var response =
            await _http.PostAsJsonAsync(url, body);

            response.EnsureSuccessStatusCode();

            var result =
                await response.Content.ReadFromJsonAsync<JsonDocument>();
            string text =
                result?
                    .RootElement
                    .GetProperty("responses")[0]
                    .GetProperty("fullTextAnnotation")
                    .GetProperty("text")
                    .GetString()
                ?? string.Empty;

            
            return ParseIdCard(text);
        }
            public IdCardResult ParseIdCard(string text)
            {
                string? name = null;
                string? idNumber = null;

                // 姓名
                var nameMatch = Regex.Match(
                    text,
                    @"姓名\s*([^\r\n]+)"
                );

                if (nameMatch.Success)
                {
                    name = nameMatch.Groups[1].Value;

                    // OCR 可能得到 "陳 筱玲"
                    name = Regex.Replace(name, @"\s+", "");
                }

                // 身分證字號
                var idMatch = Regex.Match(
                    text.ToUpperInvariant(),
                    @"[A-Z][12]\d{8}"
                );

                if (idMatch.Success)
                {
                    idNumber = idMatch.Value;
                }

                return new IdCardResult
                {
                    Name = name,
                    IdNumber = idNumber
                };
             }
        }
}
