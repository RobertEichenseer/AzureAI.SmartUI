import { Component, ViewChild, ElementRef, NgZone } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HttpClient, HttpEventType } from '@angular/common/http';
import { Observable } from 'rxjs';
import { FormsModule } from '@angular/forms';
import Chart from 'chart.js/auto';
import ChartDataLabels from 'chartjs-plugin-datalabels';
import { ChartData, Data, Dataset } from './chart-data.model';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, FormsModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})

export class AppComponent {
  @ViewChild('responseTextArea') responseTextArea!: ElementRef;
  @ViewChild('imageChart') imageChart!: ElementRef;
  @ViewChild('imageXls') imageXls!: ElementRef;

  title = 'Smart UI';
  serverResponse: any = "";
  userPrompt: any = "Hover over above buttons to see sample prompts"; 
  activityDetailText = "";
  
  chart: any = []
  chartData: any = {}
  private imageDataURL: string | null = null;
  private extractedChartData: any = null;
  private activityDetail: any[] = [
    { text: 'Performs a GET to the server to retrieve chart data and creates a chart' },
    { text: 'Takes a screenshot from the created chart and uploads the image to the server' },
    { text: 'Extracts data from the chart and returns a JSON string. This JSON string is used to answer questions.', icon: 'extract' },
  ]; 
  private queryDetail: any[] = [
    { text: 'Explain the chart in one sentence.' },
    { text: 'Which production line has downtime on Saturday and on Monday?' },
    { text: 'Create an Excel file with the chart data!' },
  ];
  

  constructor(private http: HttpClient, private ngZone: NgZone) {}

  showActivityDetail(text: string) {
    switch (text) {
      case 'Create':
        this.activityDetailText = this.activityDetail[0].text;
        break;
      case 'Screenshot':
        this.activityDetailText = this.activityDetail[1].text;;
        break;
      case 'Extract':
        this.activityDetailText = this.activityDetail[2].text;;
        break;
    }
  }

  clearActivityDetail() {
    this.activityDetailText = '';
  }

  showUserPrompt(text: string) {
    switch (text) {
      case 'Explain':
        this.userPrompt = this.queryDetail[0].text;
        break;
      case 'Downtime':
        this.userPrompt = this.queryDetail[1].text;
        break;
      case 'XLS':
        this.userPrompt = this.queryDetail[2].text;
        break;
    }
  }

  createChart(chartData: ChartData) {
    Chart.register(ChartDataLabels);

    this.chart = new Chart('canvas', {
      type: 'bar',
      data: {
        labels: chartData.data.labels,
        datasets: chartData.data.datasets.map(dataset => ({
          label: dataset.label,
          data: dataset.data,
          borderWidth: 1,
        }))
      },
      options: {
        plugins: {
          title: {
            display: true,
            text: chartData.label
          }
        },
        scales: {
          y: {
            beginAtZero: true,
          },
        },
      },
    });
  }
  
  onGetData(){
    this.serverResponse = '... getting chart data\n';
    const observer = {
      next: (response: any) => {
        this.createChart(response);
        this.serverResponse = `${this.serverResponse}\n${JSON.stringify(response)}\n`;
      },
      error: (error: any) => {
        this.serverResponse = JSON.stringify(error);
      },
      complete: () => {
      }
    };

    const observable: Observable<any> = this.http.get('http://localhost:5225/getdata');
    observable.subscribe(observer);
  }

  onTakeScreenshot(): void {
    this.serverResponse = '... taking & uploading screenshot\n';
    const canvas = document.getElementById('canvas') as HTMLCanvasElement;
    if (canvas) {
      this.imageDataURL = canvas.toDataURL('image/png');
      this.uploadScreenshot(this.imageDataURL);
    }
    this.serverResponse = `${this.serverResponse}\n...Screenshot taken\n${this.imageDataURL?.substring(0,50)}\n`;
    this.imageChart.nativeElement.style.visibility = 'visible';
  }

  uploadScreenshot(imageDataURL: string): void {
    const blob = this.dataURLToBlob(imageDataURL);
    const formData = new FormData();
    formData.append('chartScreenShot', blob, 'ChartScreenShot.png');

    const observer = {
      next: (response: any) => {
        this.serverResponse = `${this.serverResponse}\n...Screenshot uploaded successfully\n`;
      },
      error: (error: any) => {
        this.serverResponse = `${this.serverResponse}\n...Screenshot upload failed\n${JSON.stringify(error)}\n`;
      }
    };

    this.http.post('http://localhost:5225/uploadchart', formData).subscribe(observer);
  }

  dataURLToBlob(dataURL: string): Blob {
    const byteString = atob(dataURL.split(',')[1]);
    const mimeString = dataURL.split(',')[0].split(':')[1].split(';')[0];
    const ab = new ArrayBuffer(byteString.length);
    const ia = new Uint8Array(ab);
    for (let i = 0; i < byteString.length; i++) {
      ia[i] = byteString.charCodeAt(i);
    }
    return new Blob([ab], { type: mimeString });
  }

  onExtractScreenshotData(){
    this.serverResponse = '... waiting for extracted data\n';
    
    const observer = {
      next: (event: any) => {
        HttpEventType.DownloadProgress
        if (event.type === HttpEventType.DownloadProgress) {
          this.ngZone.run(() => {
            this.serverResponse = '... waiting for extracted data\n\n' + event.partialText
              .replace(/\n/g, ' ')
              .replace(/\r/g, '')
              .replace(/\s+/g, ' ') + '\n';
            this.scrollToBottom(); 
          })
        }
        if (event.type === HttpEventType.Response) {
          this.extractedChartData = JSON.stringify(event.body);
        }
      },
      error: (error: any) => {
        this.serverResponse = JSON.stringify(error);
        console.error('Error from backend:', error);
      },
      complete: () => {
        this.serverResponse = `${this.serverResponse}'\n... extracted data received`;
      }
    };

    const requestData = {
      userMessage: this.userPrompt,
      chartData: this.extractedChartData
    };

    const observable: Observable<any> = this.http.get('http://localhost:5225/extractdata', {
      responseType: 'text',
      reportProgress: true,
      observe: 'events'
    });
    observable.subscribe(observer);
  }
  
  onGetResponseStream() {
    this.serverResponse = '... waiting for server response\n';
    
    const observer = {
      next: (event: any) => {
        HttpEventType.DownloadProgress
        if (event.type === HttpEventType.DownloadProgress) {
          this.ngZone.run(() => {
            this.serverResponse = '... waiting for server response\n\n' + (event.partialText
              .replace(/\n/g, ' ')
              .replace(/\r/g, '')
              .replace(/\s+/g, ' '));
            this.scrollToBottom(); 
          })

        }
      },
      error: (error: any) => {
        this.serverResponse = JSON.stringify(error);
        console.error('Error from backend:', error);
      },
      complete: () => {
        this.serverResponse = `${this.serverResponse}'\n\n... server response received'`;
      }
    };

    const requestData = {
      userMessage: this.userPrompt,
      chartData: this.extractedChartData
    };

    const observable: Observable<any> = this.http.get('http://localhost:5225/responsestream?requestData=' + JSON.stringify(requestData), {
      responseType: 'text',
      reportProgress: true,
      observe: 'events'
    });
    observable.subscribe(observer);
  }

  private scrollToBottom(): void {
    this.ngZone.runOutsideAngular(() => {
      setTimeout(() => {
        this.responseTextArea.nativeElement.scrollTop = this.responseTextArea.nativeElement.scrollHeight;
      }, 0);
    });
  }

}
