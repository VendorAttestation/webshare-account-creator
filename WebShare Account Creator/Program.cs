using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        Console.Write("Enter the number of threads to use: ");
        if (int.TryParse(Console.ReadLine(), out int threads) && threads > 0)
        {
            try
            {
                SemaphoreSlim semaphore = new SemaphoreSlim(threads); // Set the maximum number of allowed threads

                async Task ExecuteProcessAsync(int index)
                {
                    await semaphore.WaitAsync(); // Wait until a slot is available
                    try
                    {
                        string capKey = await MethodsExensions.SolveCaptcha();
                        string authToken = await MethodsExensions.Register(capKey);
                        await MethodsExensions.GetProxy(authToken);

                        Console.WriteLine($"Task {index} completed.");
                    }
                    finally
                    {
                        semaphore.Release(); // Release the slot after the task is done
                    }
                }

                Console.Write("Enter the number of accounts to make: ");
                if (int.TryParse(Console.ReadLine(), out int numberOfTimes) && numberOfTimes > 0)
                {
                    var tasks = new Task[numberOfTimes];
                    for (int i = 0; i < numberOfTimes; i++)
                    {
                        tasks[i] = ExecuteProcessAsync(i + 1);
                    }

                    await Task.WhenAll(tasks);

                    Console.WriteLine("All tasks completed.");
                }
                else
                {
                    Console.WriteLine("Invalid input. Please enter a valid number of accounts.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("Invalid input. Please enter a valid number of threads.");
        }
        Console.ReadLine();
    }
}

