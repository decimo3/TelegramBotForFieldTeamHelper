using System.Net.Http.Json;
using telbot.models;

namespace telbot;
public static class Updater
{
  public static async Task CheckUpdates()
  {
    var github = "https://api.github.com/repos/decimo3/TelegramBotForFieldTeamHelper/tags";
    using var request = new System.Net.Http.HttpClient();
    var response = await request.GetAsync(github);
    if (response.IsSuccessStatusCode)
    {
      var result = await response.Content.ReadFromJsonAsync<List<UpdaterModel>>();
      Console.WriteLine("Response from the API:");
      Console.WriteLine(result);
    }
    else
    {
      Console.WriteLine("Request failed with status code: " + response.StatusCode);
    }
  }
  public static void GetUpdates(string updateString) {}
  public static void ApplyUpdate(bool hasUpdate) {}
}