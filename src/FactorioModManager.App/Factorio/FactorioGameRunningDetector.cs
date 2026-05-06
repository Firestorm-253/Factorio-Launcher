using System.Diagnostics;

namespace FactorioModManager.App.Factorio;

public sealed class FactorioGameRunningDetector : IFactorioGameRunningDetector
{
    public bool IsRunning()
    {
        Process[] processes;
        try
        {
            processes = Process.GetProcessesByName("factorio");
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return false;
        }

        try
        {
            foreach (var process in processes)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        return true;
                    }
                }
                catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
                {
                    // Ignore processes that exit or become inaccessible while being inspected.
                }
            }

            return false;
        }
        finally
        {
            foreach (var process in processes)
            {
                process.Dispose();
            }
        }
    }
}
