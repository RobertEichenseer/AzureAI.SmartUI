namespace AzureAI.SmartUI.Shared;
public class ChartData
{
    public string Label { get; set; } = "";
    public Data Data { get; set; } = new Data();

    public ChartData(bool defaultValues = false)
    {
        if (defaultValues){
            Label = "Production Down Time in Minutes";
            Data.Labels = new List<string> { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
            Data.Datasets = new List<Dataset> {
                new Dataset {
                    Label = "Line Q7V",
                    Data = new List<int> { 11, 19, 3, 5, 2, 0 }
                },
                new Dataset {
                    Label = "Line CX9",
                    Data = new List<int> { 9, 10, 15, 20, 25, 30 }
                }
            };
        }
    }
}

public class Data
{
    public List<string> Labels { get; set; } = new List<string>();
    public List<Dataset> Datasets { get; set; } = new List<Dataset>();
}

public class Dataset
{
    public string Label { get; set; } = "";
    public List<int> Data { get; set; } = new List<int>();
    
}
