using System.IO;

namespace GameKeeper.Services;

public class GameKeeperService
{
    private readonly string _coreDllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GameKeeperCore.dll");

    public bool Attach(int processId)
    {
        if (DllLoaderApi.GK_InjectCoreDll(processId, _coreDllPath))
        {
            return DllLoaderApi.GK_TryAttach(processId, _coreDllPath);
        }

        return false;
    }

    public bool Detach(int processId)
    {
        return DllLoaderApi.GK_TryDetach(processId, _coreDllPath);
    }
}