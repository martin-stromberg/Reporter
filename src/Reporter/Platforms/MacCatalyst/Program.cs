using ObjCRuntime;
using UIKit;

namespace Reporter;

/// <summary>
/// The Mac Catalyst entry point for the .NET MAUI application.
/// </summary>
public class Program
{
    // This is the main entry point of the application.
    static void Main(string[] args)
    {
        // if you want to use a different Application Delegate class from "AppDelegate"
        // you can specify it here.
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}
