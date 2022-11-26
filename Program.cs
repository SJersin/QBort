/*
    Q-Bort - A queue management bot designed for use in multiple guilds.

    By: Jersin - 12 DEC 2020

 */

global using Serilog;
using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Serilog.Events;

namespace QBort
{
    class Program
    {
        static void Main()
        {
            Log.Logger = new LoggerConfiguration()
               .WriteTo.Console()
               .WriteTo.File("logs/qbort_log.txt")
               .CreateLogger();

            try
            {
                Bot QBort = new();
                Console.WriteLine("Starting bot...");
                QBort.MainAsync().GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Log.Fatal($"Bot could not start:");
                Log.Error(Messages.FormatError(e));
            }
        }
    }
}
