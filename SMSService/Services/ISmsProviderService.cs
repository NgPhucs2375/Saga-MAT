namespace SmsService.Services
{
    public interface ISmsProviderService
    {
        /// <summary>
        /// Hàm gửi SMS thực tế sang bên thứ 3 (Twilio, SpeedSMS, eSMS...)
        /// </summary>
        /// <param name="phoneNumber">Số điện thoại nhận</param>
        /// <param name="content">Nội dung tin nhắn</param>
        /// <returns>True nếu gửi thành công, False nếu thất bại</returns>
        Task<bool> SendAsync(string phoneNumber, string content);
    }
}