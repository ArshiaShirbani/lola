from flask import Flask, render_template, request
# Import the scraping function from the scraper module
from winratesort import main


app = Flask(__name__)


@app.route('/', methods=['GET'])
def home():
    return render_template('form.html')

@app.route('/scrape', methods=['GET','POST'])
def scrape_data():
    url = request.form.get('url')
    data3, data4 = main(url)

    return render_template('images.html', data3=data3, data4=data4)

if __name__ == '__main__':
    app.run(debug=True)