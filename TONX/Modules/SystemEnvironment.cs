using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TONX.Modules;

public static class SystemEnvironment
{
    public static async Task SetEnvironmentVariablesAsync()
    {
        // 将最近打开的 TONX 应用程序文件夹的路径设置为用户环境变量
        await Task.Run(() => Environment.SetEnvironmentVariable("TOWN_OF_NEXT_DIR_ROOT", Environment.CurrentDirectory, EnvironmentVariableTarget.User));
        // 将日志文件夹的路径设置为用户环境变量
        var logFolderPath = await Task.Run(() => Utils.GetLogFolder().FullName);
        await Task.Run(() => Environment.SetEnvironmentVariable("TOWN_OF_NEXT_DIR_LOGS", logFolderPath, EnvironmentVariableTarget.User));
    }
}
