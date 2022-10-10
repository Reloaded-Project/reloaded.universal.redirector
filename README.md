<div align="center">
	<h1>Reloaded II: Universal File Redirector</h1>
	<img src="https://i.imgur.com/BjPn7rU.png" width="150" align="center" />
	<br/> <br/>
	<strong>Assert.Equal(expectedPath, actualPath);</strong>
	<p>1 Test Failed. Unexpected path.</p>
    <b>Id: reloaded.universal.monitor</b>
</div>

# About This Project

This project is a set of mods for [Reloaded II](https://github.com/Reloaded-Project/Reloaded-II) Mod Loader that provides support for file redirection that can be used by other mods.

## Inside This Repository

#### Mods
- [Redirector](./README-REDIRECTOR.md): *Provides file redirection support for other mods.  
- [Monitor](./README-MONITOR.md): *Prints out the files being accessed by the application.  
- [RedirectorMonitor](./README-REDIRECTORMONITOR.md): *Prints out the files being redirected by the redirector.  

(Click to read individual mods' readmes)

## How to Use The API (Programmers)

- Add the `Reloaded.Universal.Redirector.Interfaces` NuGet package to your project.
- Add the dependency `reloaded.universal.redirector` to `ModDependencies` in your `ModConfig.json`. 
- In your `Start()` function, acquire the Controller `_modLoader.GetController<IRedirectorController>()`

For more information and best practices, refer to [Reloaded-II Docs: Inter Mod Communication](https://reloaded-project.github.io/Reloaded-II/InterModCommunication/).

For an example, consider looking at `Reloaded.Universal.Monitor` in this repository.
