using CineBook.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace CineBook.Infrastructure.Services
{
    public class SmsService : ISmsService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SmsService> _logger;

        public SmsService(
            IConfiguration config,
            ILogger<SmsService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<bool> SendSmsAsync(string phoneNumber, string message)
        {
            try
            {
                var accountSid = _config["Twilio:AccountSid"];
                var authToken = _config["Twilio:AuthToken"];
                var from = _config["Twilio:WhatsAppFrom"];

                TwilioClient.Init(accountSid, authToken);

                var result = await MessageResource.CreateAsync(
                    to: new Twilio.Types.PhoneNumber($"whatsapp:+91{phoneNumber}"),
                    from: new Twilio.Types.PhoneNumber(from),
                    body: $"Your CineBook OTP is *{message}*. Valid for 10 minutes. Do not share with anyone."
                );

                if (result.ErrorCode == null)
                {
                    _logger.LogInformation("WhatsApp OTP sent to {Phone}", phoneNumber);
                    return true;
                }

                _logger.LogWarning("WhatsApp failed for {Phone}: {Error}",
                    phoneNumber, result.ErrorMessage);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError("WhatsApp exception for {Phone}: {Error}",
                    phoneNumber, ex.Message);
                return false;
            }
        }
    }
}