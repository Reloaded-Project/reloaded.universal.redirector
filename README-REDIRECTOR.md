# About the File Redirector

The file redirector is a mod for [Reloaded II](https://github.com/Reloaded-Project/Reloaded-II) Mod Loader that allows you to replace the files loaded by a game or application.

# Usage Guide

## Download the Mod

First of all, download the mod which we will be using to extend the functionality of our mod. In this case, the `Reloaded File Redirector`.

![DownloadMod](https://raw.githubusercontent.com/Reloaded-Project/reloaded.universal.redirector/master/docs/images/DownloadMod.png)

## Add Dependency to Redirector

In the `Edit Mod` menu we're going to add `Reloaded File Redirector` as a dependency.  

![AddDependency](https://raw.githubusercontent.com/Reloaded-Project/reloaded.universal.redirector/master/docs/images/AddDependency.png)

Adding a 'dependency' to your mod will make it such that the other mod will always be loaded when your mod is loaded. This is a necessary step. 

### Opening the Mod Folder

![OpenModFolder](https://raw.githubusercontent.com/Reloaded-Project/reloaded.universal.redirector/master/docs/images/OpenModFolder.png)

Go to the folder where your mod is stored, this can be done by simply clicking the `Open Folder` button.  

### Add Some Files

Make a folder called `Redirector`. 
Inside it place files that we want to be replaced.  

![FileRedirectorFolder](https://raw.githubusercontent.com/Reloaded-Project/reloaded.universal.redirector/master/docs/images/FileRedirectorFolder.png)

Files are mapped by their location relative to the EXE of the application you are using the Redirector with.

For a game at `E:/SonicHeroes/TSonic_win.exe`, the paths are relative to: `E:/SonicHeroes/`.

To replace a music file at `E:/SonicHeroes/dvdroot/bgm/SNG_STG26.adx`, your mod should place the file at `Redirector/dvdroot/bgm/SNG_STG26.adx`.

The contents of our mod folder should now look as follows.

```
// Mod Contents
ModConfig.json
Preview.png
Redirector
└─dvdroot
  ├─advertise
  │   adv_pl_rouge.one
  └─playmodel
      ro.txd
      ro_dff.one
```

The connectors `└─` represent folders.