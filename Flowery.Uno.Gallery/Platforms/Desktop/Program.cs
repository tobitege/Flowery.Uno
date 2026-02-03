using System;
using Uno.UI.Hosting;

namespace Flowery.Uno.Gallery.Desktop;

internal class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        Flowery.Uno.Gallery.App.RuntimeArguments = args;
        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseX11()
            .UseLinuxFrameBuffer()
            .UseMacOS()
            .UseWin32()
            .Build();

        host.Run();
    }
}
