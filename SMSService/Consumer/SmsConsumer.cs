using MassTransit;
using Microsoft.Extensions.Logging;
using Onion.CleanArchitecture.Application.Exceptions;
using Onion.CleanArchitecture.Domain.Events;
using SmsService.Services;

namespace SmsService.Consumer
{
    public class SmsConsumer : IConsumer<SendSmsCommand>
    {
        private readonly ILogger<SmsConsumer> _logger;
        private readonly ISmsProviderService _smsService;

        public SmsConsumer(
            ISmsProviderService smsService,
            ILogger<SmsConsumer> logger
        )
        {
            _smsService = smsService;
            _logger = logger;
        }
        
        public async Task Consume(ConsumeContext<SendSmsCommand> context)
        {
        var message = context.Message;
           _logger.LogInformation("Đang xử lý gửi SMS tới SDT: {PhoneNumber} với nội dung: {Content}", message.PhoneNumber, message.Content);
           // goi API ben thu 3
        bool result = await _smsService.SendAsync(message.PhoneNumber, message.Content);
            if (!result)
            {
                throw new ApiException($"Gửi SMS thất bại tới SDT: {message.PhoneNumber} với nội dung: {message.Content}");
            } 
        
        _logger.LogInformation("Gửi SMS thành công tới SDT: {PhoneNumber} với nội dung: {Content}", message.PhoneNumber, message.Content);
        }
    }
}