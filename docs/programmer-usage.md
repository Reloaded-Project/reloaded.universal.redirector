# Programmer Usage

!!! tip

    Redirector uses [Reloaded Dependency Injection](https://reloaded-project.github.io/Reloaded-II/DependencyInjection_HowItWork/) to expose an API. It's recommended to read that first if you haven't before already.

Anyway, in short:  

- Add the `Reloaded.Universal.Redirector.Interfaces` NuGet package to your project.  
- Add the dependency `reloaded.universal.redirector` to `ModDependencies` in your `ModConfig.json`.  
- In your `Mod()` entry point, acquire the Controller `_modLoader.GetController<IRedirectorController>()`.