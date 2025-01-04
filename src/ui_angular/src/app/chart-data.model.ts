export interface ChartData {
    label: string;
    data: Data;
}

export interface Data {
    labels: string[];
    datasets: Dataset[];
}

export interface Dataset {
    label: string;
    data: number[];
}