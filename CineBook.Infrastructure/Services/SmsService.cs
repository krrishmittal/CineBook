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

        public SmsService(IConfiguration config, ILogger<SmsService> logger)
        {
            _config = config;
            _logger = logger;
            _logger.LogInformation("📱 SmsService initialized");
        }

        // ── Send OTP via WhatsApp ─────────────────────────────
        public async Task<bool> SendSmsAsync(string phoneNumber, string message)
        {
            _logger.LogInformation("📤 Starting WhatsApp OTP send to phone: {Phone}", phoneNumber);

            try
            {
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

                var accountSid = _config["Twilio:AccountSid"];
                var authToken = _config["Twilio:AuthToken"];
                var from = _config["Twilio:WhatsAppFrom"];

                if (string.IsNullOrWhiteSpace(accountSid) || string.IsNullOrWhiteSpace(authToken) || string.IsNullOrWhiteSpace(from))
                {
                    _logger.LogError("❌ Twilio configuration missing");
                    return false;
                }

                TwilioClient.Init(accountSid, authToken);

                var whatsappNumber = $"whatsapp:+91{phoneNumber}";
                var messageBody = $"Your CineBook OTP is *{message}*. Valid for 10 minutes. Do not share with anyone.";

                var result = await MessageResource.CreateAsync(
                    to: new Twilio.Types.PhoneNumber(whatsappNumber),
                    from: new Twilio.Types.PhoneNumber(from),
                    body: messageBody
                );

                if (result.ErrorCode == null)
                {
                    _logger.LogInformation("✅ WhatsApp OTP sent to {Phone}. SID: {Sid}", phoneNumber, result.Sid);
                    return true;
                }

                _logger.LogWarning("⚠️ WhatsApp OTP failed for {Phone}. Error: {Error}", phoneNumber, result.ErrorMessage);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error sending WhatsApp OTP to {Phone}", phoneNumber);
                return false;
            }
        }

        // ── Send general WhatsApp message (text only) ─────────
        public async Task<bool> SendWhatsAppAsync(string phoneNumber, string message)
        {
            _logger.LogInformation("📤 Sending WhatsApp message to {Phone}", phoneNumber);

            try
            {
                if (string.IsNullOrWhiteSpace(phoneNumber)) return false;

                var accountSid = _config["Twilio:AccountSid"];
                var authToken = _config["Twilio:AuthToken"];
                var from = _config["Twilio:WhatsAppFrom"];

                if (string.IsNullOrWhiteSpace(accountSid) || string.IsNullOrWhiteSpace(authToken) || string.IsNullOrWhiteSpace(from))
                {
                    _logger.LogError("❌ Twilio not configured");
                    return false;
                }

                TwilioClient.Init(accountSid, authToken);

                var whatsappNumber = phoneNumber.StartsWith("+")
                    ? $"whatsapp:{phoneNumber}"
                    : $"whatsapp:+91{phoneNumber}";

                var result = await MessageResource.CreateAsync(
                    to: new Twilio.Types.PhoneNumber(whatsappNumber),
                    from: new Twilio.Types.PhoneNumber(from),
                    body: message
                );

                if (result.ErrorCode == null)
                {
                    _logger.LogInformation("✅ WhatsApp sent to {Phone}. SID: {Sid}", phoneNumber, result.Sid);
                    return true;
                }

                _logger.LogWarning("⚠️ WhatsApp failed for {Phone}. Error: {Error}", phoneNumber, result.ErrorMessage);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ WhatsApp send failed for {Phone}", phoneNumber);
                return false;
            }
        }

        // ── Send WhatsApp with media attachment (image/PDF) ───
        public async Task<bool> SendWhatsAppWithMediaAsync(
            string phoneNumber, string message, string mediaUrl)
        {
            _logger.LogInformation("📤 Sending WhatsApp with media to {Phone}", phoneNumber);

            try
            {
                if (string.IsNullOrWhiteSpace(phoneNumber)) return false;

                var accountSid = _config["Twilio:AccountSid"];
                var authToken = _config["Twilio:AuthToken"];
                var from = _config["Twilio:WhatsAppFrom"];

                if (string.IsNullOrWhiteSpace(accountSid) || string.IsNullOrWhiteSpace(authToken) || string.IsNullOrWhiteSpace(from))
                {
                    _logger.LogError("❌ Twilio not configured");
                    return false;
                }

                TwilioClient.Init(accountSid, authToken);

                var whatsappNumber = phoneNumber.StartsWith("+")
                    ? $"whatsapp:{phoneNumber}"
                    : $"whatsapp:+91{phoneNumber}";

                var result = await MessageResource.CreateAsync(
                    to: new Twilio.Types.PhoneNumber(whatsappNumber),
                    from: new Twilio.Types.PhoneNumber(from),
                    body: message,
                    // ✅ Send image as WhatsApp media attachment
                    mediaUrl: new List<Uri> { new Uri(mediaUrl) }
                );

                if (result.ErrorCode == null)
                {
                    _logger.LogInformation("✅ WhatsApp with media sent to {Phone}. SID: {Sid}",
                        phoneNumber, result.Sid);
                    return true;
                }

                _logger.LogWarning("⚠️ WhatsApp with media failed for {Phone}. Error: {Error}",
                    phoneNumber, result.ErrorMessage);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ WhatsApp with media failed for {Phone}", phoneNumber);
                return false;
            }
        }
    }
}