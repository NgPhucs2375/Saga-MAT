using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Onion.CleanArchitecture.Application.Exceptions;
using Onion.CleanArchitecture.Domain.Entities;
using Onion.CleanArchitecture.Domain.Events;
using SMSService.Context;
using SmsService.Services;

namespace SmsService.Consumer
{
    public class SmsConsumer : IConsumer<SendSmsCommand>
    {
        private readonly ILogger<SmsConsumer> _logger;
        private readonly ISmsProviderService _smsService;
        private readonly SmsDbContext _dbContext;

        public SmsConsumer(
            ISmsProviderService smsService,
            ILogger<SmsConsumer> logger,
            SmsDbContext dbContext)
        {
            _smsService = smsService;
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task Consume(ConsumeContext<SendSmsCommand> context)
        {
            var message = context.Message;
            _logger.LogInformation("Đang xử lý gửi SMS tới SDT: {PhoneNumber} với nội dung: {Content}", message.PhoneNumber, message.Content);

            var smsLog = new SmsLog
            {
                Id = Guid.NewGuid(),
                PhoneNumber = message.PhoneNumber,
                Content = message.Content,
                Status = "Pending",
                RetryCount = 0
            };

            await _dbContext.SmsLogs.AddAsync(smsLog);
            await _dbContext.SaveChangesAsync();

            try
            {
                bool result = await _smsService.SendAsync(message.PhoneNumber, message.Content);

                smsLog.Status = result ? "Sent" : "Failed";
                smsLog.SentAt = DateTime.UtcNow;

                if (!result)
                {
                    throw new ApiException($"Gửi SMS thất bại tới SDT: {message.PhoneNumber} với nội dung: {message.Content}");
                }

                _logger.LogInformation("Gửi SMS thành công tới SDT: {PhoneNumber} với nội dung: {Content}", message.PhoneNumber, message.Content);
            }
            catch (Exception ex)
            {
                smsLog.Status = "Failed";
                smsLog.ProviderResponse = ex.Message;
                _logger.LogError(ex, "Lỗi gửi SMS tới {PhoneNumber}", message.PhoneNumber);
                throw;
            }
            finally
            {
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}