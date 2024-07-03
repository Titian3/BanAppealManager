# Ban Appeal Manager

Ban Appeal Processor is a web application designed to streamline the processing of ban appeals for the SS14 project. It provides an interface for viewing and managing outstanding ban appeals and discussions. The application can gather stats, provide an overview of outstanding ban appeals, and process them by collating required info for an admin vote and generating an AI summarized description.

## Features

- **Appeal Management**: View and process outstanding ban appeals.
- **Automated Scraping**: Uses Playwright to scrape data from the SS14 forums.
- **Dynamic UI**: Built with Blazor, providing a modern and interactive interface.
- **CLI Console**: Main project also includes a CLI console for various operations.

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Node.js (for Playwright installation)

### Installation

1. Clone the repository:

    ```bash
    git clone https://github.com/Titian3/BanAppealManager.git
    cd your-repository
    ```

2. Configure the environment:

    - Add login details to `.defaultenv` and rename it to `.env`.
    - You will need to enter 2FA in the console on the first run, even on browser, watch for it in the console when processing an appeal.

3. Install dependencies:

    ```bash
    dotnet restore
    ```

4. Run the application using the HTTP profile:

    ```json
    "profiles": {
      "http": {
        "commandName": "Project",
        "dotnetRunMessages": true,
        "launchBrowser": true,
        "applicationUrl": "http://localhost:5150",
        "environmentVariables": {
          "ASPNETCORE_ENVIRONMENT": "Development"
        }
      }
    }
    ```

    ```bash
    dotnet run --launch-profile http
    ```

## Usage

1. Navigate to the home page of the application.
2. View statistics and overview of ban appeals.
3. Click "Process" next to an appeal to view details and start processing.

## Project Structure

- **BanAppealManager.Main**: Core logic for scraping and processing appeals.
- **BanAppealManager.UI**: Blazor components for the front-end interface.
- **BanAppealManager.Main.Scrapers**: Classes for web scraping using Playwright.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for more details.
