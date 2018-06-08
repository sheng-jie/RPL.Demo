# Razor Page Library：开发独立通用RPL（内嵌wwwroot资源文件夹）

# 1. Introduction
Razor Page Library 是ASP.NET Core 2.1引入的新类库项目，属于新特性之一，用于创建通用页面公用类库。也就意味着可以将多个Web项目中通用的Web页面提取出来，封装成RPL，以进行代码重用。
官方文档[Create reusable UI using the Razor Class Library project in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/mvc/razor-pages/ui-class?view=aspnetcore-2.1&tabs=visual-studio)中，仅简单介绍了如何创建RPL，但要想开发出一个独立通用的RPL远远没有那么简单，容我娓娓道来。

# 2. Hello RPL
老规矩，从Hello World 开始，我们创建一个Demo项目。
记住开始之前请确认已安装[.NET Core 2.1 SDK](https://www.microsoft.com/net/download/all)！！！
我们这次使用命令行来创建项目：
```
>dotnet --version
2.1.300
>dotnet new razorclasslib --name RPL.CommonUI
已成功创建模板“Razor Class Library”。

正在处理创建后操作...
正在 RPL.CommonUI\RPL.CommonUI.csproj 上运行 "dotnet restore"...
  正在还原 F:\Coding\Demo\RPL.CommonUI\RPL.CommonUI.csproj 的包...
  正在生成 MSBuild 文件 F:\Coding\Demo\RPL.CommonUI\obj\RPL.CommonUI.csproj.nuge
t.g.props。
  正在生成 MSBuild 文件 F:\Coding\Demo\RPL.CommonUI\obj\RPL.CommonUI.csproj.nuge
t.g.targets。
  F:\Coding\Demo\RPL.CommonUI\RPL.CommonUI.csproj 的还原在 1.34 sec 内完成。

还原成功。
>dotnet new mvc --name RPL.Web
已成功创建模板“ASP.NET Core Web App (Model-View-Controller)”。
此模板包含非 Microsoft 的各方的技术，有关详细信息，请参阅 https://aka.ms/aspnetc
ore-template-3pn-210。

正在处理创建后操作...
正在 RPL.Web\RPL.Web.csproj 上运行 "dotnet restore"...
  正在还原 F:\Coding\Demo\RPL.Web\RPL.Web.csproj 的包...
  正在生成 MSBuild 文件 F:\Coding\Demo\RPL.Web\obj\RPL.Web.csproj.nuget.g.props
。
  正在生成 MSBuild 文件 F:\Coding\Demo\RPL.Web\obj\RPL.Web.csproj.nuget.g.target
s。
  F:\Coding\Demo\RPL.Web\RPL.Web.csproj 的还原在 2 sec 内完成。

还原成功。
>dotnet new sln --name RPL.Demo
已成功创建模板“Solution File”。
>dotnet sln RPL.Demo.sln add RPL.CommonUI/RPL.CommonUI.csproj
已将项目“RPL.CommonUI\RPL.CommonUI.csproj”添加到解决方案中。
>dotnet sln RPL.Demo.sln add RPL.Web/RPL.Web.csproj
已将项目“RPL.Web\RPL.Web.csproj”添加到解决方案中。
```
创建完毕后，双击RPL.Demo.sln打开解决方案，如下图：

![](https://upload-images.jianshu.io/upload_images/2799767-02da0b2ada20a853.png?imageMogr2/auto-orient/strip%7CimageView2/2/w/1240)
1. 修改Page1.cshtml，body内添加`<h1>This is from CommonUI.Page1</h1>`
2. RPL.Web添加引用项目【RPL.CommonUI】
3. 设置RPL为启动项目。
4. CTRL+F5运行。

我们观察到RPL.CommonUI中预置了一个Razor Page，因为Razor Page是基于文件系统路由，所以直接`https://localhost:<port>/myfeature/page1`即可访问。
![](https://upload-images.jianshu.io/upload_images/2799767-6113a2a732804c6b.png?imageMogr2/auto-orient/strip%7CimageView2/2/w/1240)

到这一步，我们就可以笃定RPL正确生效。

# 3. Keep Going
以上只是简单的HTML页面，如果要想加以润色，就需要写CSS来处理。
两种处理方式：
1. 使用内联样式
2. 引用外部样式文件

内联样式，很简单，就不加以赘述。
我们来定义样式文件来处理。仿照RPL.Web项目，创建一个wwwroot根目录，然后再添加一个css文件夹，再添加一个demo.css的样式文件。
```
h1 {
    color: red;
}
```
然后将demo.css引用添加到page1.cshtml中。
```
<head>
    <meta name="viewport" content="width=device-width" />
    <link rel="stylesheet" href="~/css/demo.css" />
    <title>Page1</title>
</head>
```

CTRL+F5重新运行，运行结果如下图：
![](https://upload-images.jianshu.io/upload_images/2799767-083bc10977a81bd1.png?imageMogr2/auto-orient/strip%7CimageView2/2/w/1240)

可以清晰的看到，定义的样式并未生效。从浏览器F12 Developer Tool中可以清晰的看到，无法请求demo.css样式文件。
到这里，也就抛出了本文所要解决的问题：如何开发独立通用的RPL?
如果RPL中无法引用项目中定义一些静态资源文件（CSS、JS、Image等），那RPL将无法有效的组织View。

# 4. Analyze
要想访问RPL中的静态资源文件，首先我们要弄明白.NET Core Web项目中wwwroot文件夹的资源是如何访问的。
这一切得从应用程序启动说起，为了方便查阅，使用Code Map将相关代码显示如下：
![Program.cs](https://upload-images.jianshu.io/upload_images/2799767-84680bfcc09dd964.png?imageMogr2/auto-orient/strip%7CimageView2/2/w/1240)

从中可以看出在构建WebHost的业务逻辑中会去初始化`IHostingEnvironment`对象。该对象主要用来描述应用程序运行的web宿主环境的相关信息，主要包含以下几个属性：
```
string EnvironmentName { get; set; }
string ApplicationName { get; set; }
string WebRootPath { get; set; }
IFileProvider WebRootFileProvider { get; set; }
string ContentRootPath { get; set; }
IFileProvider ContentRootFileProvider { get; set; }
```
从上图的注释代码中可以看到，其初始化逻辑正是去指定`WebRootPath`和`WebRootFileProvider`。
如果我们在应用程序未手动通过`webHostBuilder.UseWebRoot("your web root path");`指定自定义的Web Root路径，那么将会默认指定为`wwwroot`文件夹。
同时注意下面这段代码：
```
hostingEnvironment.WebRootFileProvider = new
PhysicalFileProvider(hostingEnvironment.WebRootPath);
```
其指定的`IFileProvider`的类型为`PhysicalFileProvider`。
到这里，是不是就豁然开朗了，Web 应用启动时，指定的`WebRootFileProvider`仅仅映射了Web应用的wwwroot目录，自然是访问不了我们RPL项目指定的wwwroot目录啊。

到这里，其实我们离问题就很近了。但是只要指定了`WebRootFileProvider`就可以访问WebRoot目录的资源了吗？并不是。

我们知道，ASP.NET Core是通过由一系列中间件组装而成的请求管道来处理请求的。不管是View视图也好，还是静态资源文件也好，都是通过Http Request来请求的。HTTP Request流入请求管道后，根据请求类型，不同的中间件负责处理不同的请求。那对于静态资源文件，ASP.NET Core中是借助`StaticFileMiddleware`中间件来处理的。这也就是为什么在启动类`Startup`的`Configure`方法中需要指定`app.UseStaticFiles();`来启用`StaticFileMiddleware`中间件。

在ASP.NET Core 官方文档中[Static files in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/static-files?view=aspnetcore-2.1&tabs=aspnetcore2x)，介绍了如何访问自定义目录的静态资源文件。

如果需要访问自定义路径目录的资源，需要添加类似以下代码：
```
app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(
            Path.Combine(Directory.GetCurrentDirectory(), "MyStaticFiles")),
        RequestPath = "/StaticFiles"
    });
```

但这似乎并不能满足我们的需求。Why？看标题，开发**独立通用**的RPL。怎么理解独立通用？也就意味着RPL中的资源文件最好能够通过程序集打包。这样才能完全独立。否则，在发布RPL时，还需要输出静态资源文件，显然增加了使用的难度。而如何将资源文件打包进程序集呢？——内嵌资源。

# 5. Embedded Resource
一个程序集主要由两种类型的文件构成，它们分别是承载IL代码的托管模块文件和编译时内嵌的资源文件。那在.NET Core中如何定义内嵌资源呢？
1. 编辑RPL.CommonUI.csproj文件，添加wwwroot为内嵌资源。
```
  <ItemGroup>
    <EmbeddedResource Include="wwwroot\**\*" />
  </ItemGroup>
```
2. 添加`GenerateEmbeddedFilesManifest`节点，指定生成内嵌资源清单。
```
<GenerateEmbeddedFilesManifest>true</GenerateEmbeddedFilesManifest>
```
3. 添加`Microsoft.Extensions.FileProviders.Embedded`Nuget包引用。

修改完后的RPL.CommonUI.csproj，如下所示：
```
<Project Sdk="Microsoft.NET.Sdk.Razor">

  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <GenerateEmbeddedFilesManifest>true</GenerateEmbeddedFilesManifest>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Mvc" Version="2.1.0" />
    <PackageReference Include="Microsoft.Extensions.FileProviders.Embedded" Version="2.1.0" />
  </ItemGroup>
  <ItemGroup>
    <EmbeddedResource Include="wwwroot\**\*" />
  </ItemGroup>
</Project>
```

我们用ildasm.exe反编译RPL.CommonUI.dll，查看下其程序集清单：

![Manifest](https://upload-images.jianshu.io/upload_images/2799767-e82231307e3f7571.png?imageMogr2/auto-orient/strip%7CimageView2/2/w/1240)

从图中可以看出内嵌的demo.css文件，是以{程序集名称}.{文件路径}命名的。

那内嵌资源如何访问呢？可以借助`EmbeddedFileProvider`，我们仿照上面的例子，在`Startup.cs`的`Configure`方法中添加以下代码：
```
app.UseStaticFiles();

var dllPath = Path.Join(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "RPL.CommonUI.dll");
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new ManifestEmbeddedFileProvider(Assembly.LoadFrom(dllPath), "wwwroot")
});
```
CTRL+F5，运行。Perfect！
![](https://upload-images.jianshu.io/upload_images/2799767-b1957f2f89ec8c3a.png?imageMogr2/auto-orient/strip%7CimageView2/2/w/1240)

当然这也不是最好的解决方案，因为你肯定不想所有调用这个RPL的地方，添加这么几句代码，因为这段代码有很强的侵入性，且不可隔离变化。

# 5. Final Solution

1. 编辑RPL.CommonUI.csproj文件，添加wwwroot为内嵌资源。
```
  <ItemGroup>
    <EmbeddedResource Include="wwwroot\**\*" />
  </ItemGroup>
```
2. 添加`GenerateEmbeddedFilesManifest`节点，指定生成内嵌资源清单。
```
<GenerateEmbeddedFilesManifest>true</GenerateEmbeddedFilesManifest>
```
3. 添加`Microsoft.AspNetCore.StaticFiles`和`Microsoft.Extensions.FileProviders.Embedded`Nuget包引用。

修改完后的RPL.CommonUI.csproj，如下所示：
```
<Project Sdk="Microsoft.NET.Sdk.Razor">

  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <GenerateEmbeddedFilesManifest>true</GenerateEmbeddedFilesManifest>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Mvc" Version="2.1.0" />
    <PackageReference Include="Microsoft.AspNetCore.StaticFiles" Version="2.1.0" />
    <PackageReference Include="Microsoft.Extensions.FileProviders.Embedded" Version="2.1.0" />
  </ItemGroup>
  <ItemGroup>
    <EmbeddedResource Include="wwwroot\**\*" />
  </ItemGroup>
</Project>
```
4. 接下来添加`CommonUIConfigureOptions.cs`，定义如下：
```
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using System;

namespace RPL.CommonUI
{
    internal class CommonUIConfigureOptions: IPostConfigureOptions<StaticFileOptions>
    {
        public CommonUIConfigureOptions(IHostingEnvironment environment)
        {
            Environment = environment;
        }
        public IHostingEnvironment Environment { get; }

        public void PostConfigure(string name, StaticFileOptions options)
        {
            name = name ?? throw new ArgumentNullException(nameof(name));
            options = options ?? throw new ArgumentNullException(nameof(options));

            // Basic initialization in case the options weren't initialized by any other component
            options.ContentTypeProvider = options.ContentTypeProvider ?? new FileExtensionContentTypeProvider();
            if (options.FileProvider == null && Environment.WebRootFileProvider == null)
            {
                throw new InvalidOperationException("Missing FileProvider.");
            }

            options.FileProvider = options.FileProvider ?? Environment.WebRootFileProvider;

            // Add our provider
            var filesProvider = new ManifestEmbeddedFileProvider(GetType().Assembly, "wwwroot");
            options.FileProvider = new CompositeFileProvider(options.FileProvider, filesProvider);
        }
    }
}

```

5. 然后添加`CommonUIServiceCollectionExtensions.cs`，代码如下：
```
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace RPL.CommonUI
{
    public static class CommonUIServiceCollectionExtensions
    {
        public static void AddCommonUI(this IServiceCollection services)
        {
            services.ConfigureOptions(typeof(CommonUIConfigureOptions));
        }
    }
}

```
6. 修改RPL.Web启动类startup.cs，在`services.AddMvc()`之前添加`services.AddCommonUI();`即可。

7. CTRL+F5重新运行，我们发现H1被成功设置为红色，检查发现demo.css也能正确被请求，检查network也可以看到其Request URL为：https://localhost:44379/css/demo.css
![](https://upload-images.jianshu.io/upload_images/2799767-751cbe1d3f8246b2.png?imageMogr2/auto-orient/strip%7CimageView2/2/w/1240)
![Request URL](https://upload-images.jianshu.io/upload_images/2799767-7e14bcf25136a39f.png?imageMogr2/auto-orient/strip%7CimageView2/2/w/1240)


>1. [Static files in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/static-files?view=aspnetcore-2.1&tabs=aspnetcore2x)
>2. [File Providers in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/file-providers?view=aspnetcore-2.1)
>3. [ManifestEmbeddedFileProvider Class](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.fileproviders.manifestembeddedfileprovider?view=aspnetcore-2.1)
>4. [Make it easier to use static assets that are part of a RCL project](https://github.com/aspnet/Razor/issues/2322)
>5. [.NET Core的文件系统[4]：由EmbeddedFileProvider构建的内嵌（资源）文件系统](https://www.cnblogs.com/artech/p/net-core-file-provider-04.html)
