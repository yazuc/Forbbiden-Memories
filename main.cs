namespace fm
{
    public class Program
    {
        public static async Task Main()
        {            
             string result = await fm.Function.Fusion("177,296,211");
             Console.WriteLine(result); 
        }
    }
}