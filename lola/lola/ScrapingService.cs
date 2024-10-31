using System;
using System.Net.NetworkInformation;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;


namespace lola;



public class ScrapeResult
{
    public double AdjustedWinrate { get; set; }
    public string Games { get; set; }
    public List<string> ImageElements { get; set; }

    public override bool Equals(object obj)
    {
        if (obj is ScrapeResult other)
        {
            return AdjustedWinrate == other.AdjustedWinrate
                && Games == other.Games
                && ImageElements.SequenceEqual(other.ImageElements);
        }
        return false;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + AdjustedWinrate.GetHashCode();
            hash = hash * 23 + (Games != null ? Games.GetHashCode() : 0);
            hash = hash * 23 + (ImageElements != null ? ImageElements.Aggregate(0, (h, i) => h * 23 + (i != null ? i.GetHashCode() : 0)) : 0);
            return hash;
        }
    }
}


public static class FirstScript
{
    public static double WilsonLowerBound(double winrate, int games, double confidenceLevel = 0.95)
    {
        // Z-score for (1 - alpha/2)
        double z = MathNet.Numerics.Distributions.Normal.InvCDF(0, 1, 1 - (1 - confidenceLevel) / 2);
        // Wilson score interval lower bound calculation
        double denominator = 2 * (games + z * z);
        double term1 = 2 * games * winrate + z * z;
        double term2 = z * Math.Sqrt(z * z - 1 / (double)games + 4 * games * winrate * (1 - winrate) + 4 * winrate - 2) + 1;
        double lowerBound = Math.Max(0, (term1 - term2) / denominator);
        return lowerBound;
    }
    public static List<ScrapeResult> Scrape(int numItems, IWebDriver driver)
    {
        var buildsSet = new HashSet<ScrapeResult>();

        Thread.Sleep(3000);

        driver.FindElement(By.XPath($"//div[@data-type='c_{numItems}']")).Click();



        var element = driver.FindElement(By.XPath("(//div[@class='cursor-grab overflow-x-scroll'])[2]"));



        new Actions(driver)
            .ScrollToElement(element)
            .ScrollByAmount(0, 400)
            .Perform();

        element = driver.FindElement(By.XPath("(//div[@class='cursor-grab overflow-x-scroll'])[2]"));

        var size = driver.FindElement(By.XPath("(//div[@class='cursor-grab overflow-x-scroll'])[2]")).Size;
        var width = size.Width;
        var height = size.Height;

        Console.WriteLine($"Width: {width}, Height: {height}"); 

        var flag = false;

        var imageElement = new string[numItems];



        while(true)
        {
            var x = 0;
            for (x = 1; x < 33; x++)
            {
                try
                {
                    var xpathGames = $"(//div[@class='cursor-grab overflow-x-scroll'])[2]/div/div[{x}]/div[3]";
                    var gamesElement = driver.FindElement(By.XPath(xpathGames));
                    Console.WriteLine($"Games: {gamesElement.Text}");
                    if (int.Parse(driver.FindElement(By.XPath(xpathGames)).Text.Replace(",", "")) < 200)
                    {
                        Console.WriteLine("Less than 200 games flag is now true");
                        flag = true;
                        break;
                    }
                    else
                    {
                        var xpathWr = $"(//div[@class='cursor-grab overflow-x-scroll'])[2]/div/div[{x}]/div[1]/span[1]";
                        var wrElement = driver.FindElement(By.XPath(xpathWr));

                        for (var k = 0; k < numItems; k++)
                        {
                            var xpathImages = $"(//div[@class='cursor-grab overflow-x-scroll'])[2]/div/div[{x}]/span[{k + 1}]/img";
                            imageElement[k] = driver.FindElement(By.XPath(xpathImages)).GetAttribute("src");
                        }

                        var adjustedWinrate = Math.Round(100 * WilsonLowerBound(float.Parse(wrElement.Text.Replace("%", "")) / 100, int.Parse(gamesElement.Text.Replace(",", ""))), 2);
                        if (adjustedWinrate >= 45)
                        {
                            if (numItems == 3)
                            {
                                buildsSet.Add(new ScrapeResult
                                {
                                    AdjustedWinrate = adjustedWinrate,
                                    Games = gamesElement.Text,
                                    ImageElements = new List<string> { imageElement[0], imageElement[1], imageElement[2] }
                                });
                            }
                            else if (numItems == 4)
                            {
                                buildsSet.Add(new ScrapeResult
                                {
                                    AdjustedWinrate = adjustedWinrate,
                                    Games = gamesElement.Text,
                                    ImageElements = new List<string> { imageElement[0], imageElement[1], imageElement[2], imageElement[3] }
                                });
                            }
                        }
                    }
                }
                catch (NoSuchElementException)
                {
                    break;
                }
            }
            Console.WriteLine($"x: {x}");

            if (flag)
            {
                break;
            }

        var actions = new Actions(driver);
        element = driver.FindElement(By.XPath("(//div[@class='cursor-grab overflow-x-scroll'])[2]"));

        actions.MoveToElement(element, (width / 2) - 5, (height / 2) - 10).ClickAndHold().Perform();

        if (numItems == 3)
        {
            Thread.Sleep(2500); // adjust the delay as needed
        }
        else if (numItems == 4)
        {
            Thread.Sleep(3300); // adjust the delay as needed
        }
            actions.Release().Perform();
        Thread.Sleep(200);
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10)); // adjust the timeout as needed
        wait.Until(driver => driver.FindElement(By.XPath("(//div[@class='cursor-grab overflow-x-scroll'])[2]/div/div[1]/div[3]")));

        }


        var buildsList = buildsSet.ToList();
        buildsList.Sort((x, y) => y.AdjustedWinrate.CompareTo(x.AdjustedWinrate)); //sort by winrate

        foreach (var result in buildsList)
        {
            Console.WriteLine($"Adjusted Winrate: {result.AdjustedWinrate}");
            Console.WriteLine($"Games: {result.Games}");
            foreach (var image in result.ImageElements)
            {
                Console.WriteLine($"Image: {image}");
            }
            Console.WriteLine();
        }

        return buildsList;


    // html / body / main / div[6] / div[1] / div[14] / div[2]
    // html / body / main / div[6] / div[1] / div[14] / div[2] / div / div[1] / div[1]

    }
    public static void Main()
    { 
        var chromeOptions = new ChromeOptions();

        var service = ChromeDriverService.CreateDefaultService();
        service.HideCommandPromptWindow = true;
        service.SuppressInitialDiagnosticInformation = true;

        //chromeOptions.AddArgument("--headless");
        chromeOptions.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3");
        chromeOptions.AddArgument("--start-maximized");
        chromeOptions.AddArgument("--disable-blink-features=AutomationControlled");
        chromeOptions.AddArgument("--disable-extensions");
        chromeOptions.AddArgument("--incognito");
        chromeOptions.AddArgument("--disable-popup-blocking");
        chromeOptions.AddArgument("--disable-dev-shm-usage");
        chromeOptions.AddArgument("--disable-dev-tools");
        chromeOptions.AddArgument("--no-zygote");
        chromeOptions.AddArgument("--mute-audio");
        chromeOptions.AddArguments("--disable-logging");
        chromeOptions.AddArguments("--silent");
        chromeOptions.AddArguments("--log-level=3");


        IWebDriver driver = new ChromeDriver(service, chromeOptions);
        driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(3);

        driver.Navigate().GoToUrl("https://lolalytics.com/lol/jhin/build/?patch=14.8");
        var title = driver.Title;

        Console.WriteLine(title);

        Scrape(4, driver);

        driver.Quit();
    }
}