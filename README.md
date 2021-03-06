<div align="center">
	<h1>Reloaded II: Universal File Redirector</h1>
	<img src="https://i.imgur.com/BjPn7rU.png" width="150" align="center" />
	<br/> <br/>
	<strong>Assert.Equal(expectedPath, actualPath);</strong>
	<p>1 Test Failed. Unexpected path.</p>
    <b>Id: reloaded.universal.monitor</b>
</div>

# Prerequisites

The CRI FS Hook uses the [Hooks Shared Library](https://github.com/Sewer56/Reloaded.SharedLib.Hooks).
Please download and extract that mod first.

# About This Project

This project is a set of mods for [Reloaded II](https://github.com/Reloaded-Project/Reloaded-II) Mod Loader that provides support for file redirection that can be used by other mods.

## Inside This Repository

#### Mods
- Redirector: *Provides file redirection support for other mods.

- Monitor: *Prints out the files being accessed by the application.

- RedirectorMonitor: *Prints out the files being redirected by the redirector.

## How to Use

A. Add a dependency on this mod in your mod configuration.

```json
"ModDependencies": ["reloaded.universal.redirector"]
```

B. Add a folder called `Redirector` in your mod folder.

C. Add files to be redirected in the `Redirector` folder.

Files are mapped by their location relative to the EXE of the application you are using the Redirector with.

#### Example

For a game at `E:/SonicHeroes/TSonic_win.exe`
The paths are relative to: ``E:/SonicHeroes/`

To replace a music file at `E:/SonicHeroes/dvdroot/bgm/SNG_STG26.adx`, your mod should place the file at `Redirector/dvdroot/bgm/SNG_STG26.adx`.

## How to Use The API (Programmers)

- Add the `Reloaded.Universal.Redirector.Interfaces` NuGet package to your project.
- Add the dependency `reloaded.universal.redirector` to `ModDependencies` in your `ModConfig.json`. 
- In your `Start()` function, acquire the Controller `_modLoader.GetController<IRedirectorController>()`

For more information and best practices, refer to [Reloaded-II Docs: Inter Mod Communication](https://reloaded-project.github.io/Reloaded-II/InterModCommunication/).

For an example, consider looking at `Reloaded.Universal.Monitor` in this repository.