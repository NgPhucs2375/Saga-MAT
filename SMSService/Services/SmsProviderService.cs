using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SmsService.Services
{
    public class SmsProviderService : ISmsProviderService
    {
        private readonly ILogger<SmsProviderService> _logger;
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public SmsProviderService(
            ILogger<SmsProviderService> logger,
            IConfiguration config,
            HttpClient httpClient
        )
        {
            _logger = logger;
            _config = config;
            _httpClient = httpClient;
        }

        public async Task<bool> SendAsync(string phoneNumber, string content)
        {
            try{
                var accessToken = _config["SPEEDSMS_ACCESS_TOKEN"];
                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogError("[SPEEDSMS] Chưa cấu hình SPEEDSMS_ACCESS_TOKEN trong tệp .env");
                    return false;
                }
                var url = $"https://api.speedsms.vn/index.php/sms/send?access_token={accessToken}";

                // Chuaan hoa ve SDT VIET NAM (vd : 0912345678 -> +84912345678)
                var formattedPhone = phoneNumber.StartsWith("0") ? "84" + phoneNumber.Substring(1) : phoneNumber;

                var payload = new
                {
                    to = new[] { formattedPhone },
                    content = content,
                    sms_type = 2 // 2: Tin nhawsn Cham soc khach hang/OTP
                };

                _logger.LogInformation("[SPEEDSMS] Đang gửi SMS tới SDT: {PhoneNumber} với nội dung: {Content}", formattedPhone, content);
                var response = await _httpClient.PostAsJsonAsync(url,payload);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("[SPEEDSMS] Gửi SMS thành công!");
                    return true;
                }
                
                _logger.LogError("[SPEEDSMS] Gửi thất bại. Status: {StatusCode}, Response: {Response}", response.StatusCode, await response.Content.ReadAsStringAsync());
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SPEEDSMS] Lỗi gửi SMS: {Message}", ex.Message);
                return false;
            }
        }
    }
}