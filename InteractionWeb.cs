using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
namespace telbot;
public class InteractionWeb
{
  private Configuration cfg;
  public InteractionWeb(Configuration cfg)
  {
    this.cfg = cfg;
  }
  private string usuario = "2258038@light.com.br";
  private string palavra = "Mestre$4'";
  public void Foto(string nota)
  {

    var options = new ChromeOptions();
    options.BinaryLocation = @"C:\Users\ruan.camello\scoop\apps\googlechrome\current\chrome.exe";
    options.AddArgument($@"--user-data-dir={cfg.CURRENT_PATH}\ofs");
    options.AddArgument("--app=https://lightsa.etadirect.com/");
    using(var driver = new ChromeDriver(options: options))
    {
      driver.Manage().Window.Maximize();
      if(driver.FindElements(By.Id("SignOutStatusMessage")).Count > 0)
      {
        throw new WebDriverException("Sessão foi encerrada!");
      }
      if(driver.FindElements(By.Id("username")).Count > 0)
      {
        driver.FindElement(By.Id("username")).SendKeys(usuario);
        driver.FindElement(By.Id("password")).SendKeys(palavra);
        driver.FindElement(By.Id("sign-in")).Click();
        System.Threading.Thread.Sleep(1000);
        if(driver.FindElements(By.ClassName("tile-img")).Count > 0)
        {
          driver.FindElements(By.ClassName("tile-img"))[0].Click();
        }
        else
        {
          driver.FindElement(By.Name("loginfmt")).SendKeys(usuario);
          driver.FindElement(By.Id("idSIButton9")).Click();
        }
        System.Threading.Thread.Sleep(1000);
        driver.FindElement(By.Name("passwd")).SendKeys(palavra);
        driver.FindElement(By.Id("idSIButton9")).Click();
        System.Threading.Thread.Sleep(1000);
        driver.FindElement(By.Id("idSIButton9")).Click();
      }
      System.Threading.Thread.Sleep(5000);
      driver.FindElement(By.ClassName("search-bar-input")).Click();
      
      driver.FindElement(By.ClassName("search-bar-input")).SendKeys(nota);
      Console.ReadLine();
      driver.Quit();
    }
  }
}