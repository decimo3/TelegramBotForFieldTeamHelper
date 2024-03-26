namespace telbot;
using telbot.models;
public static class Authorization
{
  private static string secret = "XDWUUQ5P9ZNZ97XI9VRIBNIHMPORBHRW";
  public static TokenModel? RecoveryToken (string token)
  {
    var DecodedToken = new JWT.Builder.JwtBuilder()
      .WithAlgorithm(new JWT.Algorithms.HMACSHA256Algorithm())
      .WithSecret(System.Text.Encoding.UTF8.GetBytes(secret))
      .MustVerifySignature()
      .Decode(token);
    return System.Text.Json.JsonSerializer.Deserialize<TokenModel>(DecodedToken);
  }
  public static String? GenerateToken (TokenModel token)
  {
    var DecodedToken = new JWT.Builder.JwtBuilder()
      .AddClaim("adm_id_bot", token.adm_id_bot)
      .AddClaim("allowed_pc", token.allowed_pc)
      .WithAlgorithm(new JWT.Algorithms.HMACSHA256Algorithm())
      .WithSecret(System.Text.Encoding.UTF8.GetBytes(secret))
      .MustVerifySignature()
      .Encode();
    return DecodedToken;
  }
}