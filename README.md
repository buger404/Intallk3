# Intallk 3

本项目基于**OneBot - Command Route Project**。

**Intallk.Resources**中的文件应放置在 **%AppData%\Intallk\Resources** 中。

# 功能管理

### 继承SimpleOneBotController并覆写Initialize()：

```c#
    public override ModuleInformation Initialize() =>
        new ModuleInformation 
        { 
            ModuleName = "功能名称", ModuleUsage = "功能详细解释",
            HelpCmd = "帮助指令关键词", RootPermission = "根权限名"
        };
```

### 继承ArchiveOneBotController\<T\>并覆写Initialize()：

机器人将自动以json的形式储存'T'所指的数据类型，并存放在Data成员中。

```c#
    public override ModuleInformation Initialize() =>
        new ModuleInformation 
        { 
            DataFile = "储存的json文件名", ModuleName = "功能名称", ModuleUsage = "功能详细解释",
            HelpCmd = "帮助指令关键词", RootPermission = "根权限名"
        };
```

覆写OnDataNull()处理空存档情况。

# 权限服务

**ANYTHING**权限可等效于任何权限。

一个权限名由两部分组成：**根权限名_子权限名**，根权限名以功能为单位划分，持有**根权限名_GRANT**的用户可对其他用户授予该根权限下的其他权限。

持有**跟权限名_ANYTHING**的用户可授予其他用户**根权限名_GRANT**权限。

权限分为群权限和用户权限。

权限的判定可使用'PermissionService.Judge()'或'PermissionService.JudgeGroup()'

例如：

```C#
    if (!PermissionService.JudgeGroup(e, Info, "RECORD", PermissionPolicy.RequireAccepted))
        return;
```

```C#
    if (!PermissionService.Judge(e, Info, "EXTRACT", PermissionPolicy.AcceptedIfGroupAccepted))
        return;
```

* 注：对用户权限的判定调用，若用户未持有权限，机器人将自动发送提示消息。

权限的优先级：ANYTHING > Denied权限 > Accepted权限

权限需求策略(PermissionPolicy)：

* RequireAccepted：需要Accepted权限才可使用

* AcceptedAsDefault：默认允许使用，但可以被Denied权限拒绝

* AcceptedIfGroupAccepted：需要Accepted权限才可使用。例外地，如果用户所在群持有同名权限，则也视为权限接受

* AcceptedAdminAsDefault：需要Accepted权限才可使用。例外地，群主和管理员默认允许使用

# 帮助指南集成

机器人将通过反射生成指南文本，无需另外制作使用说明。

CmdHelp Attribute：

```c#
    [Command("permission group deny <group> <permission>")]
    [CmdHelp("群号 权限名", "授予某群指定的Denied权限")]
    public void PermissionGroupDeny(GroupMessageEventArgs e, long group, string permission)
```

带有CmdHelp标签的方法，也应该带有Command标签，否则没有意义。

ArgDescription 中的内容使用空格分隔，将根据个数从左向右填充Command标签中的Pattern作为参数填写说明；未被解释说明的将在说明中被丢弃。

UsageDescription 解释该指令的作用。

参照**功能管理**，**help 帮助指令关键词**将作为查看此功能指南的指令，其中'功能详细解释'和'功能名称'将展示给用户。
