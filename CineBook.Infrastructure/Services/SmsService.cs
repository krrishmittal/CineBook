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
            _logger.LogInformation("📱 SmsService initialized");
        }

        public async Task<bool> SendSmsAsync(string phoneNumber, string message)
        {
            _logger.LogInformation("📤 Starting WhatsApp OTP send to phone: {Phone}", phoneNumber);

            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(phoneNumber))
                {
                    _logger.LogWarning("⚠️ Phone number is null or empty");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(message))
                {
                    _logger.LogWarning("⚠️ Message is null or empty");
                    return false;
                }

                _logger.LogDebug("🔐 Loading Twilio configuration...");
                var accountSid = _config["Twilio:AccountSid"];
                var authToken = _config["Twilio:AuthToken"];
                var from = _config["Twilio:WhatsAppFrom"];

                // Validate configuration
                if (string.IsNullOrWhiteSpace(accountSid))
                {
                    _logger.LogError("❌ Twilio:AccountSid is not configured");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(authToken))
                {
                    _logger.LogError("❌ Twilio:AuthToken is not configured");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(from))
                {
                    _logger.LogError("❌ Twilio:WhatsAppFrom is not configured");
                    return false;
                }

                _logger.LogDebug("✓ Twilio configuration loaded. From: {From}", from);

                _logger.LogDebug("🔑 Initializing Twilio client...");
                TwilioClient.Init(accountSid, authToken);
                _logger.LogDebug("✓ Twilio client initialized successfully");

                _logger.LogDebug("📝 Preparing WhatsApp message for {Phone}. OTP: {OTP}", phoneNumber, message);
                var whatsappNumber = $"whatsapp:+91{phoneNumber}";
                var messageBody = $"Your CineBook OTP is *{message}*. Valid for 10 minutes. Do not share with anyone.";

                _logger.LogDebug("📤 Sending message to {WhatsAppNumber} from {From}", whatsappNumber, from);
                var result = await MessageResource.CreateAsync(
                    to: new Twilio.Types.PhoneNumber(whatsappNumber),
                    from: new Twilio.Types.PhoneNumber(from),
                    body: messageBody
                );

                if (result.ErrorCode == null)
                {
                    _logger.LogInformation("✅ WhatsApp OTP sent successfully to {Phone}. SID: {MessageSid}",
                        phoneNumber, result.Sid);
                    _logger.LogDebug("📊 Message Status: {Status}, DateSent: {DateSent}",
                        result.Status, result.DateSent);
                    return true;
                }

                _logger.LogWarning("⚠️ WhatsApp OTP failed for {Phone}. ErrorCode: {ErrorCode}, ErrorMessage: {ErrorMessage}",
                    phoneNumber, result.ErrorCode, result.ErrorMessage);
                return false;
            }
            catch (ArgumentException argEx)
            {
                _logger.LogError(argEx, "❌ Invalid argument while sending WhatsApp to {Phone}. Details: {Error}",
                    phoneNumber, argEx.Message);
                return false;
            }
            catch (Twilio.Exceptions.TwilioException twilioEx)
            {
                _logger.LogError(twilioEx, "❌ Twilio service error for {Phone}. Message: {Message}",
                    phoneNumber, twilioEx.Message);
                return false;
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "❌ Network error while contacting Twilio for {Phone}",
                    phoneNumber);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Unexpected error sending WhatsApp to {Phone}. Exception: {Error}",
                    phoneNumber, ex.GetType().Name);
                return false;
            }
        }
    }
}