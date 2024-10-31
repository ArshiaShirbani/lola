from selenium import webdriver
from selenium_stealth import stealth
from tempfile import mkdtemp
from selenium.webdriver.common.by import By
from selenium.webdriver.support import expected_conditions as EC
from selenium.webdriver.support.ui import WebDriverWait
from selenium.common.exceptions import TimeoutException
from selenium.common.exceptions import NoSuchElementException
from selenium.webdriver.common.action_chains import ActionChains
import time
import scipy.stats as stats



def wilson_lower_bound(winrate, games, confidence_level=0.95):
    """
    Calculate the lower bound of the Wilson confidence interval with continuity correction.
    
    :param winrate: Observed winrate as a proportion (e.g., 0.5 for 50%)
    :param games: The number of games played
    :param confidence_level: The desired confidence level (default is 99%)
    :return: The lower bound of the Wilson score interval
    """
    # Z-score for (1 - alpha/2)
    z = stats.norm.ppf(1 - (1 - confidence_level) / 2)
    # Wilson score interval lower bound calculation
    denominator = 2 * (games + z**2)
    term1 = 2 * games * winrate + z**2
    term2 = z * (z**2 -  (1/games) + 4 * games * winrate * (1 - winrate) + (4 * winrate - 2))**0.5 + 1
    lower_bound = max(0, (term1 - term2) / denominator)
    return lower_bound

def scrape(num_items, chrome):
    chrome.find_element(By.XPATH, f"//div[@data-type='c_{num_items}']").click()
    time.sleep(3)


    builds_set = set()
    
    element = chrome.find_element(By.XPATH, "(//div[@class='cursor-grab overflow-x-scroll'])[2]")
    time.sleep(1)

    ActionChains(chrome)\
        .scroll_to_element(element)\
        .scroll_by_amount(0, 100)\
        .perform()
    
    size = chrome.find_element(By.XPATH, "(//div[@class='cursor-grab overflow-x-scroll'])[2]").size
    time.sleep(0.5)
    #width and height of element

    width = size['width']
    height = size['height']

    flag = False

    image_element = [None] * num_items
    while True:
        x = 0
        for x in range(1,33):
            try:
                xpath_games = f"/html/body/main/div[6]/div[1]/div[14]/div[2]/div/div[{x}]/div[3]"
                games_element = chrome.find_element(By.XPATH, xpath_games)
                if int(games_element.text.replace(',', '')) < 200:
                    flag = True
                    break
                else:
                    #print(f'Games:{games_element.text}')
                    xpath_wr = f"/html/body/main/div[6]/div[1]/div[14]/div[2]/div[1]/div[{x}]/div[1]/span[1]"
                    wr_element = chrome.find_element(By.XPATH, xpath_wr)
                    #print(f'Winrate:{wr_element.text}\n')
                    for k in range(num_items):
                        xpath_images = f"/html/body/main/div[6]/div[1]/div[14]/div[2]/div/div[{x}]/span[{k+1}]/img"
                        image_element[k] = chrome.find_element(By.XPATH, xpath_images).get_attribute('src')

                    adjusted_winrate = round(100 * wilson_lower_bound(float(wr_element.text.replace('%', ''))/100, int(games_element.text.replace(',', ''))), 2)
                    if adjusted_winrate >= 45:
                        if num_items == 3:
                            builds_set.add((adjusted_winrate, games_element.text, image_element[0], image_element[1], image_element[2]))
                        elif num_items == 4:
                            builds_set.add((adjusted_winrate, games_element.text, image_element[0], image_element[1], image_element[2], image_element[3]))



            except NoSuchElementException:
                # No more elements found, break the loop
                break

        print(f'x:{x}')
        if flag:
            break


        # Scroll the table a few times
        actions = ActionChains(chrome)
        element = chrome.find_element(By.XPATH, "(//div[@class='cursor-grab overflow-x-scroll'])[2]")
        for _ in range(10):  # Adjust this number as needed
            actions.move_to_element_with_offset(element, (width/2)-5, (height/2)-10).click().perform()

        time.sleep(0.2)

    #/html/body/main/div[6]/div[1]/div[14]/div[2]/div/div[1]/span[1]/img
    #/html/body/main/div[6]/div[1]/div[14]/div[2]/div/div[1]/span[2]/img
    #/html/body/main/div[6]/div[1]/div[14]/div[2]/div/div[1]/span[3]/img



    #print(winrate_games_set)
    builds_list = list(builds_set)

    #builds_list.sort(key=lambda x: int(x[1].replace(',', '')), reverse=True) #sort by games
    builds_list.sort(key=lambda x: x[0], reverse=True) #sort by winrate

    if num_items == 3:
        chrome.refresh()

    return builds_list


def main(url):
    options = webdriver.ChromeOptions()
    chromedriver_path = r'C:\Users\arshi\Desktop\chromedriver-win64\chromedriver.exe'  # Adjust this path as necessary
    service = webdriver.ChromeService(chromedriver_path)



    options.add_argument("--headless=new")
    options.add_argument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3")
    options.add_argument("--start-maximized")
    options.add_argument("--disable-blink-features=AutomationControlled")
    options.add_argument("--disable-extensions")
    options.add_argument("--incognito")
    options.add_argument("--disable-popup-blocking")
    options.add_argument("--disable-application-cache")
    options.add_argument('--no-sandbox')
    #options.add_argument("--disable-gpu")
    #options.add_argument("--window-size=1280x1696")
    options.add_argument("--disable-dev-shm-usage")
    options.add_argument("--disable-dev-tools")
    options.add_argument("--no-zygote")
    options.add_argument(f"--user-data-dir={mkdtemp()}")
    options.add_argument(f"--data-path={mkdtemp()}")
    options.add_argument(f"--disk-cache-dir={mkdtemp()}")
    options.add_argument("--remote-debugging-port=9222")
    options.add_argument("--log-level=3")
    options.add_argument("--disable-webgl") #remove if website requires webgl

    chrome = webdriver.Chrome(options=options, service=service)
    stealth(chrome,
            languages=["en-US", "en"],
            vendor="Google Inc.",
            platform="Win32",
            webgl_vendor="Intel Inc.",
            renderer="Intel Iris OpenGL Engine",
            fix_hairline=True,
            )
    


    chrome.get(url)
    
    # WebDriverWait(chrome, 10).until(
    #     EC.presence_of_element_located((By.XPATH, "//div[@data-type='c_4']"))
    # )

    # WebDriverWait(chrome, 10).until(
    #     EC.presence_of_element_located((By.XPATH, "(//div[@class='cursor-grab overflow-x-scroll'])[2]"))
    # )

    builds_list3 = scrape(3, chrome)
    builds_list4 = scrape(4, chrome)

    chrome.quit()

    return builds_list3, builds_list4


if __name__ == '__main__':
    main()