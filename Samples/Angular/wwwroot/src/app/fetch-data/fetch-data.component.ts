import { Component } from '@angular/core';


interface Ipc {
  sendSync(channel : string, message : any) : any;
}

declare global {
  interface Window { ipc: Ipc; }
}

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
    this.forecasts = await window.ipc.sendSync('getWeatherForecasts', { City: 'New York', UseCelsius: true });
  }
}

interface WeatherForecast {
  dateFormatted: string;
  temperatureC: number;
  temperatureF: number;
  summary: string;
}
