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
