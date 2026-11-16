using System.Runtime.CompilerServices;

namespace Karajob.Tests;

internal static class ThreadPoolSetup
{
  // xUnit runs test classes in parallel and many bUnit tests block pool threads in synchronous WaitFor*/Click.
  // The pool then grows too slowly for bUnit's dispatcher to run wait checks within the timeout.
  [ModuleInitializer]
  internal static void RaiseMinThreads()
  {
    ThreadPool.GetMinThreads(out var workers, out var io);
    ThreadPool.SetMinThreads(Math.Max(workers, Environment.ProcessorCount * 4), io);
  }
}
