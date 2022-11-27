using Intallk.Config;
using Intallk.Models;
using OneBot.CommandRoute.Services;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Intallk.Modules;

public class ArchiveOneBotController<T> : SimpleOneBotController
{
    public T? Data;
    public string DataPath {
        get {
            if (Info == null) 
                throw new NotImplementedException();
            return Path.Combine(IntallkConfig.DataPath, this.Info.DataFile + ".json");
        }
    }

    public void Dump(int failCount = 0)
    {
        if (Info == null)
            throw new NotImplementedException();
        try
        {
            JsonSerializer serializer = new();
            var sb = new StringBuilder();
            serializer.Serialize(new StringWriter(sb), Data);
            File.WriteAllText(DataPath, sb.ToString());
            Logger?.LogInformation("{module}文件已保存：{file}。", Info.ModuleName , DataPath);
        }
        catch (Exception err)
        {
            if (failCount > 10)
            {
                if (Logger != null)
                    Logger.LogCritical("无法储存{module}，重试次数已超过预定次数。\n诱因：{message}\n调用堆栈：\n{stacktrace}", Info.ModuleName, err.Message, err.StackTrace);
                return;
            }
            Dump(++failCount);
        }
    }

    public ArchiveOneBotController(ICommandService commandService, ILogger<ArchiveOneBotController<T>> logger) : base(commandService, logger)
    {
        if (Info == null) 
            throw new NotImplementedException();
        if (File.Exists(DataPath))
        {
            JsonSerializer serializer = new();
            Data = (T)serializer.Deserialize(new StringReader(File.ReadAllText(DataPath)), typeof(T))!;
            Logger?.LogInformation("{module}文件已读取：{file}。", Info.ModuleName, DataPath);
        }
        else
        {
            Logger?.LogWarning("未发现{module}文件：{file}。", Info.ModuleName, DataPath);
            OnDataNull();
        }
    }

    public virtual void OnDataNull()
    {

    }
}
