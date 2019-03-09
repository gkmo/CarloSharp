# Carlo# #

Web rendering surface for .NET applications.

This is a port of the Google Carlo project (<https://github.com/GoogleChromeLabs/carlo)> to .NET

## Requirements ##

.NET Core SDK 2.1+ (<https://www.microsoft.com/net/learn/get-started/windows)>

For the Angular and React samples Node JS (<https://nodejs.org/en/)> is also required.

## Building and running the Angular sample ##

To run the Angular sample application you must first restore the Angular NPM dependencies.

Open a terminal at **Samples/Angular/wwwroot** and execute:
`npm install`

After this step you can build and run the project using:
`dotnet run`

![alt text](Samples/LinuxAngular01.png "Carlo# Running Angular sample on Linux")

```cs
using CarloSharp.Samples.Angular.Controllers;
using CarloSharp;
using System;
using System.Threading;

namespace CarloSharp.Samples.Angular
{
    public class Program
    {
        private static ManualResetEvent _exitEvent = new ManualResetEvent(false);

        public static void Main(string[] args)
        {
            var app = Carlo.LaunchAsync(new Options()
            {
                Title = "Carlo# - Angular",
                Width = 1024,
                Height = 600,
                Channel = new string[] { "stable" }
            }).Result;

            var controller = new WeatherForecastController(app.MainWindow);

            app.ServeFolder("./wwwroot/dist");

            app.LoadAsync("index.html").Wait();

            app.OnExit += OnAppExit;

            _exitEvent.WaitOne();
        }

        private static void OnAppExit(object sender, EventArgs args)
        {
            _exitEvent.Set();
        }
    }
}
```

```cs
using CarloSharp.Samples.Angular.Model;
using System;
using System.Linq;

namespace CarloSharp.Samples.Angular.Controllers
{
    public class WeatherForecastController
    {
        private static string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        public WeatherForecastController(Window window)
        {
            window.ExposeFunctionAsync("getWeatherForecasts", GetWeatherForecasts).Wait();
        }

        public WeatherForecast[] GetWeatherForecasts()
        {
            var random = new Random();

            return Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                {
                    DateFormatted = DateTime.Now.AddDays(index).ToString("d"),
                    TemperatureC = random.Next(-20, 55),
                    Summary = Summaries[random.Next(Summaries.Length)]
                }).ToArray();
        }
    }
}
```

```typescript
import { Component } from '@angular/core';

declare function getWeatherForecasts(): Promise<WeatherForecast[]>;

@Component({
  selector: 'app-fetch-data',
  templateUrl: './fetch-data.component.html'
})
export class FetchDataComponent {
  public forecasts: WeatherForecast[];

  constructor() {
    this.loadAsync();
  }

  async loadAsync() {
    this.forecasts = await getWeatherForecasts();
  }
}

interface WeatherForecast {
  dateFormatted: string;
  temperatureC: number;
  temperatureF: number;
  summary: string;
}
```