# Copilot Instructions

## Project Guidelines
- The user's JWT token generation has a typo in JwtService.cs: 'JwtSetting:Audience' should be 'JwtSettings:Audience' (missing 's'). This causes null audience in tokens, breaking validation.