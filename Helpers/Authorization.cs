namespace telbot;
using telbot.models;
public static class Authorization
{
  private static string secret = "michael_jackson_da_silva";
  public static TokenModel? RecoveryToken (string token)
  {
    var DecodedToken = new JWT.Builder.JwtBuilder()
      .WithAlgorithm(new JWT.Algorithms.HMACSHA256Algorithm())
      .WithSecret(System.Text.Encoding.UTF8.GetBytes(secret))
      .MustVerifySignature()
      .Decode(token);
    var tokemObject = System.Text.Json.JsonSerializer.Deserialize<TokenModel>(DecodedToken);
    return tokemObject;
  }
}